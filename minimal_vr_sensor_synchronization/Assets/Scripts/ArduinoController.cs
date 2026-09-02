using UnityEngine;
using UnityEngine.InputSystem;

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

    void Update()
    {
        if (Keyboard.current?.lKey.wasPressedThisFrame == true)
        {
            TurnLedOn();
        }
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
            transform.position += Vector3.forward * 0.1f;
    }

    public void TurnLedOn()
    {
        if (serialClient.SendLine("1"))
            Debug.Log("Sent serial line: 1");
        else
            Debug.LogWarning("Serial device is not connected.");
    }
}
