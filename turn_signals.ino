/*
  turn_signals.ino
  Detects head tilt via roll angle and controls left/right LEDs.
  Called from smart_helmet.ino every IMU sample.
*/

// ── Pin config ────────────────────────────────────────────────────────────────
#define LED_RIGHT_PIN       4
#define LED_LEFT_PIN        15

// ── Thresholds ────────────────────────────────────────────────────────────────
#define HEAD_TURN_ANGLE     15.0f   // degrees of roll to trigger signal

// ── Blink pattern ─────────────────────────────────────────────────────────────
#define BLINK_TOTAL_US      1500000UL   // 1.5s total blink duration
#define BLINK_ON_US         300000UL    // 0.3s ON
#define BLINK_CYCLE_US      500000UL    // 0.5s per cycle (0.3 ON + 0.2 OFF)

// ── State ─────────────────────────────────────────────────────────────────────
static bool          right_active    = false;
static bool          left_active     = false;
static unsigned long right_start_us  = 0;
static unsigned long left_start_us   = 0;

// Calibration offsets
static float roll_offset  = 0.0f;
static float pitch_offset = 0.0f;

// ─────────────────────────────────────────────────────────────────────────────
static void calibrate_neutral(float ax, float ay, float az) {
  // Called once during setup with averaged readings
  roll_offset  = atan2f(ay, az) * RAD_TO_DEG;
  pitch_offset = atan2f(-ax, sqrtf(ay*ay + az*az)) * RAD_TO_DEG;
  Serial.printf("[TURN] Calibrated — roll offset=%.2f°  pitch offset=%.2f°\n",
    roll_offset, pitch_offset);
}

// ─────────────────────────────────────────────────────────────────────────────
void turn_signal_setup() {
  pinMode(LED_RIGHT_PIN, OUTPUT);
  pinMode(LED_LEFT_PIN,  OUTPUT);
  digitalWrite(LED_RIGHT_PIN, LOW);
  digitalWrite(LED_LEFT_PIN,  LOW);
  Serial.println("Turn signals ready.");
}

// ─────────────────────────────────────────────────────────────────────────────
// Called every IMU sample — computes roll and triggers signals
void turn_signal_update(float ax, float ay, float az) {
  float roll = atan2f(ay, az) * RAD_TO_DEG - roll_offset;

  if (roll < -HEAD_TURN_ANGLE) {         // Tilted RIGHT
    if (!right_active) {
      right_start_us = micros();
      right_active   = true;
      Serial.println("[TURN] Right signal ON");
    }
    // Cancel left if active
    if (left_active) {
      left_active = false;
      digitalWrite(LED_LEFT_PIN, LOW);
    }
  }
  else if (roll > HEAD_TURN_ANGLE) {     // Tilted LEFT
    if (!left_active) {
      left_start_us = micros();
      left_active   = true;
      Serial.println("[TURN] Left signal ON");
    }
    // Cancel right if active
    if (right_active) {
      right_active = false;
      digitalWrite(LED_RIGHT_PIN, LOW);
    }
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Called every loop() iteration for smooth blinking
void turn_signal_blink(unsigned long now_us) {
  if (right_active) {
    unsigned long elapsed = now_us - right_start_us;
    if (elapsed >= BLINK_TOTAL_US) {
      digitalWrite(LED_RIGHT_PIN, LOW);
      right_active = false;
    } else {
      digitalWrite(LED_RIGHT_PIN,
        (elapsed % BLINK_CYCLE_US < BLINK_ON_US) ? HIGH : LOW);
    }
  }

  if (left_active) {
    unsigned long elapsed = now_us - left_start_us;
    if (elapsed >= BLINK_TOTAL_US) {
      digitalWrite(LED_LEFT_PIN, LOW);
      left_active = false;
    } else {
      digitalWrite(LED_LEFT_PIN,
        (elapsed % BLINK_CYCLE_US < BLINK_ON_US) ? HIGH : LOW);
    }
  }
}
