using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owns the serial connection for the sample scene. The device has a single
/// owner, so this component closes the port before reopening it with different
/// access instead of letting several clients hold it at once.
/// </summary>
public class HandleKey : MonoBehaviour
{
    [SerializeField] SerialClient serialClient;

    [Header("Trigger Key")]
    [SerializeField] Key triggerKey = Key.T;

    [Header("Access keys")]
    [SerializeField] Key readOnlyKey = Key.Digit1;
    [SerializeField] Key writeOnlyKey = Key.Digit2;
    [SerializeField] Key readWriteKey = Key.Digit3;

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard[readOnlyKey].wasPressedThisFrame)
            Reopen(SerialAccess.Read);
        else if (keyboard[writeOnlyKey].wasPressedThisFrame)
            Reopen(SerialAccess.Write);
        else if (keyboard[readWriteKey].wasPressedThisFrame)
            Reopen(SerialAccess.ReadWrite);

        if (keyboard[triggerKey].wasPressedThisFrame)
            SendTrigger();
    }

    /// <summary>Releases the port, then opens it again for the requested directions.</summary>
    public void Reopen(SerialAccess access)
    {
        if (serialClient == null)
        {
            Debug.LogError("[HandleKey] Assign a SerialClient in the Inspector.");
            return;
        }

        serialClient.Close();

        if (serialClient.Open(access))
            Debug.Log($"Serial port open for {access}.");
        else
            Debug.LogWarning($"Could not open the serial port for {access}.");
    }

    void SendTrigger()
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
