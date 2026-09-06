# M5Stack AtomS3 Button-Input

A small PlatformIO project on the M5Stack AtomS3
that sends `1` over Serial when a button is pressed.

## Behavior

- A button pressed sends a `1` followed by a newline.

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


If upload cannot find the board, specify its serial port explicitly:

```sh
pio run --target upload --upload-port /dev/cu.usbmodemXXXX
pio device monitor --port /dev/cu.usbmodemXXXX
```
