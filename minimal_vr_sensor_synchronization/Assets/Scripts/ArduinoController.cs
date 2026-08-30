using UnityEngine;

public class ArduinoController : MonoBehaviour
{
    [SerializeField] SerialClient serialClient;

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

        // Unity APIs are safe here.
        if (line == "BUTTON_DOWN")
            transform.position += Vector3.up;
    }

    public void TurnLedOn()
    {
        if (!serialClient.SendLine("1"))
            Debug.LogWarning("Serial device is not connected.");
    }
}