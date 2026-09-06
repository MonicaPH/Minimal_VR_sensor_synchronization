using UnityEngine;

public class ReceiveTrigger : MonoBehaviour
{
    [SerializeField] SerialClient serialClient;

    Vector3 circleCentre;
    float circleRadius = 0.5f;
    float degreesPerMessage = 0.2f;
    float angleDegrees;

    // use this if parent object also has SerialClient attached
    // void OnAwake()
    // {
    //     serialClient = GetComponent<SerialClient>();
    // }

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
        Debug.Log("Arduino sent: " + line);

        // LineReceived runs on Unity's main thread, so modifying Unity objects is safe here.
        if (line == "1")
        {
            // handle message and do something to Unity Objects
            angleDegrees += degreesPerMessage;
            transform.position = PositionOnCircle();
        }
    }
}