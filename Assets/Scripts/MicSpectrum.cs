using UnityEngine;

public class MicSpectrum : MonoBehaviour
{
    [Header("Mic")]
    public string micDevice;
    public int sampleRate = 44100;

    [Header("Bands (0..1)")]
    [Range(0, 1)] public float low;
    [Range(0, 1)] public float mid;
    [Range(0, 1)] public float high;

    [Header("Sensitivity")]
    public float gain = 80f;
    public float smooth = 10f;

    AudioClip micClip;
    readonly float[] samples = new float[512];
    float overallLevel;

    void Start()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("No microphone detected by Unity. Footsteps will still drive the monster.");
            enabled = false;
            return;
        }

        if (string.IsNullOrEmpty(micDevice))
            micDevice = Microphone.devices[0];

        micClip = Microphone.Start(micDevice, true, 10, sampleRate);

        float startTime = Time.time;
        while (Microphone.GetPosition(micDevice) <= 0)
        {
            if (Time.time - startTime > 2f)
            {
                Debug.LogWarning("Microphone did not start in time. Voice tracking disabled.");
                enabled = false;
                return;
            }
        }
    }

    void Update()
    {
        if (micClip == null)
            return;

        int micPosition = Microphone.GetPosition(micDevice) - samples.Length;
        if (micPosition < 0)
            return;

        micClip.GetData(samples, micPosition);

        float rms = RMS(samples);
        float targetLow = Mathf.Clamp01(rms * gain * 0.6f);
        float targetMid = Mathf.Clamp01(rms * gain * 1.4f);
        float targetHigh = Mathf.Clamp01(rms * gain * 0.8f);

        low = Mathf.Lerp(low, targetLow, Time.deltaTime * smooth);
        mid = Mathf.Lerp(mid, targetMid, Time.deltaTime * smooth);
        high = Mathf.Lerp(high, targetHigh, Time.deltaTime * smooth);
        overallLevel = Mathf.Lerp(overallLevel, Mathf.Clamp01(rms * gain), Time.deltaTime * smooth);
    }

    float RMS(float[] data)
    {
        float sum = 0f;
        for (int i = 0; i < data.Length; i++)
            sum += data[i] * data[i];

        return Mathf.Sqrt(sum / data.Length);
    }

    public float GetSpeechEnergy()
    {
        return Mathf.Clamp01((mid * 1.15f) + (high * 0.25f));
    }

    public float GetOverallLevel()
    {
        return overallLevel;
    }
}
