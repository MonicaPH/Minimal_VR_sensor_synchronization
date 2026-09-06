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

    [Tooltip("Open the SerialClient when this component starts.")]
    [SerializeField] bool openOnStart = true;

    [Header("Keyboard shortcuts")]
    [SerializeField] Key startTrialKey = Key.Space;

    [Min(20f)]
    [SerializeField] float toneFrequencyHz = 1000f;

    [Min(0.01f)]
    [SerializeField] float toneDurationSeconds = 0.08f;

    [Min(0f)]
    [SerializeField] float toneVolume = 0.25f;

    AudioSource toneSource;
    AudioClip toneClip;

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
        if (serialClient == null || !serialClient.Send(ReactionTimeProtocol.StartTrial))
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
            ReactionTimeReceived?.Invoke(LastReactionTimeMilliseconds);
            return;
        }

        if (ReactionTimeProtocol.IsBusy(line))
        {
            Debug.LogWarning("[ReactionTimeController] The AtomS3 already has an active trial.");
            return;
        }
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
