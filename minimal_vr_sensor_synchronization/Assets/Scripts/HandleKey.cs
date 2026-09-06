using UnityEngine;
using UnityEngine.InputSystem;

public class HandleKey : MonoBehaviour
{
    [SerializeField] SerialClientWriteOnly serialClient;

    [Header("Trigger Key")]
    [SerializeField] Key triggerKey = Key.T;

    void Start()
    {
        // Omit this if "Open On Start" is enabled on SerialClient.
        serialClient.Open();
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current?[triggerKey].wasPressedThisFrame == true)
        {
            if (serialClient.SendLine("1"))
                Debug.Log("Sent serial line: 1");
            else
                Debug.LogWarning("Serial device is not connected.");
        }
    }
}
