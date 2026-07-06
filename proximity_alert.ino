/*
  proximity_alert.ino
  Rear proximity detection using HC-SR04 ultrasonic sensor.
  Triggers buzzer when object detected within DETECTION_DISTANCE.
  Called from smart_helmet.ino every 100ms.
*/

// ── Pins ──────────────────────────────────────────────────────────────────────
#define TRIG_PIN            5
#define ECHO_PIN            18
#define BUZZER_PIN          23

// ── Config ────────────────────────────────────────────────────────────────────
#define DETECTION_DISTANCE  100     // cm — alert if object closer than this
#define BUZZER_FREQ         2000    // Hz

// ── State ─────────────────────────────────────────────────────────────────────
static bool prox_object_detected = false;

// ─────────────────────────────────────────────────────────────────────────────
void proximity_setup() {
  pinMode(TRIG_PIN,   OUTPUT);
  pinMode(ECHO_PIN,   INPUT);
  pinMode(BUZZER_PIN, OUTPUT);
  digitalWrite(TRIG_PIN,   LOW);
  digitalWrite(BUZZER_PIN, LOW);
  Serial.println("Proximity alert ready.");
}

// ─────────────────────────────────────────────────────────────────────────────
static float prox_get_distance() {
  digitalWrite(TRIG_PIN, LOW);
  delayMicroseconds(2);
  digitalWrite(TRIG_PIN, HIGH);
  delayMicroseconds(10);
  digitalWrite(TRIG_PIN, LOW);

  // 25ms timeout = max ~430cm range
  long duration = pulseIn(ECHO_PIN, HIGH, 25000);
  if (duration == 0) return 999.0f;   // no echo = out of range
  return duration * 0.01715f;         // convert to cm (speed of sound / 2)
}

// ─────────────────────────────────────────────────────────────────────────────
static void prox_beep() {
  tone(BUZZER_PIN, BUZZER_FREQ, 100);
  delay(200);
  tone(BUZZER_PIN, BUZZER_FREQ, 100);
  
}

// ─────────────────────────────────────────────────────────────────────────────
// Called every 100ms from smart_helmet.ino
void proximity_update() {
  float distance = prox_get_distance();

  if (distance > 0 && distance <= DETECTION_DISTANCE) {
    if (!prox_object_detected) {
      prox_object_detected = true;
      Serial.printf("[PROX] Object detected at %.1f cm — ALERT!\n", distance);
      if (!accident_alarm_active()) {
        prox_beep();
      }
    }
  } else {
    if (prox_object_detected) {
      Serial.println("[PROX] Object cleared.");
    }
    prox_object_detected = false;
  }
}
