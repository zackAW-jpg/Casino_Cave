using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// In-game UI: intro, pause (Esc), death, victory, and cave exit prompt. Assign panel roots and optional
/// full-screen <see cref="Image"/> backgrounds per screen in the Inspector.
/// </summary>
[DisallowMultipleComponent]
public class GameplayMenusController : MonoBehaviour
{
    public static GameplayMenusController Instance { get; private set; }

    [Header("Scene refs (optional Find if unset)")]
    public DungeonBootstrap dungeonBootstrap;
    public PlayerHealth playerHealth;
    public PlayerStateSO playerState;
    public Transform playerTransform;

    [Header("Intro cutscene")]
    public GameObject introRoot;
    public Image introBackgroundImage;
    public TextMeshProUGUI introBody;
    [TextArea(2, 6)]
    public string[] introLines =
    {
        "My casino's out of poker chips...",
        "Guess it's time to go to The Cave..."
    };

    [Header("Pause menu")]
    public GameObject pauseRoot;
    public Image pauseBackgroundImage;
    public Button pauseRestartButton;
    public Button pauseQuitButton;

    [Header("Death menu")]
    public GameObject deathRoot;
    public Image deathBackgroundImage;
    public Button deathRetryButton;
    public Button deathQuitButton;

    [Header("Victory menu")]
    public GameObject victoryRoot;
    public Image victoryBackgroundImage;
    public TextMeshProUGUI victoryScoreText;
    public TextMeshProUGUI victoryHighScoreText;
    public Button victoryReplayButton;
    public Button victoryQuitButton;

    [Header("Leave cave prompt")]
    public GameObject leaveCaveRoot;
    public Image leaveCaveBackgroundImage;
    public TextMeshProUGUI leaveCaveBody;

    [Header("Audio (optional)")]
    [Tooltip("Short sting when the dungeon is cleared (plays when the player confirms leaving the cave).")]
    public AudioClip dungeonCompleteSound;
    public AudioSource uiAudioSource;

    bool _leavePromptVisible;

    void Awake()
    {
        Instance = this;
        ResolveSceneRefs();

        if (pauseRestartButton != null)
            pauseRestartButton.onClick.AddListener(OnPauseRestartClicked);
        if (pauseQuitButton != null)
            pauseQuitButton.onClick.AddListener(OnPauseQuitClicked);
        if (deathRetryButton != null)
            deathRetryButton.onClick.AddListener(OnDeathRetryClicked);
        if (deathQuitButton != null)
            deathQuitButton.onClick.AddListener(OnDeathQuitClicked);
        if (victoryReplayButton != null)
            victoryReplayButton.onClick.AddListener(OnVictoryReplayClicked);
        if (victoryQuitButton != null)
            victoryQuitButton.onClick.AddListener(OnVictoryQuitClicked);

        if (uiAudioSource == null)
            uiAudioSource = GetComponent<AudioSource>();

        SetAllPanelsInactive();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        if (_leavePromptVisible)
        {
            if (Keyboard.current.yKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
                ConfirmLeaveCaveYes();
            else if (Keyboard.current.nKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
                ConfirmLeaveCaveNo();
            return;
        }

        if (GameplayInputGate.CanTogglePause && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePauseFromEscape();
    }

    void ResolveSceneRefs()
    {
        if (dungeonBootstrap == null)
            dungeonBootstrap = FindFirstObjectByType<DungeonBootstrap>();
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                playerTransform = p.transform;
        }
        if (playerHealth == null && playerTransform != null)
            playerHealth = playerTransform.GetComponent<PlayerHealth>();
        if (playerState == null && playerHealth != null)
            playerState = playerHealth.state;
    }

    void SetAllPanelsInactive()
    {
        if (introRoot != null) introRoot.SetActive(false);
        if (pauseRoot != null) pauseRoot.SetActive(false);
        if (deathRoot != null) deathRoot.SetActive(false);
        if (victoryRoot != null) victoryRoot.SetActive(false);
        if (leaveCaveRoot != null) leaveCaveRoot.SetActive(false);
    }

    public IEnumerator PlayIntroCutsceneIfConfigured()
    {
        if (introRoot == null || introLines == null || introLines.Length == 0)
            yield break;

        GameplayInputGate.PushMenuModal();
        introRoot.SetActive(true);

        for (int i = 0; i < introLines.Length; i++)
        {
            if (introBody != null)
                introBody.text = introLines[i];
            yield return WaitForIntroAdvance();
            // Same physical press can still be "wasPressedThisFrame" when the next wait starts; require release first.
            if (i < introLines.Length - 1)
                yield return WaitUntilIntroAdvanceReleased();
        }

        introRoot.SetActive(false);
        GameplayInputGate.PopMenuModal();
    }

    static IEnumerator WaitForIntroAdvance()
    {
        while (true)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                yield break;
            if (Keyboard.current != null &&
                (Keyboard.current.spaceKey.wasPressedThisFrame ||
                 Keyboard.current.enterKey.wasPressedThisFrame))
                yield break;
            yield return null;
        }
    }

    static IEnumerator WaitUntilIntroAdvanceReleased()
    {
        while (AnyIntroAdvanceHeld())
            yield return null;
    }

    static bool AnyIntroAdvanceHeld()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            return true;
        if (Keyboard.current == null)
            return false;
        return Keyboard.current.spaceKey.isPressed || Keyboard.current.enterKey.isPressed;
    }

    public void NotifyGameplayStarted()
    {
        GameplayInputGate.SetDungeonBootComplete(true);
    }

    public void BeginDeathSequence(PlayerHealth health)
    {
        StartCoroutine(CoDeathSequence(health));
    }

    IEnumerator CoDeathSequence(PlayerHealth health)
    {
        GameplayInputGate.PushMenuModal();

        if (health != null)
            yield return health.PlayDeathPresentationIfAny();

        if (deathRoot != null)
            deathRoot.SetActive(true);

        Time.timeScale = 0f;

        if (GameplaySaveContext.Dungeon != null && GameplaySaveContext.Player != null)
            PlayerSaveStore.MarkRunAbandoned(
                GameplaySaveContext.Dungeon,
                GameplaySaveContext.Player,
                GameplaySaveContext.PlayerTransform);
    }

    void TogglePauseFromEscape()
    {
        if (pauseRoot == null)
            return;

        if (pauseRoot.activeSelf)
            ClosePauseIfOpen();
        else
        {
            pauseRoot.SetActive(true);
            GameplayInputGate.SetPauseMenuOpen(true);
        }
    }

    void ClosePauseIfOpen()
    {
        if (pauseRoot != null && pauseRoot.activeSelf)
        {
            pauseRoot.SetActive(false);
            GameplayInputGate.SetPauseMenuOpen(false);
        }
    }

    void OnPauseRestartClicked()
    {
        ClosePauseIfOpen();
        if (dungeonBootstrap != null)
            dungeonBootstrap.RegenerateFreshDungeonRun();
    }

    void OnPauseQuitClicked()
    {
        ClosePauseIfOpen();
        GoToMainMenu();
    }

    void OnDeathRetryClicked()
    {
        Time.timeScale = 1f;
        if (deathRoot != null)
            deathRoot.SetActive(false);
        GameplayInputGate.PopMenuModal();

        if (dungeonBootstrap != null)
            dungeonBootstrap.RegenerateFreshDungeonRun();
    }

    void OnDeathQuitClicked()
    {
        Time.timeScale = 1f;
        if (deathRoot != null)
            deathRoot.SetActive(false);
        GameplayInputGate.PopMenuModal();
        GoToMainMenu();
    }

    void OnVictoryReplayClicked()
    {
        Time.timeScale = 1f;
        if (victoryRoot != null)
            victoryRoot.SetActive(false);
        GameplayInputGate.PopMenuModal();

        if (dungeonBootstrap != null)
            dungeonBootstrap.RegenerateFreshDungeonRun();
    }

    void OnVictoryQuitClicked()
    {
        Time.timeScale = 1f;
        if (victoryRoot != null)
            victoryRoot.SetActive(false);
        GameplayInputGate.PopMenuModal();
        GoToMainMenu();
    }

    public void ShowLeaveCavePrompt()
    {
        if (leaveCaveRoot == null)
            return;

        if (!leaveCaveRoot.activeSelf)
            GameplayInputGate.PushMenuModal();

        leaveCaveRoot.SetActive(true);
        _leavePromptVisible = true;
        if (leaveCaveBody != null)
            leaveCaveBody.text = "Leave the cave?\n\nY — Yes    N — No";
    }

    public void HideLeaveCavePrompt()
    {
        if (leaveCaveRoot == null)
            return;

        if (leaveCaveRoot.activeSelf)
            GameplayInputGate.PopMenuModal();

        leaveCaveRoot.SetActive(false);
        _leavePromptVisible = false;
    }

    void ConfirmLeaveCaveYes()
    {
        HideLeaveCavePrompt();

        if (playerState == null)
            ResolveSceneRefs();

        SfxUtil.PlayOneShot(dungeonCompleteSound, uiAudioSource, transform.position);

        int chips = playerState != null ? playerState.gold : 0;
        PlayerSaveStore.RecordVictory(
            chips,
            GameplaySaveContext.Dungeon,
            GameplaySaveContext.Player,
            GameplaySaveContext.PlayerTransform);

        int high = PlayerSaveStore.ReadHighScoreOnly();

        if (victoryRoot != null)
        {
            if (victoryScoreText != null)
                victoryScoreText.text = $"Chips this run: {chips}";
            if (victoryHighScoreText != null)
                victoryHighScoreText.text = $"Best chips: {high}";
            victoryRoot.SetActive(true);
            GameplayInputGate.PushMenuModal();
            Time.timeScale = 0f;
        }
    }

    void ConfirmLeaveCaveNo()
    {
        HideLeaveCavePrompt();
    }

    static void GoToMainMenu()
    {
        GameplayInputGate.ResetForTitleOrFreshSession();
        SceneManager.LoadScene(GameSession.MainMenuSceneName);
    }
}
