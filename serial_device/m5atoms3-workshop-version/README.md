# M5Stack AtomS3 Reaction-Time Controller

A small PlatformIO project that measures reaction time on the M5Stack AtomS3
and returns the result to Unity over USB serial.

## Behavior

- Unity sends the single ASCII byte `T` to begin a trial.
- The AtomS3 records `micros()` immediately and flashes its LCD red, fading to
  black over 500 milliseconds.
- The first front-button press ends the trial and returns
  `R:<microseconds>` followed by a newline.
- A second `T` during an active trial is rejected with `B` followed by a
  newline; it does not restart the timer.
- Button presses outside an active trial send a `1` followed by a newline.

## Requirements

- M5Stack AtomS3
- USB-C data cable
- Visual Studio Code with the PlatformIO extension, or PlatformIO Core

PlatformIO downloads the Arduino framework and M5Unified dependency declared in
`platformio.ini`.

## Build and upload

Open this directory as a PlatformIO project. From a terminal in the project
directory, you can also run:

```sh
pio run
pio run --target upload
pio device monitor
```

To upload and immediately open the Serial Monitor:

```sh
pio run --target upload && pio device monitor
```

The monitor is configured for 115200 baud in `platformio.ini`.
The firmware uses the AtomS3's built-in `USBSerial` connection directly and
prints `AtomS3 ready` when it starts. Local keyboard echo is enabled for the
PlatformIO monitor.

## Serial protocol

| Direction | Message | Meaning |
| --- | --- | --- |
| Unity → AtomS3 | `T` | Start a trial immediately; no newline required |
| AtomS3 → Unity | `R:347123\n` | Button was pressed after 347,123 µs |
| AtomS3 → Unity | `B\n` | A trial was already active |

The controller polls the button every millisecond. The 32-bit microsecond
counter can wrap safely during a trial.

## Run the native tests

The display timing is isolated from the hardware and can be tested with a local
C++ compiler:

```sh
c++ -std=c++11 -Wall -Wextra -Werror -Isrc \
  test/native/test_screen_effect.cpp \
  -o /tmp/m5atoms3-screen-effect-test
/tmp/m5atoms3-screen-effect-test

c++ -std=c++11 -Wall -Wextra -Werror -Isrc \
  test/native/test_reaction_timer.cpp \
  -o /tmp/m5atoms3-reaction-timer-test
/tmp/m5atoms3-reaction-timer-test
```

If upload cannot find the board, specify its serial port explicitly:

```sh
pio run --target upload --upload-port /dev/cu.usbmodemXXXX
pio device monitor --port /dev/cu.usbmodemXXXX
```
