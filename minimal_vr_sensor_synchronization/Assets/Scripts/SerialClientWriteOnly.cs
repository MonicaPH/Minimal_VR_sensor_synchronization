#if ARDUINO_SERIAL && (UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX || UNITY_STANDALONE)
#define SERIAL_PORT_SUPPORTED
#endif

using System;
using System.Collections.Concurrent;
using UnityEngine;

#if SERIAL_PORT_SUPPORTED
using System.IO.Ports;
#endif

/// <summary>
/// Exchanges newline-terminated text messages with a serial device.
/// Reading happens on a worker thread; LineReceived is always raised by Update
/// on Unity's main thread.
/// </summary>
public class SerialClientWriteOnly : MonoBehaviour
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
    [Tooltip("Maximum time a write may block Unity's main thread.")]
    [Min(1)]
    [SerializeField] int writeTimeoutMilliseconds = 500;
#pragma warning restore 0414

#if SERIAL_PORT_SUPPORTED
    readonly object portLock = new object();
    SerialPort serialPort;
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

        SerialPort newPort = null;
        try
        {
            newPort = new SerialPort(portName, baudRate)
            {
                NewLine = "\n",
                WriteTimeout = Math.Max(1, writeTimeoutMilliseconds)
            };
            newPort.Open();

            lock (portLock)
            {
                serialPort = newPort;
                serialRunning = true;
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


    /// <summary>Stops the reader and releases the port. Calling Close repeatedly is safe.</summary>
    public void Close()
    {
#if SERIAL_PORT_SUPPORTED
        SerialPort portToClose;

        lock (portLock)
        {
            serialRunning = false;
            portToClose = serialPort;
            serialPort = null;

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
}
