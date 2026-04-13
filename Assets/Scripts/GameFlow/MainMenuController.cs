using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Title screen: Continue (only when a run is in progress), New Game, Exit.
/// Assign optional <see cref="Image"/> backgrounds on each button group in the Inspector.
/// </summary>
[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    public Button continueButton;
    public Button newGameButton;
    public Button exitButton;

    [Header("Optional loading UI")]
    public GameObject loadingOverlayRoot;
    public Slider loadingProgressSlider;

    [Header("Main menu music (optional)")]
    [Tooltip("Looping music while this menu is open. Drag your soundtrack .ogg/.wav here.")]
    public AudioClip menuMusicLoop;
    [Range(0f, 1f)] public float menuMusicVolume = 0.55f;

    AudioSource _menuMusicSource;

    void Awake()
    {
        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);

        if (menuMusicLoop != null)
        {
            _menuMusicSource = gameObject.AddComponent<AudioSource>();
            _menuMusicSource.loop = true;
            _menuMusicSource.playOnAwake = false;
            _menuMusicSource.clip = menuMusicLoop;
            _menuMusicSource.volume = menuMusicVolume;
        }
    }

    void Start()
    {
        if (_menuMusicSource != null && menuMusicLoop != null)
            _menuMusicSource.Play();
    }

    void OnEnable()
    {
        GameplayInputGate.ResetForTitleOrFreshSession();
        RefreshContinueState();
    }

    public void RefreshContinueState()
    {
        if (continueButton == null)
            return;

        bool can = PlayerSaveStore.CanContinue();
        continueButton.interactable = can;

        var cg = continueButton.GetComponent<CanvasGroup>();
        if (cg != null)
            cg.alpha = can ? 1f : 0.45f;
    }

    void OnContinueClicked()
    {
        if (!PlayerSaveStore.CanContinue())
            return;

        GameSession.NextBoot = GameSession.BootMode.Continue;
        StartCoroutine(LoadGameplayRoutine());
    }

    void OnNewGameClicked()
    {
        GameSession.NextBoot = GameSession.BootMode.NewGame;
        StartCoroutine(LoadGameplayRoutine());
    }

    void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator LoadGameplayRoutine()
    {
        if (loadingOverlayRoot != null)
            loadingOverlayRoot.SetActive(true);

        AsyncOperation op = SceneManager.LoadSceneAsync(GameSession.GameplaySceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            if (loadingProgressSlider != null)
                loadingProgressSlider.value = op.progress;
            yield return null;
        }

        if (loadingProgressSlider != null)
            loadingProgressSlider.value = 1f;

        op.allowSceneActivation = true;
        yield return op;
    }
}
