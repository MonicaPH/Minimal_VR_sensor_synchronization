#include <Arduino.h>
#include <M5Unified.h>

constexpr uint32_t kSerialBaud = 115200;

void setup() {
    auto config = M5.config();
    config.serial_baudrate = 0;
    config.clear_display = true;
    M5.begin(config);

    USBSerial.begin(kSerialBaud);
    USBSerial.println("AtomS3 ready");
}

void loop() {
    M5.update();

    if (M5.BtnA.isPressed()) {
        USBSerial.println("1");
    }

    M5.delay(1);
}
