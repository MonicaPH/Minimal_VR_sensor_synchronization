using UnityEngine;
using UnityEngine.InputSystem;

public class HandleKey : MonoBehaviour
{
    [SerializeField] SerialClientWriteOnly serialClient;

    void Start()
    {
        // Omit this if "Open On Start" is enabled on SerialClient.
        serialClient.Open();
    }

    public void Send()
    {
        if (serialClient.SendLine("1"))
            Debug.Log("Sent serial line: 1");
        else
            Debug.LogWarning("Serial device is not connected.");
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current?.lKey.wasPressedThisFrame == true)
        {
            Send();
        }
    }
}
