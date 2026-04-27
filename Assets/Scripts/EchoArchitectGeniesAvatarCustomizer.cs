using System.Reflection;
using Cysharp.Threading.Tasks;
using Genies.Sdk;
using UnityEngine;

public class EchoArchitectGeniesAvatarCustomizer : MonoBehaviour
{
    const bool UseGeniesRuntimePreview = false;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int TintColorId = Shader.PropertyToID("_TintColor");

    struct BodyChoice
    {
        public Vector3 Scale;
        public string Label;
    }

    readonly BodyChoice[] bodyChoices =
    {
        new BodyChoice { Scale = new Vector3(1.02f, 1.10f, 1.02f), Label = "Slim" },
        new BodyChoice { Scale = new Vector3(1.12f, 1.18f, 1.12f), Label = "Medium" },
        new BodyChoice { Scale = new Vector3(1.18f, 1.28f, 1.18f), Label = "Tall" },
        new BodyChoice { Scale = new Vector3(1.28f, 1.16f, 1.28f), Label = "Heavy" }
    };

    readonly Color[] skinChoices =
    {
        new Color(0.97f, 0.82f, 0.69f),
        new Color(0.83f, 0.66f, 0.50f),
        new Color(0.65f, 0.47f, 0.33f),
        new Color(0.43f, 0.29f, 0.20f)
    };

    readonly Color[] hairChoices =
    {
        new Color(0.12f, 0.09f, 0.07f),
        new Color(0.29f, 0.20f, 0.11f),
        new Color(0.58f, 0.46f, 0.22f),
        new Color(0.68f, 0.17f, 0.16f),
        new Color(0.82f, 0.82f, 0.86f)
    };

    Transform player;
    GameObject previewRoot;
    Transform previewAnchor;
    ManagedAvatar previewAvatar;
    bool isLoading;
    bool demoSdkReady;
    bool loadAttempted;
    string status = "Genies demo avatar is not loaded yet.";
    int bodyIndex = 1;
    int skinIndex = 1;
    int hairIndex;

    public void Setup()
    {
        if (!IsSupportedScene())
            return;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        EnsurePreviewObjects();
        status = UseGeniesRuntimePreview
            ? "Genies demo avatar is not loaded yet."
            : "Genies runtime preview is disabled in the main game scene to prevent SDK crashes.";
    }

    public void OpenCustomizer()
    {
        if (!IsSupportedScene())
            return;

        EnsurePreviewObjects();
        SetPreviewVisible(true);
        if (UseGeniesRuntimePreview)
            LoadDemoAvatarIfNeeded().Forget();
    }

    public void SetPreviewVisible(bool isVisible)
    {
        if (!IsSupportedScene())
            return;

        EnsurePreviewObjects();
        previewRoot.SetActive(isVisible);
    }

    public void DrawGui(float x, float y, float width)
    {
        GUI.Label(new Rect(x, y, width, 44f), "Genies Character");
        GUI.Label(new Rect(x, y + 28f, width, 62f), status);

        string reloadLabel = UseGeniesRuntimePreview
            ? (isLoading ? "Loading..." : "Reload Demo Avatar")
            : "Genies Preview Disabled";
        if (GUI.Button(new Rect(x, y + 96f, width, 34f), reloadLabel) && UseGeniesRuntimePreview)
            LoadDemoAvatarIfNeeded(forceReload: true).Forget();

        DrawSelector(x, y + 146f, width, "Body", bodyChoices[bodyIndex].Label, () => ChangeBody(-1), () => ChangeBody(1));
        DrawSelector(x, y + 186f, width, "Skin", "Tone " + (skinIndex + 1), () => ChangeSkin(-1), () => ChangeSkin(1));
        DrawSelector(x, y + 226f, width, "Hair", "Color " + (hairIndex + 1), () => ChangeHair(-1), () => ChangeHair(1));

        if (GUI.Button(new Rect(x, y + 270f, width, 34f), "Apply Customization"))
            ApplyCustomization().Forget();

        GUI.Label(
            new Rect(x, y + 316f, width, 110f),
            "The Genies SDK package is installed, but its runtime preview is disabled in the gameplay scene " +
            "because the current demo avatar path crashes inside the SDK. Full login and cloud editing can be enabled later.");
    }

    void Update()
    {
        if (!IsSupportedScene() || previewAnchor == null || player == null)
            return;

        Vector3 forward = player.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;

        forward.Normalize();
        previewAnchor.position = player.position + (forward * 4.25f) + (Vector3.down * 0.95f);
        previewAnchor.rotation = Quaternion.LookRotation(-forward, Vector3.up);
    }

    void EnsurePreviewObjects()
    {
        if (previewRoot != null)
            return;

        previewRoot = new GameObject("GeniesPreviewRoot");
        previewAnchor = new GameObject("GeniesPreviewAnchor").transform;
        previewAnchor.SetParent(previewRoot.transform);
        previewAnchor.localPosition = Vector3.zero;

    }

    async UniTaskVoid LoadDemoAvatarIfNeeded(bool forceReload = false)
    {
        if (!UseGeniesRuntimePreview)
        {
            status = "Genies runtime preview is disabled in the main game scene to prevent SDK crashes.";
            return;
        }

        if (isLoading)
            return;

        if (loadAttempted && !forceReload && previewAvatar != null)
            return;

        isLoading = true;
        loadAttempted = true;
        status = "Initializing Genies demo mode...";

        try
        {
            if (forceReload)
                DisposePreviewAvatar();

            if (!demoSdkReady)
                demoSdkReady = await InitializeDemoModeAsync();

            if (!demoSdkReady)
            {
                status = "Genies demo mode could not initialize.";
                return;
            }

            status = "Loading Genies demo avatar...";

            previewAvatar = await AvatarSdk.LoadAvatarAsync(new LoadAvatarOptions.Test
            {
                AvatarName = "EchoArchitectGeniesDemoAvatar",
                Parent = previewAnchor,
                ShowLoadingSilhouette = true
            });

            if (previewAvatar == null || previewAvatar.Root == null)
            {
                status = "Genies demo avatar returned null.";
                return;
            }

            previewAvatar.Root.transform.localPosition = Vector3.zero;
            previewAvatar.Root.transform.localRotation = Quaternion.identity;
            previewAvatar.Root.transform.localScale = Vector3.one * 1.12f;
            await ApplyCustomizationInternal();
            status = "Genies demo avatar loaded successfully.";
        }
        catch (System.Exception ex)
        {
            status = "Genies demo avatar failed: " + ex.Message;
        }
        finally
        {
            isLoading = false;
        }
    }

    bool IsSupportedScene()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == "Assets/Main.unity";
    }

    async UniTask<bool> InitializeDemoModeAsync()
    {
        MethodInfo method = typeof(AvatarSdk).GetMethod(
            "InitializeDemoModeAsync",
            BindingFlags.Static | BindingFlags.NonPublic);

        if (method == null)
            return false;

        object invocationResult = method.Invoke(null, null);
        if (invocationResult is UniTask<bool> task)
            return await task;

        return false;
    }

    void DrawSelector(float x, float y, float width, string label, string value, System.Action previous, System.Action next)
    {
        GUI.Label(new Rect(x, y, width, 22f), label + ": " + value);
        if (GUI.Button(new Rect(x, y + 18f, 44f, 24f), "<"))
            previous();
        if (GUI.Button(new Rect(x + width - 44f, y + 18f, 44f, 24f), ">"))
            next();
    }

    void ChangeBody(int direction)
    {
        bodyIndex = Wrap(bodyIndex + direction, bodyChoices.Length);
    }

    void ChangeSkin(int direction)
    {
        skinIndex = Wrap(skinIndex + direction, skinChoices.Length);
    }

    void ChangeHair(int direction)
    {
        hairIndex = Wrap(hairIndex + direction, hairChoices.Length);
    }

    async UniTaskVoid ApplyCustomization()
    {
        if (!UseGeniesRuntimePreview)
        {
            status = "Customization settings are saved locally, but Genies live preview is disabled for stability.";
            await UniTask.CompletedTask;
            return;
        }

        if (previewAvatar == null || isLoading)
            return;

        status = "Applying customization...";
        try
        {
            await ApplyCustomizationInternal();
            status = "Customization applied successfully.";
        }
        catch (System.Exception ex)
        {
            status = "Customization failed: " + ex.Message;
        }
    }

    async UniTask ApplyCustomizationInternal()
    {
        if (previewAvatar == null)
            return;

        BodyChoice choice = bodyChoices[Mathf.Clamp(bodyIndex, 0, bodyChoices.Length - 1)];
        previewAvatar.Root.transform.localScale = choice.Scale * 1.12f;

        Color skinColor = skinChoices[Mathf.Clamp(skinIndex, 0, skinChoices.Length - 1)];
        Color hairColor = hairChoices[Mathf.Clamp(hairIndex, 0, hairChoices.Length - 1)];
        ApplyColorsToAvatar(previewAvatar.Root, skinColor, hairColor);
        await UniTask.CompletedTask;
    }

    void ApplyColorsToAvatar(GameObject avatarRoot, Color skinColor, Color hairColor)
    {
        Renderer[] renderers = avatarRoot.GetComponentsInChildren<Renderer>(true);
        bool appliedHair = false;
        bool appliedSkin = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            Material[] materials = renderer.materials;
            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];
                if (material == null)
                    continue;

                string rendererName = renderer.name.ToLowerInvariant();
                string lowerName = material.name.ToLowerInvariant();
                bool looksLikeHair =
                    rendererName.Contains("hair") ||
                    rendererName.Contains("brow") ||
                    rendererName.Contains("lash") ||
                    lowerName.Contains("hair") ||
                    lowerName.Contains("brow") ||
                    lowerName.Contains("lash");
                bool looksLikeSkin =
                    rendererName.Contains("skin") ||
                    rendererName.Contains("body") ||
                    rendererName.Contains("face") ||
                    rendererName.Contains("head") ||
                    rendererName.Contains("arm") ||
                    rendererName.Contains("leg") ||
                    lowerName.Contains("skin") ||
                    lowerName.Contains("body") ||
                    lowerName.Contains("face") ||
                    lowerName.Contains("head");

                if (looksLikeHair)
                {
                    SetMaterialColor(material, hairColor);
                    appliedHair = true;
                }
                else if (looksLikeSkin)
                {
                    SetMaterialColor(material, skinColor);
                    appliedSkin = true;
                }
            }
        }

        // Fallback for Genies demo avatars whose runtime material names do not expose clear categories.
        if (!appliedSkin || !appliedHair)
            ApplyFallbackColors(renderers, skinColor, hairColor, appliedSkin, appliedHair);
    }

    void ApplyFallbackColors(Renderer[] renderers, Color skinColor, Color hairColor, bool appliedSkin, bool appliedHair)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            string rendererName = renderer.name.ToLowerInvariant();
            Material[] materials = renderer.materials;

            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];
                if (material == null)
                    continue;

                if (!appliedHair && (rendererName.Contains("head") || rendererName.Contains("top") || rendererName.Contains("cap")))
                    SetMaterialColor(material, hairColor);

                if (!appliedSkin && (rendererName.Contains("mesh") || rendererName.Contains("avatar") || rendererName.Contains("genie")))
                    SetMaterialColor(material, skinColor);
            }
        }
    }

    void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty(BaseColorId))
            material.SetColor(BaseColorId, color);
        if (material.HasProperty(ColorId))
            material.SetColor(ColorId, color);
        if (material.HasProperty(TintColorId))
            material.SetColor(TintColorId, color);
    }

    void SetAvatarVisibility(bool isVisible)
    {
        if (previewAvatar == null || previewAvatar.Root == null)
            return;

        Renderer[] renderers = previewAvatar.Root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = isVisible;
    }

    int Wrap(int value, int count)
    {
        if (count <= 0)
            return 0;

        int wrapped = value % count;
        return wrapped < 0 ? wrapped + count : wrapped;
    }

    void DisposePreviewAvatar()
    {
        if (previewAvatar == null)
            return;

        previewAvatar.Dispose();
        previewAvatar = null;
    }

    public void ShowCustomizationPreview(bool isVisible)
    {
        SetPreviewVisible(isVisible);
        SetAvatarVisibility(isVisible);
    }

    void OnDestroy()
    {
        DisposePreviewAvatar();
    }
}
