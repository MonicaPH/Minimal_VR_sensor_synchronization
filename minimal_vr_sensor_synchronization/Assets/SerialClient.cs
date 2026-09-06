#if (UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX || UNITY_STANDALONE)
#define SERIAL_PORT_SUPPORTED
#endif

using System;
using System.Collections.Concurrent;
using UnityEngine;

#if SERIAL_PORT_SUPPORTED
using System.IO.Ports;
using System.Threading;
#endif

/// <summary>
/// Which directions of traffic a <see cref="SerialClient"/> is opened for.
/// A serial device has only one owner, so a scene should hold exactly one
/// SerialClient and change its access instead of opening several clients.
///
/// The values are bits so that access can be tested with &, but the type is
/// deliberately not [Flags]: that would make Unity draw the Inspector field as
/// a bitmask whose "Everything" entry stores -1, and a -1 access matches none
/// of the modes the key handlers expect.
/// </summary>
public enum SerialAccess
{
    None = 0,
    Read = 1 << 0,
    Write = 1 << 1,
    ReadWrite = Read | Write
}

/// <summary>
/// Exchanges newline-terminated text messages with a serial device.
/// Reading happens on a worker thread; LineReceived is always raised by Update
/// on Unity's main thread.
/// </summary>
public class SerialClient : MonoBehaviour
{
#pragma warning disable 0414 // These Inspector fields are used only when desktop serial support is compiled in.
    [Header("Serial device")]
    [Tooltip("Windows: COM3. Linux: /dev/ttyUSB0. macOS: /dev/cu.usbmodem...")]
    [SerializeField] string portName = "COM3";

    [Tooltip("Must match the baud rate used by the device.")]
    [SerializeField] int baudRate = 115200;

    [Tooltip("Open the port automatically when this component starts.")]
    [SerializeField] bool openOnStart;

    [Tooltip("Which directions Open() uses when called without an explicit access. " +
             "Leave this at None so that the mode has to be chosen deliberately at " +
             "runtime, with Open(access) or the access keys.")]
    [SerializeField] SerialAccess accessOnStart = SerialAccess.None;

    [Header("Timeouts and workload")]
    [Tooltip("How long the reader waits for a complete line before checking whether it should stop.")]
    [Min(1)]
    [SerializeField] int readTimeoutMilliseconds = 250;

    [Tooltip("Maximum time a write may block Unity's main thread.")]
    [Min(1)]
    [SerializeField] int writeTimeoutMilliseconds = 500;
#pragma warning restore 0414

    [Tooltip("Maximum number of received lines delivered during one Unity frame.")]
    [Min(1)]
    [SerializeField] int maxMessagesPerFrame = 100;

    readonly ConcurrentQueue<string> receivedLines = new ConcurrentQueue<string>();
    readonly ConcurrentQueue<string> backgroundErrors = new ConcurrentQueue<string>();

    SerialAccess access = SerialAccess.None;

    /// <summary>Which directions the port is currently open for; None while closed.</summary>
    public SerialAccess Access { get { return access; } }

    /// <summary>
    /// Raised from Unity's main thread for every complete line received.
    /// The newline itself is not included.
    /// </summary>
    public event Action<string> LineReceived;

#if SERIAL_PORT_SUPPORTED
    readonly object portLock = new object();
    SerialPort serialPort;
    Thread serialThread;
    volatile bool serialRunning;
#endif

    /// <summary>True while the serial port is open.</summary>
    public bool IsConnected
    {
        get
        {
#if SERIAL_PORT_SUPPORTED
            lock (portLock)
            {
                try
                {
                    return serialRunning && serialPort != null && serialPort.IsOpen;
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
            }
#else
            return false;
#endif
        }
    }

    void Start()
    {
        if (openOnStart)
            Open();
    }

    /// <summary>
    /// Opens the configured port using the access selected in the Inspector.
    /// Returns true when connected.
    /// </summary>
    public bool Open()
    {
        return Open(accessOnStart);
    }

    /// <summary>
    /// Opens the configured port for the requested directions and returns true when
    /// connected. Fails when the port is already open: call Close first, so that
    /// changing access is always deliberate.
    /// </summary>
    public bool Open(SerialAccess requestedAccess)
    {
        // A serial device has one owner. Reopening in place would silently change
        // the access other components already rely on, so require an explicit Close.
        if (access != SerialAccess.None)
        {
            Debug.LogWarning(
                "[SerialClient] The port is already open for " + access +
                ". Call Close before opening it again.");
            return false;
        }

        if (requestedAccess == SerialAccess.None)
        {
            Debug.LogWarning(
                "[SerialClient] No access is selected, so there is nothing to open. " +
                "Choose a mode at runtime with Open(access), or set the access field " +
                "if the port really should open on start.");
            return false;
        }

        // Refuse anything that is not one of the three real modes. Without this, a
        // stale value such as -1 opens the port with an access that no key handler
        // recognises, and the trigger and trial keys go dead with no explanation.
        if (requestedAccess != SerialAccess.Read &&
            requestedAccess != SerialAccess.Write &&
            requestedAccess != SerialAccess.ReadWrite)
        {
            Debug.LogWarning(
                $"[SerialClient] {(int)requestedAccess} is not a valid SerialAccess. " +
                "Use Read, Write, or ReadWrite; check the access field in the Inspector.");
            return false;
        }

#if SERIAL_PORT_SUPPORTED
        // Clean up a port left behind by a previous read error before reconnecting.
        Close();
        ClearQueue(receivedLines);
        ClearQueue(backgroundErrors);

        SerialPort newPort = null;
        try
        {
            newPort = new SerialPort(portName, baudRate)
            {
                NewLine = "\n",
                ReadTimeout = Math.Max(1, readTimeoutMilliseconds),
                WriteTimeout = Math.Max(1, writeTimeoutMilliseconds)
            };
            newPort.Open();

            lock (portLock)
            {
                serialPort = newPort;
                serialRunning = true;
                access = requestedAccess;

                if ((requestedAccess & SerialAccess.Read) != 0)
                {
                    serialThread = new Thread(() => SerialLoop(newPort))
                    {
                        IsBackground = true,
                        Name = "SerialClient reader"
                    };
                    serialThread.Start();
                }
            }

            Debug.Log($"[SerialClient] Opened {portName} at {baudRate} baud for {requestedAccess}.");
            return true;
        }
        catch (Exception exception)
        {
            serialRunning = false;
            lock (portLock)
            {
                serialPort = null;
                serialThread = null;
                access = SerialAccess.None;
            }

            if (newPort != null)
            {
                try
                {
                    newPort.Close();
                }
                catch
                {
                    // Preserve the original connection error.
                }
                newPort.Dispose();
            }

            Debug.LogWarning($"[SerialClient] Could not open {portName}: {exception.Message}");
            return false;
        }
#else
        Debug.LogWarning(
            "[SerialClient] Serial support is unavailable. For a Windows, macOS, or Linux " +
            "Editor/standalone build, add ARDUINO_SERIAL to Player Settings > Scripting Define Symbols.");
        return false;
#endif
    }

    /// <summary>Writes text exactly as supplied. Use SendLine for line-based Arduino commands.</summary>
    public bool Send(string message)
    {
        return Write(message, false);
    }

    /// <summary>Writes text followed by a newline, for example "LED_ON\n".</summary>
    public bool SendLine(string message)
    {
        return Write(message, true);
    }

    bool Write(string message, bool appendNewline)
    {
        if (message == null)
        {
            Debug.LogWarning("[SerialClient] Cannot send a null message.");
            return false;
        }

        // An open client without Write access is a configuration mistake worth naming.
        // A closed client falls through to the port checks below and fails quietly.
        if (access != SerialAccess.None && (access & SerialAccess.Write) == 0)
        {
            Debug.LogWarning("[SerialClient] The port is open read-only; it cannot send.");
            return false;
        }

#if SERIAL_PORT_SUPPORTED
        lock (portLock)
        {
            try
            {
                if (serialPort == null || !serialPort.IsOpen)
                    return false;

                if (appendNewline)
                    serialPort.WriteLine(message);
                else
                    serialPort.Write(message);

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SerialClient] Serial write failed: {exception.Message}");
                return false;
            }
        }
#else
        return false;
#endif
    }

#if SERIAL_PORT_SUPPORTED
    void SerialLoop(SerialPort port)
    {
        try
        {
            while (serialRunning)
            {
                try
                {
                    // Arduino Serial.println uses CRLF. ReadLine removes LF; remove only the remaining CR.
                    string line = port.ReadLine().TrimEnd('\r');
                    receivedLines.Enqueue(line);
                }
                catch (TimeoutException)
                {
                    // Expected while the device has not sent a complete line.
                }
            }
        }
        catch (Exception exception)
        {
            if (serialRunning)
                backgroundErrors.Enqueue(exception.Message);
        }
        finally
        {
            serialRunning = false;
        }
    }
#endif

    void Update()
    {
        bool readFailed = false;
        string error;
        while (backgroundErrors.TryDequeue(out error))
        {
            readFailed = true;
            Debug.LogWarning($"[SerialClient] Serial read failed: {error}");
        }

        if (readFailed)
            Close();

        int limit = Math.Max(1, maxMessagesPerFrame);
        string line;
        for (int delivered = 0; delivered < limit && receivedLines.TryDequeue(out line); delivered++)
            DispatchLine(line);
    }

    void DispatchLine(string line)
    {
        Action<string> subscribers = LineReceived;
        if (subscribers == null)
            return;

        foreach (Delegate subscriber in subscribers.GetInvocationList())
        {
            try
            {
                ((Action<string>)subscriber)(line);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    /// <summary>Stops the reader and releases the port. Calling Close repeatedly is safe.</summary>
    public void Close()
    {
        access = SerialAccess.None;

#if SERIAL_PORT_SUPPORTED
        SerialPort portToClose;
        Thread threadToJoin;

        lock (portLock)
        {
            serialRunning = false;
            portToClose = serialPort;
            threadToJoin = serialThread;
            serialPort = null;
            serialThread = null;

            if (portToClose != null)
            {
                try
                {
                    if (portToClose.IsOpen)
                        portToClose.Close();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[SerialClient] Error while closing the port: {exception.Message}");
                }
            }
        }

        if (threadToJoin != null && threadToJoin != Thread.CurrentThread)
        {
            int waitMilliseconds = Math.Max(250, readTimeoutMilliseconds + 250);
            if (!threadToJoin.Join(waitMilliseconds))
                Debug.LogWarning("[SerialClient] The serial reader did not stop before the shutdown timeout.");
        }

        if (portToClose != null)
            portToClose.Dispose();
#endif
    }

    void OnDisable()
    {
        Close();
    }

    void OnDestroy()
    {
        Close();
    }

    static void ClearQueue(ConcurrentQueue<string> queue)
    {
        string ignored;
        while (queue.TryDequeue(out ignored)) { }
    }
}
