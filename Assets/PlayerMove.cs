using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 6.75f;
    public float crouchSpeed = 2.8f;
    public float gravity = -9.81f * 2f;
    public float jumpHeight = 1.6f;

    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundMask;
    public PlayerNoiseEmitter noiseEmitter;

    [Header("Footsteps")]
    public float stepDistance = 2.1f;
    public float sprintStepDistance = 1.45f;
    public float crouchStepDistance = 2.8f;

    CharacterController cc;
    Vector3 velocity;
    bool isGrounded;
    float stepProgress;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        if (noiseEmitter == null)
            noiseEmitter = GetComponent<PlayerNoiseEmitter>();
    }

    void Update()
    {
        if (!EchoArchitectGameState.IsGameplayActive)
            return;

        if (groundCheck == null)
            return;

        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);

        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool isMoving = Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f;
        bool isCrouching =
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl) ||
            Input.GetKey(KeyCode.C);
        bool isSprinting =
            !isCrouching &&
            (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        float currentSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

        Vector3 move = (transform.right * h) + (transform.forward * v);
        move = move.normalized;

        cc.Move(move * currentSpeed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            noiseEmitter?.EmitFootstep(0.85f);
        }

        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);

        UpdateFootsteps(isMoving, currentSpeed, isSprinting, isCrouching);
        noiseEmitter?.SetMovementState(move, currentSpeed, isGrounded, isSprinting, isCrouching);
    }

    void UpdateFootsteps(bool isMoving, float currentSpeed, bool isSprinting, bool isCrouching)
    {
        if (!isGrounded || !isMoving)
        {
            stepProgress = 0f;
            return;
        }

        stepProgress += currentSpeed * Time.deltaTime;
        float targetStepDistance = isCrouching ? crouchStepDistance : (isSprinting ? sprintStepDistance : stepDistance);

        if (stepProgress < targetStepDistance)
            return;

        stepProgress = 0f;
        float footstepStrength = isCrouching ? 0.28f : (isSprinting ? 0.78f : 0.5f);
        noiseEmitter?.EmitFootstep(footstepStrength);
    }
}
