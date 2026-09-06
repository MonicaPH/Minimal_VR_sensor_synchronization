# Minimal_VR_sensor_synchronization

A minimal Unity project that exchanges messages with an M5Stack AtomS3 over USB
serial, together with the AtomS3 firmware it talks to.

- `minimal_vr_sensor_synchronization/` — the Unity project (Unity 6000.5.9f1).
- `serial_device/` — four PlatformIO firmware projects for the AtomS3; each has
  its own README.

## Unity–Arduino serial communication

`Assets/SerialClient.cs` sends and receives newline-terminated text without blocking Unity's frame loop. A worker thread waits for serial input, and Unity's `Update()` method delivers received lines on the main thread. It is therefore safe for a `LineReceived` handler to update GameObjects, UI, or other Unity objects.

### Supported targets

This sample is intended for the Unity Editor and standalone players on Windows, macOS, and Linux. It does not directly support Android/Meta Quest, iOS, consoles, or WebGL. Those targets require a platform-specific USB serial plugin or, for a browser, a JavaScript Web Serial integration.

### Unity setup

1. Open **Edit > Project Settings > Player > Other Settings**.
2. Set **API Compatibility Level** to **.NET Framework**. Older Unity versions
   label this **.NET 4.x**. The project is already saved with this setting.
3. Open one of the sample scenes from the Project window (see
   [Sample scenes](#sample-scenes)).
4. Select the **SerialClient** GameObject in the scene and set the same baud
   rate used by the firmware (115200) and the port name:
   - Windows: `COM3`, `COM4`, and so on.
   - Linux: commonly `/dev/ttyUSB0` or `/dev/ttyACM0`.
   - macOS: commonly `/dev/cu.usbmodem...` or `/dev/cu.usbserial...`.

On Linux, the user running Unity must have permission to access the port, often through membership in the `dialout` group. Close the Arduino Serial Monitor or `pio device monitor` before opening the port in Unity because only one program can normally own it at a time.

## Sample scenes

Each scene pairs with one firmware project under `serial_device/`.

| Scene | Access | Firmware | What it does |
|-------|--------|----------|--------------|
| `Assets/UnitySendScene/SendTrigger.unity` | `Write`, opened on start | `m5atoms3-trigger-box` | Unity sends `1`; the AtomS3 flashes its display |
| `Assets/UnityReceiveScene/ReceiveTrigger.unity` | `Read`, opened on start | `m5atoms3-button-input` | Each received `1` advances a cube around a circle |
| `Assets/SendReceiveScene/SendReceive.unity` | `ReadWrite`, opened on start | `m5atoms3-reaction-timer` | `Space` starts a reaction-time trial and shows the result |
| `Assets/WorkshopScene/SerialSample.unity` | chosen at runtime with `1`/`2`/`3` | `m5atoms3-workshop-version` | All of the above in one scene |

The three single-purpose scenes open the port in `Start()` with the access
stored on the `SerialClient` component, so nothing has to be pressed to
connect. The workshop scene deliberately starts closed.

### Send scene

`Assets/UnitySendScene/SendTrigger.cs` is attached to the Trigger Object and sends
the line `1` when its **Trigger Key** (`T` by default) is pressed.

### Receive scene

`Assets/UnityReceiveScene/ReceiveTrigger.cs` is attached to the Cube and
subscribes to `LineReceived`. Each `1` advances the cube 0.2° around a circle of
radius 0.5 in the x/y plane, centred on the position the cube had at scene
start. `Awake` places the cube on that orbit, so the first message does not make
it jump.

The AtomS3 repeats `1` for as long as its button is held, which is why the step
is small enough that holding the button walks the cube smoothly round the
circle.

### Send-receive scene

`Assets/SendReceiveScene/SendReceiveController.cs` owns one bidirectional
`SerialClient`, opens it on start, and starts a trial when **Space** is pressed:
it sends the byte `T`, plays a short beep generated in code (no audio file
needed), and draws the returned reaction time in the centre of the screen for
two seconds.

Other scripts can start trials and receive results through its public API:

```csharp
using UnityEngine;

public class ExperimentExample : MonoBehaviour
{
    [SerializeField] SendReceiveController reactionController;

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

### Workshop scene

`Assets/WorkshopScene/` holds the combined demo presented at ACII2026.

All four components share the one `SerialClient`; see
[One device, one client](#one-device-one-client).

Enter Play mode. Nothing is connected until an access mode is chosen:

| Key | Access | What works |
|-----|--------|------------|
| `1` | `Read` | `ArduinoController` walks the cube around a circle on `1` from the AtomS3 button |
| `2` | `Write` | The `T` key sends the line `1`, flashing the AtomS3 display |
| `3` | `ReadWrite` | `Space` starts a trial and `R:<microseconds>` comes back |

Each key closes the port before reopening it, so only one access mode is ever
live.

## SerialClient API summary

- `bool Open()` — opens the configured port using the **Access On Start** value set in the Inspector.
- `bool Open(SerialAccess)` — opens the port for `Read`, `Write`, or `ReadWrite`. **Fails if the port is already open**; call `Close()` first, so changing access is always deliberate. Also fails for `None` and for any value outside the three real modes.
- `void Close()` — stops reading, releases the port, and resets `Access` to `None`; safe to call repeatedly.
- `SerialAccess Access` — the directions the port is currently open for; `None` while closed.
- `bool IsConnected` — reports whether the port is currently open.
- `event Action<string> LineReceived` — receives complete lines on Unity's main thread.
- `bool SendLine(string)` — sends text followed by `\n`.
- `bool Send(string)` — sends text exactly as supplied.

Use `SendLine("command")` for the usual line-based protocol. Use `Send("raw text")` only when the device protocol does not expect a trailing newline. Both methods return `false` instead of throwing when the port is disconnected or a write fails.

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

## Serial protocol

Defined in `Assets/WorkshopScene/ReactionTimeProtocol.cs` and implemented by the
firmware in `serial_device/`:

| Direction | Message | Meaning |
|-----------|---------|---------|
| Unity → AtomS3 | `T` | Start a reaction-time trial; sent without a newline |
| AtomS3 → Unity | `R:<microseconds>\n` | The button was pressed after that many microseconds |
| AtomS3 → Unity | `B\n` | A trial was already active; the timer was not restarted |
| Unity → AtomS3 | `1\n` | Flash the AtomS3 display (trigger box) |
| AtomS3 → Unity | `1\n` | Button press outside a trial |
