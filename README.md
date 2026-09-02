# Minimal_VR_sensor_synchronization


## Unity–Arduino serial communication

`SerialClient.cs` sends and receives newline-terminated text without blocking Unity's frame loop. A worker thread waits for serial input, and Unity's `Update()` method delivers received lines on the main thread. It is therefore safe for a `LineReceived` handler to update GameObjects, UI, or other Unity objects.

### Supported targets

This sample is intended for the Unity Editor and standalone players on Windows, macOS, and Linux. It does not directly support Android/Meta Quest, iOS, consoles, or WebGL. Those targets require a platform-specific USB serial plugin or, for a browser, a JavaScript Web Serial integration.

### Unity setup

1. Open **Edit > Project Settings > Player > Other Settings**.
2. Set **API Compatibility Level** to **.NET Framework**. Older Unity versions label this **.NET 4.x**.
3. Add `ARDUINO_SERIAL` under **Scripting Define Symbols**, then select **Apply**.
4. Add `SerialClient` to a GameObject.
5. In its Inspector, set the same baud rate used by the Arduino sketch and enter the port name:
   - Windows: `COM3`, `COM4`, and so on.
   - Linux: commonly `/dev/ttyUSB0` or `/dev/ttyACM0`.
   - macOS: commonly `/dev/cu.usbmodem...` or `/dev/cu.usbserial...`.

On Linux, the user running Unity must have permission to access the port, often through membership in the `dialout` group. Close the Arduino Serial Monitor before opening the port in Unity because only one program can normally own it at a time.

## Unity example

Attach the controller script to a GameObject and assign its `Serial Client` field in the Inspector.

Use `SendLine("command")` for the usual line-based protocol. Use `Send("raw text")` only when the device protocol does not expect a trailing newline. Both methods return `false` instead of throwing when the port is disconnected or a write fails.

## API summary

- `bool Open()` — opens the configured port for continuous reading and writing; safe to call again.
- `void Close()` — stops reading and releases the port; safe to call repeatedly.
- `bool IsConnected` — reports whether the port is currently open.
- `event Action<string> LineReceived` — receives complete lines on Unity's main thread.
- `bool SendLine(string)` — sends text followed by `\n`.
- `bool Send(string)` — sends text exactly as supplied.

The component closes the port when it is disabled or destroyed. Read errors are returned to the main thread for logging, and one failing `LineReceived` subscriber does not prevent other subscribers from receiving the same line.
