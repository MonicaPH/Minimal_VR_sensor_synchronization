using UnityEngine;

public class ArduinoController : MonoBehaviour
{
    [SerializeField] SerialClient serialClient;

    // void OnAwake()
    // {
    //     // use this if parent object also has SerialClient attached
    //     serialClient = GetComponent<SerialClient>();
    // }

    // HandleKey owns the connection, so this component only subscribes.
    // Subscribing does not require an open port.
    void OnEnable()
    {
        serialClient.LineReceived += OnSerialLine;
    }

    void OnDisable()
    {
        serialClient.LineReceived -= OnSerialLine;
    }

    void OnSerialLine(string line)
    {
        // While the port is writable a reaction-time trial is possible, and the AtomS3
        // keeps streaming "1" for as long as its button is held once the trial ends.
        // Those lines belong to the trial, not to this demo, and arrive fast enough to
        // flood the console, so ignore them entirely outside read-only mode.
        if (line == "1" && (serialClient.Access & SerialAccess.Write) != 0)
            return;

        Debug.Log("Arduino sent: " + line);

        // LineReceived runs on Unity's main thread, so moving the Cube is safe here.
        if (line == "1")
            // handle message and do something to Unity Objects
            transform.position += Vector3.forward * 0.1f;
    }
}