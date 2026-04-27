using UnityEngine;

public class PlayerNoiseEmitter : MonoBehaviour
{
    public MicSpectrum micSpectrum;

    [Header("Noise Tuning")]
    public float footstepDecay = 1.8f;
    public float voiceWeight = 1.1f;
    public float movementWeight = 0.35f;
    public float crouchNoiseMultiplier = 0.45f;
    public float sprintNoiseMultiplier = 1.35f;
    public float minimumHearingRadius = 6f;
    public float maximumHearingRadius = 60f;
    public float noiseMemoryDuration = 1.2f;

    public float CurrentNoise { get; private set; }
    public float CurrentHearingRadius => Mathf.Lerp(minimumHearingRadius, maximumHearingRadius, CurrentNoise);
    public Vector3 LastNoisePosition { get; private set; }
    public bool HasRecentNoise => recentNoiseTimer > 0f && CurrentNoise > 0.02f;

    float transientFootstepNoise;
    float movementNoise;
    bool isSprinting;
    bool isCrouching;
    float recentNoiseTimer;

    void Start()
    {
        if (micSpectrum == null)
            micSpectrum = FindObjectOfType<MicSpectrum>();
    }

    void Update()
    {
        if (!EchoArchitectGameState.IsGameplayActive)
        {
            CurrentNoise = 0f;
            transientFootstepNoise = 0f;
            movementNoise = 0f;
            recentNoiseTimer = 0f;
            return;
        }

        transientFootstepNoise = Mathf.MoveTowards(transientFootstepNoise, 0f, footstepDecay * Time.deltaTime);
        recentNoiseTimer = Mathf.Max(0f, recentNoiseTimer - Time.deltaTime);

        float voiceNoise = micSpectrum != null ? micSpectrum.GetSpeechEnergy() * voiceWeight : 0f;
        float postureMultiplier = isCrouching ? crouchNoiseMultiplier : (isSprinting ? sprintNoiseMultiplier : 1f);

        CurrentNoise = Mathf.Clamp01(Mathf.Max(transientFootstepNoise, movementNoise * postureMultiplier, voiceNoise));

        if (CurrentNoise > 0.02f)
        {
            LastNoisePosition = transform.position;
            recentNoiseTimer = noiseMemoryDuration;
        }
    }

    public void EmitFootstep(float strength)
    {
        transientFootstepNoise = Mathf.Clamp01(Mathf.Max(transientFootstepNoise, strength));
        LastNoisePosition = transform.position;
        recentNoiseTimer = noiseMemoryDuration;
    }

    public void SetMovementState(Vector3 moveDirection, float moveSpeed, bool grounded, bool sprinting, bool crouching)
    {
        isSprinting = sprinting;
        isCrouching = crouching;

        if (!grounded)
        {
            movementNoise = 0.12f;
            return;
        }

        float movementAmount = Mathf.Clamp01(moveDirection.magnitude);
        float normalizedSpeed = Mathf.InverseLerp(0f, 7f, moveSpeed);
        movementNoise = movementAmount * normalizedSpeed * movementWeight;
    }
}
