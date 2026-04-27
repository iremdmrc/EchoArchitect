using UnityEngine;

public class VoiceVisibility : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int TintColorId = Shader.PropertyToID("_TintColor");

    public MicSpectrum micSpectrum;
    public PlayerNoiseEmitter noiseEmitter;
    public Renderer playerRenderer;

    [Header("Alpha")]
    public float hiddenAlpha = 0.15f;
    public float visibleAlpha = 1f;

    [Header("Calibration")]
    public float calibrationTime = 2f;

    [Header("Speech Detection")]
    public float extraThreshold = 0.08f;
    public float requiredSpeakTime = 0.18f;
    public float releaseSpeed = 2f;

    float baseline;
    float calibrationTimer;
    bool calibrated;
    float speakMeter;
    float visibility = 0.15f;

    void Start()
    {
        if (playerRenderer == null)
            playerRenderer = GetComponent<Renderer>();

        if (noiseEmitter == null)
            noiseEmitter = GetComponent<PlayerNoiseEmitter>();

        if (playerRenderer != null)
            ApplyAlpha(hiddenAlpha);
    }

    void Update()
    {
        if (playerRenderer == null)
            return;

        if (micSpectrum == null)
            micSpectrum = FindObjectOfType<MicSpectrum>();

        float speechValue = micSpectrum != null ? micSpectrum.GetSpeechEnergy() : 0f;
        if (noiseEmitter != null)
            speechValue = Mathf.Max(speechValue, noiseEmitter.CurrentNoise);

        if (!calibrated)
        {
            calibrationTimer += Time.deltaTime;
            baseline = Mathf.Lerp(baseline, speechValue, Time.deltaTime * 3f);

            if (calibrationTimer >= calibrationTime)
                calibrated = true;

            ApplyAlpha(hiddenAlpha);
            return;
        }

        float threshold = baseline + extraThreshold;

        if (speechValue > threshold)
            speakMeter += Time.deltaTime;
        else
            speakMeter -= Time.deltaTime * 2.5f;

        speakMeter = Mathf.Clamp(speakMeter, 0f, requiredSpeakTime);

        if (speakMeter >= requiredSpeakTime)
            visibility = 1f;
        else
            visibility -= releaseSpeed * Time.deltaTime;

        visibility = Mathf.Clamp01(visibility);
        float alpha = Mathf.Lerp(hiddenAlpha, visibleAlpha, visibility);
        ApplyAlpha(alpha);
    }

    void ApplyAlpha(float alpha)
    {
        Material mat = playerRenderer.material;
        if (TrySetAlpha(mat, BaseColorId, alpha))
            return;

        if (TrySetAlpha(mat, ColorId, alpha))
            return;

        TrySetAlpha(mat, TintColorId, alpha);
    }

    bool TrySetAlpha(Material material, int propertyId, float alpha)
    {
        if (!material.HasProperty(propertyId))
            return false;

        Color color = material.GetColor(propertyId);
        color.a = alpha;
        material.SetColor(propertyId, color);
        return true;
    }

    public float GetVisibility()
    {
        return visibility;
    }
}
