using UnityEngine;

public class ArduinoController : MonoBehaviour
{
    [SerializeField] SerialClientReadOnly serialClient;

    // void OnAwake()
    // {
    //     // use this if parent object also has SerialClient attached
    //     serialClient = GetComponent<SerialClient>();
    // }

    void OnEnable()
    {
        serialClient.LineReceived += OnSerialLine;
    }

    void Start()
    {
        // Omit this if "Open On Start" is enabled on SerialClient.
        serialClient.Open();
    }

    void OnDisable()
    {
        serialClient.LineReceived -= OnSerialLine;
    }

    void OnSerialLine(string line)
    {
        Debug.Log("Arduino sent: " + line);

        // LineReceived runs on Unity's main thread, so moving the Cube is safe here.
        if (line == "1")
            // handle message and do something to Unity Objects
            transform.position += Vector3.forward * 0.1f;
    }
}