using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owns the serial connection for the sample scene. The device has a single
/// owner, so this component closes the port before reopening it with different
/// access instead of letting several clients hold it at once.
/// </summary>
public class SendTrigger : MonoBehaviour
{
    [SerializeField] SerialClient serialClient;

    [Header("Trigger Key")]
    [SerializeField] Key triggerKey = Key.T;

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard[triggerKey].wasPressedThisFrame)
            Send();
    }

    void Send()
    {
        if (serialClient == null || serialClient.Access != SerialAccess.Write)
        {
            Debug.LogWarning(
                $"Trigger key needs write-only access; press {writeOnlyKey}. " +
                $"Current access: {(serialClient == null ? SerialAccess.None : serialClient.Access)}.");
            return;
        }

        if (serialClient.SendLine("1"))
            Debug.Log("Sent serial line: 1");
        else
            Debug.LogWarning("Serial device is not connected.");
    }
}
