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

- `bool Open()` — opens the configured port using the **Access On Start** value set in the Inspector.
- `bool Open(SerialAccess)` — opens the port for `Read`, `Write`, or `ReadWrite`. **Fails if the port is already open**; call `Close()` first, so changing access is always deliberate.
- `void Close()` — stops reading, releases the port, and resets `Access` to `None`; safe to call repeatedly.
- `SerialAccess Access` — the directions the port is currently open for; `None` while closed.
- `bool IsConnected` — reports whether the port is currently open.
- `event Action<string> LineReceived` — receives complete lines on Unity's main thread.
- `bool SendLine(string)` — sends text followed by `\n`.
- `bool Send(string)` — sends text exactly as supplied.

The component closes the port when it is disabled or destroyed. Read errors are returned to the main thread for logging, and one failing `LineReceived` subscriber does not prevent other subscribers from receiving the same line.

### One device, one client

A serial device has a single owner. Two `SerialPort` objects on the same port
share one kernel input queue, so each incoming byte reaches only one of them:
lines get split between the readers, both stay permanently mis-framed, and the
first read error closes one client for good. Use **one** `SerialClient` per
device and change its access instead:

```csharp
serialClient.Close();
serialClient.Open(SerialAccess.Read);   // reader thread only
serialClient.Open(SerialAccess.Write);  // no reader thread; Send/SendLine only
serialClient.Open(SerialAccess.ReadWrite);
```

`Read` and `Write` are enforced in C#, not by the operating system:
`System.IO.Ports.SerialPort` takes no access mode and always opens the device
read/write. `SerialAccess.Read` means *no reader thread is started*, and
without `Write` the `Send` methods return `false` and log a warning.

## Reaction-time experiment

`ReactionTimeController.cs` uses one bidirectional `SerialClient` to start a
trial and receive the result. That same client is shared by `ArduinoController`
and `HandleKey`; see [One device, one client](#one-device-one-client).

1. Add `SerialClient` to an empty GameObject and configure its port and baud
   rate as described above. Leave **Open On Start** unticked so `HandleKey`
   controls the connection.
2. Add `ReactionTimeController` to another GameObject.
3. Drag the GameObject containing `SerialClient` onto the controller's
   **Serial Client** field, and onto the same field on `ArduinoController` and
   `HandleKey`.
4. Enter Play mode. Nothing is connected until you choose an access mode:

   | Key | Access | What works |
   |-----|--------|------------|
   | `1` | `Read` | `ArduinoController` walks the cube around a circle on `1` from the AtomS3 button |
   | `2` | `Write` | The `T` key sends `1`, flashing the AtomS3 display |
   | `3` | `ReadWrite` | `Space` starts a trial and `R:<microseconds>` comes back |

   Each key closes the port before reopening it, so only one access mode is
   ever live.

Note that the **`T` key** and the **`T` byte** are different things. The `T` key
belongs to `HandleKey` and sends the line `1`; it works only in write-only mode.
A reaction-time trial is started with **Space**, which sends the byte `T`.

Each received `1` advances the cube one step around a circle in the x/y plane,
centred on the position it had at scene start. Because the AtomS3 repeats `1`
for as long as its button is held, **Degrees Per Message** is small by default
(0.2°) so that holding the button walks the cube smoothly round the circle.
`Awake` places the cube on its orbit, one **Circle Radius** away from where you
positioned it, so the first message does not make it jump.

`ArduinoController` ignores received `1` lines whenever the port is writable.
Once a trial ends, the AtomS3 keeps sending `1` for as long as its button is
held, so in `ReadWrite` mode those lines belong to the reaction-time flow rather
than to the cube demo, and they arrive fast enough to flood the console.

The controller creates its short beep in code, so it does not require an audio
file. Other scripts can start trials and receive results through its public API:

```csharp
using UnityEngine;

public class ExperimentExample : MonoBehaviour
{
    [SerializeField] ReactionTimeController reactionController;

    void OnEnable()
    {
        reactionController.ReactionTimeReceived += OnReactionTime;
    }

    void OnDisable()
    {
        reactionController.ReactionTimeReceived -= OnReactionTime;
    }

    public void BeginTrial()
    {
        reactionController.StartTrial();
    }

    void OnReactionTime(double milliseconds)
    {
        Debug.Log("Reaction time: " + milliseconds + " ms");
    }
}
```

The serial protocol is `T` from Unity to start a trial, `R:<microseconds>` from
the AtomS3 after the first button press, and `B` if Unity tries to start a
second trial while one is active. `effects:on`, `effects:off`, and
`effects:toggle` are newline-terminated setup messages. Disabling effects does
not disable timing.
