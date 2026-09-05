#if ARDUINO_SERIAL && (UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX || UNITY_STANDALONE)
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
/// Exchanges newline-terminated text messages with a serial device.
/// Reading happens on a worker thread; LineReceived is always raised by Update
/// on Unity's main thread.
/// </summary>
public class SerialClientReadOnly : MonoBehaviour
{
#pragma warning disable 0414 // These Inspector fields are used only when desktop serial support is compiled in.
    [Header("Serial device")]
    [Tooltip("Windows: COM3. Linux: /dev/ttyUSB0. macOS: /dev/cu.usbmodem...")]
    [SerializeField] string portName = "COM3";

    [Tooltip("Must match the baud rate used by the device.")]
    [SerializeField] int baudRate = 115200;

    [Tooltip("Open the port automatically when this component starts.")]
    [SerializeField] bool openOnStart;

    [Header("Timeouts and workload")]
    [Tooltip("How long the reader waits for a complete line before checking whether it should stop.")]
    [Min(1)]
    [SerializeField] int readTimeoutMilliseconds = 250;

#pragma warning restore 0414

    [Tooltip("Maximum number of received lines delivered during one Unity frame.")]
    [Min(1)]
    [SerializeField] int maxMessagesPerFrame = 100;

    readonly ConcurrentQueue<string> receivedLines = new ConcurrentQueue<string>();
    readonly ConcurrentQueue<string> backgroundErrors = new ConcurrentQueue<string>();

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
    /// Opens the configured port for both reading and writing.
    /// Returns true when connected. Calling Open again while connected is safe.
    /// </summary>
    public bool Open()
    {
#if SERIAL_PORT_SUPPORTED
        if (IsConnected)
            return true;

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
                ReadTimeout = Math.Max(1, readTimeoutMilliseconds)
            };
            newPort.Open();

            lock (portLock)
            {
                serialPort = newPort;
                serialRunning = true;
                serialThread = new Thread(() => SerialLoop(newPort))
                {
                    IsBackground = true,
                    Name = "SerialClient reader"
                };
                serialThread.Start();
            }

            Debug.Log($"[SerialClient] Opened {portName} at {baudRate} baud.");
            return true;
        }
        catch (Exception exception)
        {
            serialRunning = false;
            lock (portLock)
            {
                serialPort = null;
                serialThread = null;
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
