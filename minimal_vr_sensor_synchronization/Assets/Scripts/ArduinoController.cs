using UnityEngine;

public class ArduinoController : MonoBehaviour
{
    [SerializeField] SerialClient serialClient;

    [Header("Circular motion")]
    [Tooltip("Radius of the circle the cube travels, in world units.")]
    [Min(0f)]
    [SerializeField] float circleRadius = 0.5f;

    [Tooltip("How far around the circle each received \"1\" moves the cube. The AtomS3 " +
             "sends \"1\" continuously while its button is held, so keep this small.")]
    [SerializeField] float degreesPerMessage = 0.2f;

    Vector3 circleCentre;
    float angleDegrees;

    void Awake()
    {
        circleCentre = transform.position;
        transform.position = PositionOnCircle();
    }

    Vector3 PositionOnCircle()
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        return circleCentre + new Vector3(
            Mathf.Cos(radians) * circleRadius,
            Mathf.Sin(radians) * circleRadius,
            0f);
    }

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
        {
            // handle message and do something to Unity Objects
            angleDegrees += degreesPerMessage;
            transform.position = PositionOnCircle();
        }
    }
}