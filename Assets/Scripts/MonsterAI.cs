using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MonsterAI : MonoBehaviour
{
    public Transform player;
    public PlayerNoiseEmitter noiseEmitter;

    [Header("Movement")]
    public float patrolSpeed = 2.5f;
    public float chaseSpeed = 4.75f;
    public float hearingSlack = 4f;
    public float catchDistance = 1.6f;
    public float directAwarenessDistance = 7f;

    [Header("Search")]
    public float memoryDuration = 4f;
    public float stopDistance = 0.9f;
    public float turnSpeed = 6f;

    [Header("Animation")]
    public string idleState = "idle1";
    public string chaseState = "run1";
    public string attackState = "attack1";
    public float animationBlendDuration = 0.15f;
    public AnimationClip idleClip;
    public AnimationClip chaseClip;
    public AnimationClip attackClip;

    Vector3 startPos;
    Vector3 investigationTarget;
    float heardTimer;
    Animator animator;
    string currentState;
    PlayableGraph animationGraph;
    AnimationMixerPlayable animationMixer;
    int currentClipIndex = -1;

    void Start()
    {
        startPos = transform.position;
        investigationTarget = startPos;
        animator = GetComponent<Animator>();
        LoadFallbackClipsIfNeeded();
        InitializeClipGraph();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (noiseEmitter == null)
            noiseEmitter = FindObjectOfType<PlayerNoiseEmitter>();
    }

    void OnDestroy()
    {
        if (animationGraph.IsValid())
            animationGraph.Destroy();
    }

    void Update()
    {
        if (!EchoArchitectGameState.IsGameplayActive || player == null)
            return;

        if (noiseEmitter == null)
            noiseEmitter = FindObjectOfType<PlayerNoiseEmitter>();

        if (noiseEmitter != null)
        {
            float distanceToNoise = Vector3.Distance(transform.position, noiseEmitter.LastNoisePosition);
            bool hearsPlayer =
                noiseEmitter.HasRecentNoise &&
                noiseEmitter.CurrentNoise > 0.03f &&
                distanceToNoise <= (noiseEmitter.CurrentHearingRadius + hearingSlack);

            if (hearsPlayer)
            {
                heardTimer = memoryDuration;
                investigationTarget = noiseEmitter.LastNoisePosition;
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= directAwarenessDistance)
        {
            heardTimer = Mathf.Max(heardTimer, 0.35f);
            investigationTarget = player.position;
        }

        bool isMoving;
        if (heardTimer > 0f)
        {
            heardTimer -= Time.deltaTime;
            isMoving = MoveTowards(investigationTarget, chaseSpeed);
            PlayState(isMoving ? chaseState : idleState);
        }
        else
        {
            isMoving = MoveTowards(startPos, patrolSpeed);
            PlayState(isMoving ? chaseState : idleState);
        }

        if (distanceToPlayer <= catchDistance)
        {
            PlayState(attackState);
            EchoArchitectGameState.SetCaught();
        }
    }

    bool MoveTowards(Vector3 target, float speed)
    {
        Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);
        Vector3 flatOffset = flatTarget - transform.position;

        if (flatOffset.sqrMagnitude <= stopDistance * stopDistance)
            return false;

        Vector3 direction = flatOffset.normalized;
        transform.position += direction * speed * Time.deltaTime;

        Quaternion desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, turnSpeed * Time.deltaTime);
        return true;
    }

    void PlayState(string stateName)
    {
        if (TryPlayClipState(stateName))
        {
            currentState = stateName;
            return;
        }

        if (animator == null || string.IsNullOrEmpty(stateName) || currentState == stateName)
            return;

        if (!animator.HasState(0, Animator.StringToHash(stateName)))
            return;

        animator.CrossFade(stateName, animationBlendDuration);
        currentState = stateName;
    }

    bool TryPlayClipState(string stateName)
    {
        if (!animationGraph.IsValid() || string.IsNullOrEmpty(stateName) || currentState == stateName)
            return false;

        int clipIndex = GetClipIndexForState(stateName);
        if (clipIndex < 0 || clipIndex == currentClipIndex)
            return clipIndex >= 0;

        for (int i = 0; i < animationMixer.GetInputCount(); i++)
            animationMixer.SetInputWeight(i, i == clipIndex ? 1f : 0f);

        currentClipIndex = clipIndex;
        return true;
    }

    int GetClipIndexForState(string stateName)
    {
        if (stateName == idleState)
            return idleClip != null ? 0 : -1;
        if (stateName == chaseState)
            return chaseClip != null ? 1 : -1;
        if (stateName == attackState)
            return attackClip != null ? 2 : -1;

        return -1;
    }

    void InitializeClipGraph()
    {
        if (animator == null || idleClip == null || chaseClip == null || attackClip == null)
            return;

        animator.runtimeAnimatorController = null;
        animationGraph = PlayableGraph.Create("MonsterAI_AnimationGraph");
        animationGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        var output = AnimationPlayableOutput.Create(animationGraph, "Animation", animator);
        animationMixer = AnimationMixerPlayable.Create(animationGraph, 3);
        output.SetSourcePlayable(animationMixer);

        ConnectClip(0, idleClip);
        ConnectClip(1, chaseClip);
        ConnectClip(2, attackClip);

        currentClipIndex = -1;
        animationGraph.Play();
        TryPlayClipState(idleState);
    }

    void ConnectClip(int index, AnimationClip clip)
    {
        var clipPlayable = AnimationClipPlayable.Create(animationGraph, clip);
        clipPlayable.SetApplyFootIK(false);
        clipPlayable.SetDuration(double.MaxValue);
        animationGraph.Connect(clipPlayable, 0, animationMixer, index);
        animationMixer.SetInputWeight(index, 0f);
    }

    void LoadFallbackClipsIfNeeded()
    {
#if UNITY_EDITOR
        if (idleClip == null)
            idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/Stylized3DMonster/Monster01/Anim/InPlace_Anim/Monster01_Idle.anim");

        if (chaseClip == null)
            chaseClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/Stylized3DMonster/Monster01/Anim/InPlace_Anim/Monster01_Run_InPlace.anim");

        if (attackClip == null)
            attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/Stylized3DMonster/Monster01/Anim/InPlace_Anim/Monster01_Attack01_InPlace.anim");
#endif
    }
}
