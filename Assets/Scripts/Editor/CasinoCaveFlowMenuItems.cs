#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-shot scene wiring for the meta loop (menus, save bootstrap, beacon prefab, build settings).
/// </summary>
public static class CasinoCaveFlowMenuItems
{
    const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    const string DungeonPath = "Assets/Scenes/DungeonGameplay.unity";
    const string BeaconPrefabPath = "Assets/Prefabs/CaveExitBeacon.prefab";

    [MenuItem("Casino Cave/Flow/1) Create Main Menu Scene + Build Settings")]
    public static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        CreateFullStretchImage(canvasGo.transform, "MenuBackground", new Color(0.08f, 0.06f, 0.12f, 1f));

        var menuRoot = new GameObject("MainMenuRoot");
        menuRoot.transform.SetParent(canvasGo.transform, false);
        var rt = menuRoot.AddComponent<RectTransform>();
        StretchFull(rt);

        var mm = menuRoot.AddComponent<MainMenuController>();

        var continueBtn = CreateMenuButton(menuRoot.transform, "ContinueButton", new Vector2(0, 80), out _);
        var newBtn = CreateMenuButton(menuRoot.transform, "NewGameButton", new Vector2(0, 0), out _);
        var exitBtn = CreateMenuButton(menuRoot.transform, "ExitButton", new Vector2(0, -80), out _);

        SetButtonLabel(continueBtn.transform, "Continue");
        SetButtonLabel(newBtn.transform, "New Game");
        SetButtonLabel(exitBtn.transform, "Exit Game");

        SerializedObject soMm = new SerializedObject(mm);
        soMm.FindProperty("continueButton").objectReferenceValue = continueBtn;
        soMm.FindProperty("newGameButton").objectReferenceValue = newBtn;
        soMm.FindProperty("exitButton").objectReferenceValue = exitBtn;
        soMm.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, MainMenuPath);
        SetBuildSettings();
        Debug.Log($"Casino Cave: saved {MainMenuPath} and updated Build Settings (Main Menu first).");
    }

    [MenuItem("Casino Cave/Flow/2) Add Gameplay Menus Under Canvas (DungeonGameplay)")]
    public static void AddGameplayMenus()
    {
        var scene = EditorSceneManager.OpenScene(DungeonPath);
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas in DungeonGameplay.");
            return;
        }

        Transform parent = canvas.transform;
        if (parent.Find("GameplayFlowMenus") != null)
        {
            Debug.LogWarning("GameplayFlowMenus already exists — delete it first to regenerate.");
            return;
        }

        var root = new GameObject("GameplayFlowMenus");
        root.transform.SetParent(parent, false);
        var rootRt = root.AddComponent<RectTransform>();
        StretchFull(rootRt);

        var flow = root.AddComponent<GameplayMenusController>();

        var intro = CreatePanelWithBackground(root.transform, "IntroCutscene", new Color(0, 0, 0, 0.92f), out var introBg, out var introBodyRect);
        var introTmp = CreateTmp(introBodyRect, 32, TextAlignmentOptions.Center);
        intro.SetActive(false);

        var pause = CreatePanelWithBackground(root.transform, "PauseMenu", new Color(0, 0, 0, 0.75f), out var pauseBg, out var pauseBtnParent);
        CreatePauseButtons(pauseBtnParent, out var pauseRestart, out var pauseQuit);
        pause.SetActive(false);

        var death = CreatePanelWithBackground(root.transform, "DeathMenu", new Color(0.12f, 0, 0, 0.88f), out var deathBg, out var deathBtnParent);
        CreateTmp(deathBtnParent, 48, TextAlignmentOptions.Center).text = "You died";
        var deathBtnRow = new GameObject("Buttons");
        deathBtnRow.transform.SetParent(deathBtnParent, false);
        var drr = deathBtnRow.AddComponent<RectTransform>();
        drr.sizeDelta = new Vector2(600, 120);
        var deathRetry = CreateMenuButton(deathBtnRow.transform, "Retry", new Vector2(-140, -80), out _);
        var deathQuit = CreateMenuButton(deathBtnRow.transform, "QuitTitle", new Vector2(140, -80), out _);
        SetButtonLabel(deathRetry.transform, "Retry");
        SetButtonLabel(deathQuit.transform, "Quit to title");
        death.SetActive(false);

        var victory = CreatePanelWithBackground(root.transform, "VictoryMenu", new Color(0.05f, 0.12f, 0.05f, 0.92f), out var vicBg, out var vicBody);
        CreateTmp(vicBody, 40, TextAlignmentOptions.Center).text = "Casino cave cleared!";
        var score = CreateTmp(vicBody, 28, TextAlignmentOptions.Center);
        score.rectTransform.anchoredPosition = new Vector2(0, -40);
        var high = CreateTmp(vicBody, 28, TextAlignmentOptions.Center);
        high.rectTransform.anchoredPosition = new Vector2(0, -80);
        var vicBtnRow = new GameObject("Buttons");
        vicBtnRow.transform.SetParent(vicBody, false);
        vicBtnRow.AddComponent<RectTransform>().sizeDelta = new Vector2(600, 120);
        var vicReplay = CreateMenuButton(vicBtnRow.transform, "Replay", new Vector2(-140, -140), out _);
        var vicQuit = CreateMenuButton(vicBtnRow.transform, "VicQuit", new Vector2(140, -140), out _);
        SetButtonLabel(vicReplay.transform, "Replay");
        SetButtonLabel(vicQuit.transform, "Quit to title");
        victory.SetActive(false);

        var leave = CreatePanelWithBackground(root.transform, "LeaveCavePrompt", new Color(0, 0, 0, 0.7f), out var leaveBg, out var leaveBody);
        var leaveTmp = CreateTmp(leaveBody, 30, TextAlignmentOptions.Center);
        leave.SetActive(false);

        SerializedObject so = new SerializedObject(flow);
        so.FindProperty("introRoot").objectReferenceValue = intro;
        so.FindProperty("introBackgroundImage").objectReferenceValue = introBg;
        so.FindProperty("introBody").objectReferenceValue = introTmp;
        so.FindProperty("pauseRoot").objectReferenceValue = pause;
        so.FindProperty("pauseBackgroundImage").objectReferenceValue = pauseBg;
        so.FindProperty("pauseRestartButton").objectReferenceValue = pauseRestart;
        so.FindProperty("pauseQuitButton").objectReferenceValue = pauseQuit;
        so.FindProperty("deathRoot").objectReferenceValue = death;
        so.FindProperty("deathBackgroundImage").objectReferenceValue = deathBg;
        so.FindProperty("deathRetryButton").objectReferenceValue = deathRetry;
        so.FindProperty("deathQuitButton").objectReferenceValue = deathQuit;
        so.FindProperty("victoryRoot").objectReferenceValue = victory;
        so.FindProperty("victoryBackgroundImage").objectReferenceValue = vicBg;
        so.FindProperty("victoryScoreText").objectReferenceValue = score;
        so.FindProperty("victoryHighScoreText").objectReferenceValue = high;
        so.FindProperty("victoryReplayButton").objectReferenceValue = vicReplay;
        so.FindProperty("victoryQuitButton").objectReferenceValue = vicQuit;
        so.FindProperty("leaveCaveRoot").objectReferenceValue = leave;
        so.FindProperty("leaveCaveBackgroundImage").objectReferenceValue = leaveBg;
        so.FindProperty("leaveCaveBody").objectReferenceValue = leaveTmp;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Casino Cave: GameplayFlowMenus added under Canvas. Assign optional art on each panel Image.");
    }

    [MenuItem("Casino Cave/Flow/3) Create Cave Exit Beacon Prefab")]
    public static void CreateBeaconPrefab()
    {
        var go = new GameObject("CaveExitBeacon");
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.2f;
        go.AddComponent<CaveExitBeacon>();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.4f, 0.9f, 1f, 0.85f);

        EnsurePrefabFolder();
        PrefabUtility.SaveAsPrefabAsset(go, BeaconPrefabPath);
        Object.DestroyImmediate(go);
        Debug.Log($"Casino Cave: saved {BeaconPrefabPath}. Assign a sprite on the SpriteRenderer, then assign prefab on DungeonRoomSpawner.caveExitBeaconPrefab.");
    }

    [MenuItem("Casino Cave/Flow/4) Link DungeonBootstrap → GameplayMenusController")]
    public static void LinkBootstrapToMenus()
    {
        var scene = EditorSceneManager.OpenScene(DungeonPath);
        var boot = Object.FindFirstObjectByType<DungeonBootstrap>();
        var menus = Object.FindFirstObjectByType<GameplayMenusController>();
        if (boot == null || menus == null)
        {
            Debug.LogError("Casino Cave: need DungeonBootstrap and GameplayMenusController in the scene (run step 3 first).");
            return;
        }

        SerializedObject so = new SerializedObject(boot);
        so.FindProperty("gameplayMenus").objectReferenceValue = menus;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Casino Cave: DungeonBootstrap.gameplayMenus linked.");
    }

    [MenuItem("Casino Cave/Flow/5) Assign Beacon Prefab To Dungeon Spawner")]
    public static void AssignBeaconToSpawner()
    {
        var beacon = AssetDatabase.LoadAssetAtPath<GameObject>(BeaconPrefabPath);
        if (beacon == null)
        {
            Debug.LogError("Create beacon prefab first (menu item 4).");
            return;
        }

        var scene = EditorSceneManager.OpenScene(DungeonPath);
        var spawner = Object.FindFirstObjectByType<DungeonRoomSpawner>();
        if (spawner == null)
        {
            Debug.LogError("No DungeonRoomSpawner.");
            return;
        }

        SerializedObject so = new SerializedObject(spawner);
        so.FindProperty("caveExitBeaconPrefab").objectReferenceValue = beacon;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Casino Cave: caveExitBeaconPrefab assigned on DungeonRoomSpawner.");
    }

    static void SetBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuPath, true),
            new EditorBuildSettingsScene(DungeonPath, true),
        };
    }

    static void EnsurePrefabFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Image CreateFullStretchImage(Transform parent, string name, Color c)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        StretchFull(rt);
        var img = go.AddComponent<Image>();
        img.color = c;
        return img;
    }

    static GameObject CreatePanelWithBackground(Transform parent, string name, Color bg, out Image bgImage, out RectTransform contentParent)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        var prt = panel.AddComponent<RectTransform>();
        StretchFull(prt);

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(panel.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        StretchFull(bgRt);
        bgImage = bgGo.AddComponent<Image>();
        bgImage.color = bg;

        var content = new GameObject("Content");
        content.transform.SetParent(panel.transform, false);
        contentParent = content.AddComponent<RectTransform>();
        StretchFull(contentParent);

        return panel;
    }

    static TextMeshProUGUI CreateTmp(RectTransform parent, float size, TextAlignmentOptions align)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900, 200);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        return tmp;
    }

    static void CreatePauseButtons(RectTransform parent, out Button restart, out Button quit)
    {
        var title = CreateTmp(parent, 36, TextAlignmentOptions.Center);
        title.text = "Paused";
        title.rectTransform.anchoredPosition = new Vector2(0, 60);

        restart = CreateMenuButton(parent, "PauseRestart", new Vector2(0, -20), out _);
        quit = CreateMenuButton(parent, "PauseQuit", new Vector2(0, -90), out _);
        SetButtonLabel(restart.transform, "Restart");
        SetButtonLabel(quit.transform, "Quit to title");
    }

    static Button CreateMenuButton(Transform parent, string name, Vector2 anchoredPos, out RectTransform rt)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(280, 48);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        return btn;
    }

    static void SetButtonLabel(Transform button, string label)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(button, false);
        var rt = go.AddComponent<RectTransform>();
        StretchFull(rt);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }
}
#endif
