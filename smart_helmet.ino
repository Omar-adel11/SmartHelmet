/*
  smart_helmet.ino — MAIN FILE
  Smart Helmet System — ESP32-WROOM

  Features:
    1. Accident detection via 1D-CNN TFLite model
    2. Turn signal LEDs via head tilt detection
    3. Rear proximity alert via HC-SR04 + buzzer

  Pin assignments:
    MPU-9250  SDA     → GPIO 21
    MPU-9250  SCL     → GPIO 22
    MPU-9250  AD0     → GND
    Right LED         → GPIO 4
    Left LED          → GPIO 15
    HC-SR04   TRIG    → GPIO 5
    HC-SR04   ECHO    → GPIO 18
    Buzzer            → GPIO 23
    SOS indicator LED → GPIO 2
    SOS cancel button -> GPIO 19 to GND

  Sketch folder must contain:
    smart_helmet.ino       ← this file
    accident_detection.ino ← ML inference
    turn_signals.ino       ← head tilt LEDs
    proximity_alert.ino    ← ultrasonic + buzzer
    model_int8.h           ← TFLite model
*/

#include <Wire.h>
#include <MPU9250.h>
#include <Chirale_TensorFlowLite.h>
#include "tensorflow/lite/micro/all_ops_resolver.h"
#include "tensorflow/lite/micro/micro_interpreter.h"
#include "tensorflow/lite/micro/system_setup.h"
#include "tensorflow/lite/schema/schema_generated.h"
#include "model_int8.h"

// ── Shared IMU instance (used by all modules) ─────────────────────────────────
MPU9250 mpu;

// ── Sampling config ───────────────────────────────────────────────────────────
#define SAMPLE_RATE_HZ          100
#define IMU_SAMPLE_INTERVAL_US  (1000000UL / SAMPLE_RATE_HZ)  // 10ms

// ── Shared raw IMU readings (updated every sample, read by all modules) ───────
float g_ax = 0, g_ay = 0, g_az = 0;
float g_gx = 0, g_gy = 0, g_gz = 0;

// ── Timing ────────────────────────────────────────────────────────────────────
unsigned long last_imu_us        = 0;
unsigned long last_proximity_ms  = 0;
#define PROXIMITY_INTERVAL_MS    100   // check ultrasonic every 100ms

// ─────────────────────────────────────────────────────────────────────────────
void setup() {
  Serial.begin(115200);
  delay(500);
  Serial.println("\n========================================");
  Serial.println("   SMART HELMET SYSTEM — Booting...");
  Serial.println("========================================");

  Wire.begin(21, 22);

  // Init MPU-9250
  if (!mpu.setup(0x68)) {
    Serial.println("ERROR: MPU-9250 not found! Check wiring.");
    while (true) delay(1000);
  }
  Serial.println("MPU-9250 ready.");

  // Init each module
  accident_setup();    // TFLite model + arena
  turn_signal_setup(); // LED pins + calibration
  proximity_setup();   // Ultrasonic + buzzer pins

  last_imu_us       = micros();
  last_proximity_ms = millis();

  Serial.println("System ready. Monitoring...\n");
}

// ─────────────────────────────────────────────────────────────────────────────
void loop() {
  unsigned long now_us = micros();
  unsigned long now_ms = millis();

  // Poll SOS button every loop so cancel/reset presses are not missed.
  accident_poll_controls();

  // ── 1. Read IMU at exactly 100 Hz ─────────────────────────────────────────
  if (now_us - last_imu_us >= IMU_SAMPLE_INTERVAL_US) {
    last_imu_us += IMU_SAMPLE_INTERVAL_US;

    if (mpu.update()) {
      g_ax = mpu.getAccX();
      g_ay = mpu.getAccY();
      g_az = mpu.getAccZ();
      g_gx = mpu.getGyroX();
      g_gy = mpu.getGyroY();
      g_gz = mpu.getGyroZ();

      // Feed new sample to accident detector (handles its own windowing)
      accident_update(g_ax, g_ay, g_az, g_gx, g_gy, g_gz);

      // Update turn signals from current tilt
      turn_signal_update(g_ax, g_ay, g_az);
    }
  }

  // ── 2. Manage LED blinking (must run every loop for smooth blink) ──────────
  turn_signal_blink(micros());

  // ── 3. Check proximity sensor every 100ms ─────────────────────────────────
  if (now_ms - last_proximity_ms >= PROXIMITY_INTERVAL_MS) {
    last_proximity_ms = now_ms;
    proximity_update();
  }
}
