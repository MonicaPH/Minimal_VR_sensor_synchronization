using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Starts reaction-time trials and receives their results from an AtomS3.
/// Add this component and SerialClient to GameObjects, then drag the
/// SerialClient component into the Serial Client field in the Inspector.
/// </summary>
public class ReactionTimeController : MonoBehaviour
{
    [Header("Serial connection")]
    [Tooltip("The bidirectional SerialClient connected to the AtomS3.")]
    [SerializeField] SerialClient serialClient;

    [Tooltip("Open the SerialClient when this component starts. Off by default, so the " +
             "access mode is chosen deliberately at runtime.")]
    [SerializeField] bool openOnStart;

    [Header("Keyboard shortcuts")]
    [SerializeField] Key startTrialKey = Key.Space;

    [Min(20f)]
    [SerializeField] float toneFrequencyHz = 1000f;

    [Min(0.01f)]
    [SerializeField] float toneDurationSeconds = 0.08f;

    [Min(0f)]
    [SerializeField] float toneVolume = 0.25f;

    [Header("On-screen result")]
    [Tooltip("How long the newest result stays on screen, in seconds.")]
    [Min(0f)]
    [SerializeField] float resultDisplaySeconds = 2f;

    [Min(6)]
    [SerializeField] int fontSize = 32;

    AudioSource toneSource;
    AudioClip toneClip;
    GUIStyle resultStyle;

    // Unscaled, so a changed Time.timeScale cannot stretch or freeze the display.
    float hideResultAtTime = float.NegativeInfinity;

    /// <summary>Raised on Unity's main thread with the result in milliseconds.</summary>
    public event Action<double> ReactionTimeReceived;

    /// <summary>The newest result, in milliseconds; NaN until the first result.</summary>
    public double LastReactionTimeMilliseconds { get; private set; } = double.NaN;

    void Awake()
    {
        toneSource = GetComponent<AudioSource>();
        if (toneSource == null)
            toneSource = gameObject.AddComponent<AudioSource>();

        toneSource.playOnAwake = false;
        toneClip = CreateToneClip();
    }

    void OnEnable()
    {
        if (serialClient != null)
            serialClient.LineReceived += OnSerialLine;
    }

    void Start()
    {
        if (serialClient == null)
        {
            Debug.LogError("[ReactionTimeController] Assign a SerialClient in the Inspector.");
            return;
        }

        if (openOnStart && !serialClient.Open())
            return;
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard[startTrialKey].wasPressedThisFrame)
            StartTrial();
    }

    /// <summary>Sends the timing-critical, single-byte trial command.</summary>
    public bool StartTrial()
    {
        // A trial is a request and a reply, so write-only access cannot run one.
        if (serialClient == null || serialClient.Access != SerialAccess.ReadWrite)
        {
            Debug.LogWarning(
                "[ReactionTimeController] A trial needs read-write access to receive its result. " +
                $"Current access: {(serialClient == null ? SerialAccess.None : serialClient.Access)}.");
            return false;
        }

        if (!serialClient.Send(ReactionTimeProtocol.StartTrial))
        {
            Debug.LogWarning("[ReactionTimeController] Cannot start a trial: serial is not connected.");
            return false;
        }

        if (toneSource != null && toneClip != null)
            toneSource.PlayOneShot(toneClip, Mathf.Clamp01(toneVolume));

        return true;
    }

    void OnSerialLine(string line)
    {
        uint reactionMicroseconds;
        if (ReactionTimeProtocol.TryParseReactionTime(line, out reactionMicroseconds))
        {
            LastReactionTimeMilliseconds = reactionMicroseconds / 1000.0;
            Debug.Log(
                $"[ReactionTimeController] Reaction time: " +
                $"{LastReactionTimeMilliseconds:F3} ms");
            hideResultAtTime = Time.unscaledTime + resultDisplaySeconds;
            ReactionTimeReceived?.Invoke(LastReactionTimeMilliseconds);
            return;
        }

        if (ReactionTimeProtocol.IsBusy(line))
        {
            Debug.LogWarning("[ReactionTimeController] The AtomS3 already has an active trial.");
            return;
        }
    }

    void OnGUI()
    {
        // A later result simply pushes the deadline out; nothing needs cancelling.
        if (Time.unscaledTime >= hideResultAtTime)
            return;

        // Styles cannot be built in Awake: GUI.skin exists only inside OnGUI.
        if (resultStyle == null)
            resultStyle = new GUIStyle(GUI.skin.label);

        resultStyle.fontSize = fontSize;

        // Centring the text inside a screen-sized rect keeps it centred at any
        // resolution, and needs no measuring of the string itself.
        resultStyle.alignment = TextAnchor.MiddleCenter;

        string text = $"Reaction: {LastReactionTimeMilliseconds:F3} ms";
        var area = new Rect(0f, 0f, Screen.width, Screen.height);

        // Draw a dark copy behind the light one so the text stays readable
        // over both bright and dark scenes, without needing any textures.
        Color previousColor = GUI.color;

        GUI.color = Color.black;
        GUI.Label(new Rect(area.x + 1f, area.y + 1f, area.width, area.height), text, resultStyle);

        GUI.color = Color.white;
        GUI.Label(area, text, resultStyle);

        GUI.color = previousColor;
    }

    AudioClip CreateToneClip()
    {
        int sampleRate = Math.Max(8000, AudioSettings.outputSampleRate);
        int sampleCount = Math.Max(1, Mathf.CeilToInt(toneDurationSeconds * sampleRate));
        var samples = new float[sampleCount];
        const float fadeSeconds = 0.005f;

        for (int index = 0; index < sampleCount; index++)
        {
            float time = index / (float)sampleRate;
            float fadeIn = Mathf.Clamp01(time / fadeSeconds);
            float fadeOut = Mathf.Clamp01((toneDurationSeconds - time) / fadeSeconds);
            float envelope = Mathf.Min(fadeIn, fadeOut);
            samples[index] = Mathf.Sin(2f * Mathf.PI * toneFrequencyHz * time) * envelope;
        }

        AudioClip clip = AudioClip.Create(
            "Reaction time beep", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void OnDisable()
    {
        if (serialClient != null)
            serialClient.LineReceived -= OnSerialLine;
    }

    void OnDestroy()
    {
        if (toneClip != null)
            Destroy(toneClip);
    }
}
