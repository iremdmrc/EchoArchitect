using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EchoArchitectGeniesCreatorBridge : MonoBehaviour
{
    const string CreatorSceneName = "CreatingCustomEditor";
    const string MainSceneName = "Main";

    Button saveButton;
    Canvas overlayCanvas;
    bool saveButtonBound;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInstance()
    {
        if (FindObjectOfType<EchoArchitectGeniesCreatorBridge>() != null)
            return;

        GameObject bridge = new GameObject("EchoArchitectGeniesCreatorBridge");
        DontDestroyOnLoad(bridge);
        bridge.AddComponent<EchoArchitectGeniesCreatorBridge>();
    }

    void Update()
    {
        if (!IsCreatorScene())
        {
            DestroyOverlay();
            return;
        }

        EnsureOverlay();
        TryBindSaveButton();

        if (Input.GetKeyDown(KeyCode.Escape))
            ReturnToMainScene();
    }

    bool IsCreatorScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        return scene.name == CreatorSceneName || scene.path.Contains(CreatorSceneName);
    }

    void EnsureOverlay()
    {
        if (overlayCanvas != null)
            return;

        GameObject canvasObject = new GameObject("EchoArchitectCreatorOverlay");
        canvasObject.transform.SetParent(transform);

        overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = short.MaxValue;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        CreatePanel(canvasObject.transform);
    }

    void CreatePanel(Transform parent)
    {
        GameObject panel = CreateUiObject("Panel", parent);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.68f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(20f, -20f);
        panelRect.sizeDelta = new Vector2(260f, 82f);

        CreateButton(panel.transform, "Return To Main Menu", new Vector2(12f, -12f), ReturnToMainScene);
        CreateButton(panel.transform, "Play Game", new Vector2(12f, -46f), PlayGame);
    }

    void CreateButton(Transform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateUiObject(label, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.94f, 0.94f, 0.94f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(action);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(236f, 26f);

        CreateText("Label", buttonObject.transform, label, 14, Vector2.zero, rect.sizeDelta, TextAlignmentOptions.Center, Color.black);
    }

    void CreateText(string name, Transform parent, string text, int fontSize, Vector2 anchoredPosition, Vector2 sizeDelta, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft, Color? color = null)
    {
        GameObject textObject = CreateUiObject(name, parent);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color ?? Color.white;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = 5;
        go.AddComponent<RectTransform>();
        return go;
    }

    void DestroyOverlay()
    {
        if (overlayCanvas == null)
            return;

        Destroy(overlayCanvas.gameObject);
        overlayCanvas = null;
        saveButton = null;
        saveButtonBound = false;
    }

    void TryBindSaveButton()
    {
        if (saveButtonBound)
            return;

        Button[] buttons = FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button candidate = buttons[i];
            if (candidate == null)
                continue;

            string objectName = candidate.gameObject.name;
            TMP_Text tmp = candidate.GetComponentInChildren<TMP_Text>(true);
            string label = tmp != null ? tmp.text : string.Empty;

            if (!string.Equals(objectName, "Save Button", System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(label, "Save", System.StringComparison.OrdinalIgnoreCase))
                continue;

            saveButton = candidate;
            saveButton.onClick.AddListener(OnSaveClicked);
            saveButtonBound = true;
            break;
        }
    }

    void OnSaveClicked()
    {
        CancelInvoke(nameof(PlayGame));
        Invoke(nameof(PlayGame), 0.25f);
    }

    void ReturnToMainScene()
    {
        SceneManager.LoadScene(MainSceneName);
    }

    void PlayGame()
    {
        EchoArchitectGameState.RequestAutoStartNextRun();
        SceneManager.LoadScene(MainSceneName);
    }

    void OnDestroy()
    {
        if (saveButton != null)
            saveButton.onClick.RemoveListener(OnSaveClicked);

        DestroyOverlay();
    }
}
