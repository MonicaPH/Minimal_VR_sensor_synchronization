#include <Arduino.h>
#include <M5Unified.h>

namespace {
    constexpr uint32_t kSerialBaud = 115200;
    uint32_t screenFlashMs = 0;
    uint32_t screenFlashDurationMs = 500;

    void handleSerialInput(uint32_t nowMs) {
        while (USBSerial.available() > 0) {
            const char received = static_cast<char>(USBSerial.read());

            if (received == '1') {
                screenFlashMs = nowMs;
            }
        }
    }

    void renderScreen(uint32_t nowMs) {
        const uint32_t elapsedMs = nowMs - screenFlashDurationMs;
        if (elapsedMs >= screenFlashMs) {
            M5.Display.clearDisplay();
            return;
        }

        M5.Display.fillScreen(M5.Display.color565(255, 0, 0));
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
    renderScreen(nowMs);

    M5.delay(1);
}
