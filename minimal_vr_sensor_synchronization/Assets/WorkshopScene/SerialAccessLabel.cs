using UnityEngine;

/// <summary>
/// Draws the SerialClient's current access mode in the top-left corner of the
/// screen. Add this component to a GameObject, then drag the SerialClient
/// component into the Serial Client field in the Inspector.
/// </summary>
public class SerialAccessLabel : MonoBehaviour
{
    [Header("Serial connection")]
    [Tooltip("The SerialClient whose access mode is displayed.")]
    [SerializeField] SerialClient serialClient;

    [Header("Appearance")]
    [Min(6)]
    [SerializeField] int fontSize = 32;

    [Tooltip("Distance from the top-left corner of the screen, in pixels.")]
    [Min(0f)]
    [SerializeField] float margin = 10f;

    GUIStyle labelStyle;

    void Start()
    {
        if (serialClient == null)
            Debug.LogError("[SerialAccessLabel] Assign a SerialClient in the Inspector.");
    }

    void OnGUI()
    {
        if (serialClient == null)
            return;

        // Styles cannot be built in Awake: GUI.skin exists only inside OnGUI.
        if (labelStyle == null)
            labelStyle = new GUIStyle(GUI.skin.label);

        labelStyle.fontSize = fontSize;

        SerialAccess access = serialClient.Access;
        string text = access == SerialAccess.None ? "Closed" : access.ToString();
        text = "Serial: " + text;

        Vector2 size = labelStyle.CalcSize(new GUIContent(text));
        var area = new Rect(margin, margin, size.x, size.y);

        // Draw a dark copy behind the light one so the text stays readable
        // over both bright and dark scenes, without needing any textures.
        Color previousColor = GUI.color;

        GUI.color = Color.black;
        GUI.Label(new Rect(area.x + 1f, area.y + 1f, area.width, area.height), text, labelStyle);

        GUI.color = Color.white;
        GUI.Label(area, text, labelStyle);

        GUI.color = previousColor;
    }
}
