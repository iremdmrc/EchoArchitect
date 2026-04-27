using UnityEngine;
using UnityEngine.SceneManagement;

public class EchoArchitectGameState : MonoBehaviour
{
    enum GamePhase
    {
        MainMenu,
        Playing,
        GameOver
    }

    const string BestTimeKey = "EchoArchitect.BestSurvivalTime";
    const string AutoStartNextRunKey = "EchoArchitect.AutoStartNextRun";
    const float IntroMessageDuration = 9f;
    const string MainScenePath = "Assets/Main.unity";
    const string GeniesCreatorSceneName = "CreatingCustomEditor";

    static EchoArchitectGameState instance;
    static bool autoStartNextRun;
    static bool sceneLoadHookRegistered;

    PlayerNoiseEmitter noiseEmitter;
    MonsterAI monster;
    GamePhase phase = GamePhase.MainMenu;
    string statusMessage = "Stay quiet, watch your noise, and find the gate before the monster reaches you.";
    float introMessageTimer;
    float gameStartedBannerTimer;
    float survivalTimer;
    float runResultTime;
    float bestSurvivalTime;

    public static bool IsGameOver { get; private set; }
    public static bool IsGameplayActive => instance != null && instance.phase == GamePhase.Playing && !IsGameOver;

    public static void RequestAutoStartNextRun()
    {
        autoStartNextRun = true;
        PlayerPrefs.SetInt(AutoStartNextRunKey, 1);
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterSceneLoadHook()
    {
        if (sceneLoadHookRegistered)
            return;

        SceneManager.sceneLoaded += OnSceneLoaded;
        sceneLoadHookRegistered = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInstanceAfterStartup()
    {
        EnsureInstanceForActiveScene();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstanceForActiveScene();
    }

    static void EnsureInstanceForActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != MainScenePath && scene.name != "Main")
            return;

        if (FindObjectOfType<EchoArchitectGameState>() != null)
            return;

        GameObject bootstrap = new GameObject("EchoArchitectGameState");
        bootstrap.AddComponent<EchoArchitectGameState>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        IsGameOver = false;
        bestSurvivalTime = PlayerPrefs.GetFloat(BestTimeKey, 0f);
    }

    void Start()
    {
        noiseEmitter = FindObjectOfType<PlayerNoiseEmitter>();
        monster = FindObjectOfType<MonsterAI>();
        OpenMainMenu();

        if (!autoStartNextRun && PlayerPrefs.GetInt(AutoStartNextRunKey, 0) == 1)
        {
            autoStartNextRun = true;
            PlayerPrefs.DeleteKey(AutoStartNextRunKey);
            PlayerPrefs.Save();
        }

        if (autoStartNextRun)
        {
            autoStartNextRun = false;
            PlayerPrefs.DeleteKey(AutoStartNextRunKey);
            PlayerPrefs.Save();
            BeginGame();
        }
    }

    void Update()
    {
        if (phase == GamePhase.Playing && !IsGameOver)
        {
            survivalTimer += Time.deltaTime;
            introMessageTimer = Mathf.Max(0f, introMessageTimer - Time.deltaTime);
            gameStartedBannerTimer = Mathf.Max(0f, gameStartedBannerTimer - Time.deltaTime);

            if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
                LockCursor();
        }

        if (phase == GamePhase.GameOver && Input.GetKeyDown(KeyCode.R))
            ReloadScene();
    }

    public static void SetCaught()
    {
        if (instance == null || IsGameOver)
            return;

        instance.EndRun("The monster caught you.");
    }

    public static void SetEscaped()
    {
        if (instance == null || IsGameOver)
            return;

        instance.EndRun("You escaped the town.");
    }

    void EndRun(string resultPrefix)
    {
        IsGameOver = true;
        phase = GamePhase.GameOver;
        runResultTime = survivalTimer;
        bestSurvivalTime = Mathf.Max(bestSurvivalTime, runResultTime);
        PlayerPrefs.SetFloat(BestTimeKey, bestSurvivalTime);
        PlayerPrefs.Save();
        statusMessage = resultPrefix + " Press R to return to the main menu.";
        UnlockCursor();
    }

    void OpenMainMenu()
    {
        phase = GamePhase.MainMenu;
        IsGameOver = false;
        survivalTimer = 0f;
        runResultTime = 0f;
        introMessageTimer = 0f;
        gameStartedBannerTimer = 0f;
        statusMessage = "Choose Play to enter the town, or open the Genies creator first.";
        UnlockCursor();
    }

    void BeginGame()
    {
        phase = GamePhase.Playing;
        IsGameOver = false;
        survivalTimer = 0f;
        runResultTime = 0f;
        introMessageTimer = IntroMessageDuration;
        gameStartedBannerTimer = 4f;
        statusMessage = "Game started. Click the Game view if mouse look is not active.";
        LockCursor();
    }

    void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ReplayRun()
    {
        autoStartNextRun = true;
        ReloadScene();
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnGUI()
    {
        GUI.color = Color.white;

        float noiseLevel = noiseEmitter != null ? noiseEmitter.CurrentNoise : 0f;
        float monsterDistance = monster != null && monster.player != null
            ? Vector3.Distance(monster.transform.position, monster.player.position)
            : 0f;

        if (phase == GamePhase.MainMenu)
        {
            DrawMainMenu();
            return;
        }

        DrawHud(noiseLevel, monsterDistance);

        if (phase == GamePhase.GameOver)
            DrawGameOver();
    }

    void DrawMainMenu()
    {
        Rect rect = new Rect((Screen.width * 0.5f) - 230f, (Screen.height * 0.5f) - 150f, 460f, 300f);
        DrawPanel(rect, string.Empty);

        GUI.Label(new Rect(rect.x + 22f, rect.y + 18f, rect.width - 44f, 28f), "Echo Architect");
        GUI.Label(new Rect(rect.x + 22f, rect.y + 50f, rect.width - 44f, 56f), "A sound-driven horror chase through a dead town.");
        GUI.Label(new Rect(rect.x + 22f, rect.y + 92f, rect.width - 44f, 24f), "Best Record: " + FormatTime(bestSurvivalTime));

        if (GUI.Button(new Rect(rect.x + 22f, rect.y + 132f, rect.width - 44f, 42f), "Play"))
            BeginGame();

        if (GUI.Button(new Rect(rect.x + 22f, rect.y + 184f, rect.width - 44f, 42f), "Open Genies Creator"))
            SceneManager.LoadScene(GeniesCreatorSceneName);

        GUI.Label(new Rect(rect.x + 22f, rect.y + 238f, rect.width - 44f, 44f), statusMessage);
    }

    void DrawHud(float noiseLevel, float monsterDistance)
    {
        DrawPanel(new Rect(18f, 18f, 470f, 138f), "Echo Architect");
        GUI.Label(new Rect(32f, 52f, 420f, 22f), "Noise: " + Mathf.RoundToInt(noiseLevel * 100f) + "%");
        GUI.Label(new Rect(32f, 74f, 420f, 22f), "Monster Distance: " + Mathf.RoundToInt(monsterDistance) + "m");
        GUI.Label(new Rect(32f, 96f, 420f, 22f), "Time Alive: " + FormatTime(survivalTimer));
        GUI.Label(new Rect(32f, 118f, 420f, 22f), "Best Record: " + FormatTime(bestSurvivalTime));

        if (introMessageTimer > 0f)
        {
            DrawPanel(
                new Rect((Screen.width * 0.5f) - 250f, 24f, 500f, 88f),
                "WASD move, Shift sprint, Ctrl crouch, Space jump. Speak into the mic or make footsteps and the monster will follow the sound.");
        }

        if (gameStartedBannerTimer > 0f)
        {
            DrawPanel(
                new Rect((Screen.width * 0.5f) - 220f, 126f, 440f, 58f),
                "Game Started. Click here once if you need to recapture the mouse.");
        }

        DrawPanel(new Rect(18f, Screen.height - 92f, 640f, 58f), statusMessage);
    }

    void DrawGameOver()
    {
        Rect rect = new Rect((Screen.width * 0.5f) - 230f, (Screen.height * 0.5f) - 120f, 460f, 240f);
        DrawPanel(rect, string.Empty);

        GUI.Label(new Rect(rect.x + 20f, rect.y + 18f, rect.width - 40f, 26f), "Run Over");
        GUI.Label(new Rect(rect.x + 20f, rect.y + 54f, rect.width - 40f, 24f), "Survived: " + FormatTime(runResultTime));
        GUI.Label(new Rect(rect.x + 20f, rect.y + 80f, rect.width - 40f, 24f), "Best Record: " + FormatTime(bestSurvivalTime));
        GUI.Label(new Rect(rect.x + 20f, rect.y + 112f, rect.width - 40f, 44f), statusMessage);

        if (GUI.Button(new Rect(rect.x + 20f, rect.y + 146f, rect.width - 40f, 30f), "Play Again"))
            ReplayRun();

        if (GUI.Button(new Rect(rect.x + 20f, rect.y + 182f, rect.width - 40f, 30f), "Return To Main Menu"))
            ReloadScene();
    }

    void DrawPanel(Rect rect, string text)
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.Box(rect, GUIContent.none);
        GUI.color = oldColor;

        if (!string.IsNullOrEmpty(text))
            GUI.Label(new Rect(rect.x + 14f, rect.y + 14f, rect.width - 20f, rect.height - 20f), text);
    }

    string FormatTime(float seconds)
    {
        if (seconds <= 0f)
            return "0.0s";

        return seconds.ToString("0.0") + "s";
    }
}
