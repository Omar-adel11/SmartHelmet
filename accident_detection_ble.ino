/*
  accident_detection.ino
  1D-CNN supervised accident detection via TFLite Micro.
  Called from smart_helmet.ino every IMU sample.

  BLE SOS uses the same service/characteristic UUIDs as the mobile app:
    Service UUID        : 6e400001-b5a3-f393-e0a9-e50e24dcca9e
    Characteristic UUID : 6e400002-b5a3-f393-e0a9-e50e24dcca9e
  SOS payload format    : "SOS|<millis_timestamp>"
  Device name           : "ESP32_MPU9250"  (matches what the app scans for)
*/

#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>

// ── BLE UUIDs — must match the mobile app exactly ─────────────────────────────
#define BLE_SERVICE_UUID        "6e400001-b5a3-f393-e0a9-e50e24dcca9e"
#define BLE_CHARACTERISTIC_UUID "6e400002-b5a3-f393-e0a9-e50e24dcca9e"
#define BLE_DEVICE_NAME         "ESP32_MPU9250"

static BLEServer*         ble_server         = nullptr;
static BLECharacteristic* ble_characteristic = nullptr;
static bool               ble_connected      = false;

class AccBLECallbacks : public BLEServerCallbacks {
  void onConnect(BLEServer* server) override {
    ble_connected = true;
    Serial.println("[BLE] Mobile app connected.");
  }
  void onDisconnect(BLEServer* server) override {
    ble_connected = false;
    Serial.println("[BLE] Mobile app disconnected — restarting advertising.");
    BLEDevice::startAdvertising();
  }
};

static void ble_setup() {
  BLEDevice::init(BLE_DEVICE_NAME);
  ble_server = BLEDevice::createServer();
  ble_server->setCallbacks(new AccBLECallbacks());

  BLEService* service = ble_server->createService(BLE_SERVICE_UUID);

  ble_characteristic = service->createCharacteristic(
    BLE_CHARACTERISTIC_UUID,
    BLECharacteristic::PROPERTY_NOTIFY | BLECharacteristic::PROPERTY_READ
  );
  ble_characteristic->addDescriptor(new BLE2902());
  ble_characteristic->setValue("READY");

  service->start();

  BLEAdvertising* advertising = BLEDevice::getAdvertising();
  advertising->addServiceUUID(BLE_SERVICE_UUID);
  BLEDevice::startAdvertising();

  Serial.printf("[BLE] Advertising as '%s'\n", BLE_DEVICE_NAME);
}

// ── Normalization stats from training ─────────────────────────────────────────
const float ACC_NORM_MEAN[6] = { 0.06207677721977234f, 0.019389599561691284f, 3.072171926498413f, -0.038939863443374634f, -0.5478214025497437f, 0.21976105868816376f };
const float ACC_NORM_STD[6]  = { 0.9281962513923645f, 0.11619482934474945f, 3.921076774597168f, 5.132999897003174f, 4.932448863983154f, 5.554668426513672f };

// ── Config ────────────────────────────────────────────────────────────────────
#define ACC_WINDOW_SIZE         200
#define ACC_N_CHANNELS          6
#define ACC_ML_THRESHOLD        0.50f
#define ACC_CONFIRM_MS          3000
#define ACC_CANCEL_COOLDOWN_MS  10000UL
#define SOS_LED_PIN             2
#define SOS_BUTTON_PIN          19
#define SOS_BUZZER_PIN          23
#define SOS_BUZZER_FREQ         2500
#define SOS_BUTTON_DEBOUNCE_MS  50UL

#define ACC_IMPACT_DELTA_ACCEL  2.0f
#define ACC_IMPACT_DELTA_GYRO   200.0f
#define ACC_IMPACT_ACCEL_G      2.8f
#define ACC_IMPACT_GYRO_DPS     250.0f

// ── TFLite ────────────────────────────────────────────────────────────────────
constexpr int kTensorArenaSize = 60 * 1024;
alignas(16) static uint8_t tensor_arena[kTensorArenaSize];

static tflite::AllOpsResolver      acc_resolver;
const tflite::Model*               acc_model      = nullptr;
tflite::MicroInterpreter*          acc_interpreter = nullptr;
TfLiteTensor*                      acc_input       = nullptr;
TfLiteTensor*                      acc_output      = nullptr;

// ── Sliding window buffer ─────────────────────────────────────────────────────
static int16_t acc_buf[ACC_WINDOW_SIZE][ACC_N_CHANNELS];
static int     acc_buf_idx    = 0;
static int     acc_buf_filled = 0;

// ── Pre-filter delta tracking ─────────────────────────────────────────────────
static float acc_prev[6] = {0};

// ── State machine ─────────────────────────────────────────────────────────────
enum AccState { ACC_IDLE, ACC_CONFIRMING, ACC_SOS_SENT };
static AccState       acc_state          = ACC_IDLE;
static unsigned long  acc_confirm_start  = 0;
static unsigned long  acc_cooldown_until = 0;
static bool           acc_button_stable  = HIGH;
static bool           acc_button_last    = HIGH;
static unsigned long  acc_button_changed = 0;

// ─────────────────────────────────────────────────────────────────────────────
void accident_setup() {
  tflite::InitializeTarget();

  acc_model = tflite::GetModel(accident_model_data);
  if (acc_model == nullptr) {
    Serial.println("ACC ERROR: model is null");
    while (true) delay(1000);
  }

  static tflite::MicroInterpreter static_interp(
    acc_model, acc_resolver, tensor_arena, kTensorArenaSize
  );
  acc_interpreter = &static_interp;

  if (acc_interpreter->AllocateTensors() != kTfLiteOk) {
    Serial.println("ACC ERROR: AllocateTensors failed — increase kTensorArenaSize");
    while (true) delay(1000);
  }

  acc_input  = acc_interpreter->input(0);
  acc_output = acc_interpreter->output(0);

  pinMode(SOS_LED_PIN,    OUTPUT);
  pinMode(SOS_BUTTON_PIN, INPUT_PULLUP);
  pinMode(SOS_BUZZER_PIN, OUTPUT);
  digitalWrite(SOS_LED_PIN,    LOW);
  digitalWrite(SOS_BUZZER_PIN, LOW);
  acc_button_last    = digitalRead(SOS_BUTTON_PIN);
  acc_button_stable  = acc_button_last;
  acc_button_changed = millis();

  // BLE must be initialized after TFLite (both use significant heap)
  ble_setup();

  Serial.printf("Accident detector ready. Arena: %d / %d bytes\n",
    acc_interpreter->arena_used_bytes(), kTensorArenaSize);
}

// ─────────────────────────────────────────────────────────────────────────────
static bool acc_impact_detected(float ax, float ay, float az,
                                float gx, float gy, float gz) {
  float da = sqrtf(sq(ax - acc_prev[0]) + sq(ay - acc_prev[1]) + sq(az - acc_prev[2]));
  float dg = sqrtf(sq(gx - acc_prev[3]) + sq(gy - acc_prev[4]) + sq(gz - acc_prev[5]));
  float accel_mag = sqrtf(sq(ax) + sq(ay) + sq(az));
  float gyro_mag  = sqrtf(sq(gx) + sq(gy) + sq(gz));

  acc_prev[0]=ax; acc_prev[1]=ay; acc_prev[2]=az;
  acc_prev[3]=gx; acc_prev[4]=gy; acc_prev[5]=gz;

  return (da > ACC_IMPACT_DELTA_ACCEL) ||
         (dg > ACC_IMPACT_DELTA_GYRO)  ||
         (accel_mag > ACC_IMPACT_ACCEL_G) ||
         (gyro_mag  > ACC_IMPACT_GYRO_DPS);
}

// ─────────────────────────────────────────────────────────────────────────────
static void acc_fill_tensor() {
  float scale = acc_input->params.scale;
  int   zp    = acc_input->params.zero_point;

  for (int t = 0; t < ACC_WINDOW_SIZE; t++) {
    int buf_t = (acc_buf_idx + t) % ACC_WINDOW_SIZE;
    for (int ch = 0; ch < ACC_N_CHANNELS; ch++) {
      float raw  = acc_buf[buf_t][ch] / 100.0f;
      float norm = (raw - ACC_NORM_MEAN[ch]) / ACC_NORM_STD[ch];
      int q = (int)roundf(norm / scale) + zp;
      if (q < -128) q = -128;
      if (q >  127) q =  127;
      acc_input->data.int8[t * ACC_N_CHANNELS + ch] = (int8_t)q;
    }
  }
}

// ─────────────────────────────────────────────────────────────────────────────
static float acc_run_inference() {
  acc_fill_tensor();
  if (acc_interpreter->Invoke() != kTfLiteOk) {
    Serial.println("ACC ERROR: Invoke failed");
    return 0.0f;
  }
  float scale = acc_output->params.scale;
  int   zp    = acc_output->params.zero_point;
  return (acc_output->data.int8[0] - zp) * scale;
}

// ─────────────────────────────────────────────────────────────────────────────
static void acc_send_ble_sos() {
  // Payload: "SOS|<timestamp_ms>" — app parses on the '|' delimiter
  char payload[32];
  snprintf(payload, sizeof(payload), "SOS|%lu", millis());

  ble_characteristic->setValue((uint8_t*)payload, strlen(payload));

  if (ble_connected) {
    ble_characteristic->notify();
    Serial.printf("[BLE] SOS sent: %s\n", payload);
  } else {
    // Value is still written so the app can read it on next connection
    Serial.println("[BLE] WARNING: no device connected — SOS stored but not notified.");
  }
}

// ─────────────────────────────────────────────────────────────────────────────
static void acc_trigger_sos() {
  Serial.println("!!! SOS TRIGGERED !!!");
  noTone(SOS_BUZZER_PIN);
  digitalWrite(SOS_BUZZER_PIN, LOW);
  digitalWrite(SOS_LED_PIN, HIGH);
  acc_send_ble_sos();
}

static bool acc_serial_command(char command) {
  if (!Serial.available()) return false;
  return Serial.read() == command;
}

static void acc_clear_window() {
  acc_buf_idx    = 0;
  acc_buf_filled = 0;
}

static void acc_reset_to_idle(const char* reason) {
  noTone(SOS_BUZZER_PIN);
  digitalWrite(SOS_BUZZER_PIN, LOW);
  digitalWrite(SOS_LED_PIN, LOW);
  acc_clear_window();
  acc_cooldown_until = millis() + ACC_CANCEL_COOLDOWN_MS;
  acc_state = ACC_IDLE;
  Serial.println(reason);
}

bool accident_alarm_active() {
  return acc_state == ACC_CONFIRMING;
}

// ─────────────────────────────────────────────────────────────────────────────
void accident_poll_controls() {
  bool button_now = digitalRead(SOS_BUTTON_PIN);
  unsigned long now = millis();

  if (button_now != acc_button_last) {
    acc_button_last    = button_now;
    acc_button_changed = now;
  }

  if ((now - acc_button_changed) < SOS_BUTTON_DEBOUNCE_MS) return;
  if (button_now == acc_button_stable) return;

  acc_button_stable = button_now;
  if (acc_button_stable != LOW) return;

  if (acc_state == ACC_CONFIRMING) {
    acc_reset_to_idle("[ACC] SOS cancelled by driver button.");
  } else if (acc_state == ACC_SOS_SENT) {
    acc_reset_to_idle("[ACC] System reset by driver button.");
  } else {
    Serial.println("[ACC] Button press detected.");
  }
}

// ─────────────────────────────────────────────────────────────────────────────
void accident_update(float ax, float ay, float az,
                     float gx, float gy, float gz) {
  // 1. Push into circular buffer (stored as int16 * 100 to save RAM)
  acc_buf[acc_buf_idx][0] = (int16_t)(ax * 100.0f);
  acc_buf[acc_buf_idx][1] = (int16_t)(ay * 100.0f);
  acc_buf[acc_buf_idx][2] = (int16_t)(az * 100.0f);
  acc_buf[acc_buf_idx][3] = (int16_t)(gx * 100.0f);
  acc_buf[acc_buf_idx][4] = (int16_t)(gy * 100.0f);
  acc_buf[acc_buf_idx][5] = (int16_t)(gz * 100.0f);
  acc_buf_idx = (acc_buf_idx + 1) % ACC_WINDOW_SIZE;
  if (acc_buf_filled < ACC_WINDOW_SIZE) acc_buf_filled++;

  // 2. State machine
  switch (acc_state) {

    case ACC_IDLE:
      if ((long)(millis() - acc_cooldown_until) < 0) break;
      if (acc_buf_filled < ACC_WINDOW_SIZE) break;
      if (!acc_impact_detected(ax, ay, az, gx, gy, gz)) break;
      {
        float prob = acc_run_inference();
        Serial.printf("[ACC] prob=%.4f\n", prob);
        if (prob >= ACC_ML_THRESHOLD) {
          Serial.printf("[ACC] ACCIDENT DETECTED (prob=%.4f) — confirming...\n", prob);
          acc_confirm_start = millis();
          tone(SOS_BUZZER_PIN, SOS_BUZZER_FREQ);
          acc_state = ACC_CONFIRMING;
        }
      }
      break;

    case ACC_CONFIRMING:
      if (millis() - acc_confirm_start >= ACC_CONFIRM_MS) {
        acc_trigger_sos();
        acc_state = ACC_SOS_SENT;
        break;
      }
      if (acc_serial_command('C')) {
        acc_reset_to_idle("[ACC] SOS cancelled by Serial.");
      }
      break;

    case ACC_SOS_SENT:
      if (acc_serial_command('R')) {
        acc_reset_to_idle("[ACC] System reset by Serial.");
      }
      break;
  }
}
