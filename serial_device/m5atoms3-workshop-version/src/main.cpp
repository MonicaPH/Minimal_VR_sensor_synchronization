#include <Arduino.h>
#include <M5Unified.h>

#include "reaction_timer.h"
#include "screen_effect.h"

namespace {
constexpr uint32_t kSerialBaud = 115200;
constexpr uint32_t kFadeDurationMs = 500;

ScreenEffect screenEffect(kFadeDurationMs);
ReactionTimer reactionTimer;
uint16_t lastRenderedRed = 256;

void handleSerialInput(uint32_t nowMs) {
    while (USBSerial.available() > 0) {
        const char received = static_cast<char>(USBSerial.read());

        // T is deliberately handled as one byte, without waiting for a newline.
        if (received == 'T') {
            if (reactionTimer.start(micros())) {
                screenEffect.flash(nowMs);
            } else {
                USBSerial.println('B');
            }
            continue;
        }
        if (received == '1') {
            screenEffect.flash(nowMs);
        }
    }
}

void handleButton(bool buttonPressed, uint32_t nowMs) {
    screenEffect.setButtonPressed(buttonPressed, nowMs);

    if (M5.BtnA.wasPressed()) {
        uint32_t elapsedUs = 0;
        if (reactionTimer.finish(micros(), elapsedUs)) {
            USBSerial.print("R:");
            USBSerial.println(elapsedUs);
        }
    }

    if (!reactionTimer.isActive() & buttonPressed) {
        USBSerial.println("1");
    }
}

void renderScreen(uint32_t nowMs) {
    const uint8_t red = screenEffect.redLevel(nowMs);
    if (red == lastRenderedRed) {
        return;
    }

    M5.Display.fillScreen(M5.Display.color565(red, 0, 0));
    lastRenderedRed = red;
}
}  // namespace

void setup() {
    auto config = M5.config();
    config.serial_baudrate = 0;
    config.clear_display = true;
    M5.begin(config);

    USBSerial.begin(kSerialBaud);
    USBSerial.println("AtomS3 ready");

    M5.Display.setRotation(0);
    M5.Display.setBrightness(128);
    M5.Display.fillScreen(TFT_BLACK);
}

void loop() {
    M5.update();
    const uint32_t nowMs = millis();

    handleSerialInput(nowMs);
    handleButton(M5.BtnA.isPressed(), nowMs);
    renderScreen(nowMs);

    M5.delay(1);
}
