# M5Stack AtomS3 Reaction-Time Controller

A small PlatformIO project that on the M5Stack AtomS3
that flashes the display upon receiving a `1` via USB serial.

## Behavior

- Unity sends the single ASCII byte `1`.
- The AtomS3 flashes its LCD red, and resetting to black after 500 milliseconds.

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
| Unity → AtomS3 | `1` | Flash the screen; no newline required |


If upload cannot find the board, specify its serial port explicitly:

```sh
pio run --target upload --upload-port /dev/cu.usbmodemXXXX
pio device monitor --port /dev/cu.usbmodemXXXX
```
