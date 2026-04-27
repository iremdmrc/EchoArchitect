using Cysharp.Threading.Tasks;
using Genies.Sdk;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EchoArchitectGeniesPlayerAvatar : MonoBehaviour
{
    const string MainScenePath = "Assets/Main.unity";

    Transform player;
    Transform cameraTransform;
    Transform avatarAnchor;
    ManagedAvatar loadedAvatar;
    bool isLoading;
    bool eventsRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInstance()
    {
        if (SceneManager.GetActiveScene().path != MainScenePath)
            return;

        if (FindObjectOfType<EchoArchitectGeniesPlayerAvatar>() != null)
            return;

        GameObject root = new GameObject("EchoArchitectGeniesPlayerAvatar");
        root.AddComponent<EchoArchitectGeniesPlayerAvatar>();
    }

    async void Start()
    {
        if (SceneManager.GetActiveScene().path != MainScenePath)
            return;

        await AvatarSdk.InitializeAsync();
        RegisterEvents();
        ResolveSceneReferences();

        if (AvatarSdk.IsLoggedIn)
            LoadAvatarForPlayer().Forget();
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().path != MainScenePath)
            return;

        ResolveSceneReferences();
        UpdateAnchorPlacement();
        UpdateAvatarVisibility();
    }

    void RegisterEvents()
    {
        if (eventsRegistered)
            return;

        eventsRegistered = true;
        AvatarSdk.Events.UserLoggedIn += OnUserLoggedIn;
        AvatarSdk.Events.UserLoggedOut += OnUserLoggedOut;
    }

    void ResolveSceneReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (cameraTransform == null && player != null)
        {
            Camera cam = player.GetComponentInChildren<Camera>(true);
            if (cam != null)
                cameraTransform = cam.transform;
        }

        if (avatarAnchor == null && player != null)
        {
            Transform existing = player.Find("GeniesAvatarAnchor");
            if (existing != null)
            {
                avatarAnchor = existing;
            }
            else
            {
                GameObject anchor = new GameObject("GeniesAvatarAnchor");
                avatarAnchor = anchor.transform;
                avatarAnchor.SetParent(player);
                avatarAnchor.localPosition = Vector3.zero;
                avatarAnchor.localRotation = Quaternion.identity;
            }
        }
    }

    void UpdateAnchorPlacement()
    {
        if (avatarAnchor == null || player == null)
            return;

        if (EchoArchitectGameState.IsGameplayActive)
        {
            avatarAnchor.localPosition = new Vector3(0f, -0.92f, 0.08f);
            avatarAnchor.localRotation = Quaternion.identity;
            return;
        }

        Transform lookSource = cameraTransform != null ? cameraTransform : player;
        Vector3 forward = lookSource.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;

        forward.Normalize();
        avatarAnchor.position = player.position + (forward * 3f) + (Vector3.down * 0.95f);
        avatarAnchor.rotation = Quaternion.LookRotation(-forward, Vector3.up);
    }

    void UpdateAvatarVisibility()
    {
        if (loadedAvatar == null || loadedAvatar.Root == null)
            return;

        bool isVisible = !EchoArchitectGameState.IsGameplayActive;
        Renderer[] renderers = loadedAvatar.Root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = isVisible;
    }

    void OnUserLoggedIn()
    {
        LoadAvatarForPlayer().Forget();
    }

    void OnUserLoggedOut()
    {
        DisposeLoadedAvatar();
    }

    async UniTaskVoid LoadAvatarForPlayer()
    {
        if (isLoading)
            return;

        ResolveSceneReferences();
        if (avatarAnchor == null)
            return;

        isLoading = true;
        try
        {
            DisposeLoadedAvatar();

            loadedAvatar = await AvatarSdk.LoadAvatarAsync(new LoadAvatarOptions.User
            {
                Parent = avatarAnchor
            });

            if (loadedAvatar == null || loadedAvatar.Root == null)
                return;

            loadedAvatar.Root.transform.localPosition = Vector3.zero;
            loadedAvatar.Root.transform.localRotation = Quaternion.identity;
            loadedAvatar.Root.transform.localScale = Vector3.one;
            UpdateAnchorPlacement();
            UpdateAvatarVisibility();
        }
        finally
        {
            isLoading = false;
        }
    }

    void DisposeLoadedAvatar()
    {
        if (loadedAvatar == null)
            return;

        loadedAvatar.Dispose();
        loadedAvatar = null;
    }

    void OnDestroy()
    {
        if (eventsRegistered)
        {
            AvatarSdk.Events.UserLoggedIn -= OnUserLoggedIn;
            AvatarSdk.Events.UserLoggedOut -= OnUserLoggedOut;
        }

        DisposeLoadedAvatar();
    }
}
