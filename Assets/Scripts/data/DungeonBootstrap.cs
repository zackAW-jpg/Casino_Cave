using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public class DungeonBootstrap : MonoBehaviour
{
    public DungeonStateSO dungeonState;

    public int totalRooms = 25;
    public int bossMinDistance = 6;
    public int bossMaxDistance = 10;
    public int merchantCount = 1;
    public int treasureCount = 2;

    [Header("Optional wiring (auto-resolve when null)")]
    public DungeonRoomSpawner dungeonRoomSpawner;
    public Transform playerTransform;
    public PlayerHealth playerHealth;
    public GameplayMenusController gameplayMenus;

    [Header("Dungeon music (optional)")]
    public AudioClip dungeonMusicLoop;
    public AudioClip dungeonAmbientBedLoop;
    [Range(0f, 1f)] public float dungeonMusicVolume = 0.5f;
    [Range(0f, 1f)] public float dungeonAmbientVolume = 0.35f;

    DungeonRoomSpawner _spawner;
    Transform _playerTransform;
    PlayerHealth _playerHealth;
    PlayerStateSO _playerState;
    AudioSource _dungeonMusicSource;
    AudioSource _dungeonAmbientSource;
    bool _dungeonMusicStarted;

    void Awake()
    {
        ResolveRefs();

        if (GameSession.NextBoot == GameSession.BootMode.Continue && PlayerSaveStore.CanContinue())
        {
            if (dungeonState == null || _playerState == null)
            {
                Debug.LogError(
                    "DungeonBootstrap: Continue load needs dungeonState and a player with PlayerStateSO assigned.",
                    this);
            }
            else
            {
                PlayerSaveStore.ApplyContinueToScriptables(dungeonState, _playerState, out _);
                ContinueLoadGuard.MarkPendingSkipPlayerHealthInit();
            }
        }
    }

    IEnumerator Start()
    {
        if (dungeonState == null)
        {
            Debug.LogError("DungeonBootstrap: dungeonState is not assigned.", this);
            yield break;
        }

        if (_spawner == null || _playerTransform == null || _playerHealth == null || _playerState == null)
        {
            Debug.LogError("DungeonBootstrap: missing spawner or player references.", this);
            yield break;
        }

        GameplayInputGate.SetDungeonBootComplete(false);

        if (GameSession.NextBoot == GameSession.BootMode.Continue && !PlayerSaveStore.CanContinue())
        {
            Debug.LogWarning("DungeonBootstrap: Continue invalid — starting a new dungeon.");
            GameSession.NextBoot = GameSession.BootMode.NewGame;
        }

        GameplaySaveContext.Bind(dungeonState, _playerState, _playerTransform);

        var menus = gameplayMenus != null ? gameplayMenus : FindFirstObjectByType<GameplayMenusController>();

        if (GameSession.NextBoot == GameSession.BootMode.Continue)
        {
            Vector3 pos = PlayerSaveStore.LastLoadedPlayerPosition;
            _spawner.DestroySpawnedRooms();
            _spawner.BuildDungeonAndPlacePlayer(true, pos);
            GameSession.NextBoot = GameSession.BootMode.NewGame;
            PlayerSaveStore.PersistRunInProgress(dungeonState, _playerState, _playerTransform);
            menus?.NotifyGameplayStarted();
            StartDungeonMusicIfConfigured();
            yield break;
        }

        GamblingArmRuntimeState.ResetForNewRun();

        _playerState.gold = 0;
        _playerHealth.PrepareForFreshRunAfterDungeonReset();
        _playerState.healthPotionCount = _playerHealth.startingHealthPotions;

        if (menus != null)
            yield return menus.PlayIntroCutsceneIfConfigured();

        dungeonState.GenerateNewDungeon(totalRooms, bossMinDistance, bossMaxDistance, merchantCount, treasureCount);
        _spawner.DestroySpawnedRooms();
        _spawner.BuildDungeonAndPlacePlayer(false, default);
        PlayerSaveStore.PersistRunInProgress(dungeonState, _playerState, _playerTransform);
        menus?.NotifyGameplayStarted();
        StartDungeonMusicIfConfigured();
    }

    void StartDungeonMusicIfConfigured()
    {
        if (_dungeonMusicStarted)
            return;

        if (dungeonMusicLoop != null && _dungeonMusicSource == null)
        {
            _dungeonMusicSource = gameObject.AddComponent<AudioSource>();
            _dungeonMusicSource.loop = true;
            _dungeonMusicSource.playOnAwake = false;
            _dungeonMusicSource.clip = dungeonMusicLoop;
            _dungeonMusicSource.volume = dungeonMusicVolume;
            _dungeonMusicSource.Play();
        }

        if (dungeonAmbientBedLoop != null && _dungeonAmbientSource == null)
        {
            _dungeonAmbientSource = gameObject.AddComponent<AudioSource>();
            _dungeonAmbientSource.loop = true;
            _dungeonAmbientSource.playOnAwake = false;
            _dungeonAmbientSource.clip = dungeonAmbientBedLoop;
            _dungeonAmbientSource.volume = dungeonAmbientVolume;
            _dungeonAmbientSource.Play();
        }

        if (dungeonMusicLoop != null || dungeonAmbientBedLoop != null)
            _dungeonMusicStarted = true;
    }

    void ResolveRefs()
    {
        if (dungeonRoomSpawner != null)
            _spawner = dungeonRoomSpawner;
        else
            _spawner = FindFirstObjectByType<DungeonRoomSpawner>();

        if (playerTransform != null)
            _playerTransform = playerTransform;
        else
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                _playerTransform = p.transform;
        }

        if (playerHealth != null)
            _playerHealth = playerHealth;
        else if (_playerTransform != null)
            _playerHealth = _playerTransform.GetComponent<PlayerHealth>();

        _playerState = _playerHealth != null ? _playerHealth.state : null;
    }

    
    
    
    public void RegenerateFreshDungeonRun()
    {
        StartCoroutine(CoRegenerateFreshDungeonRun());
    }

    IEnumerator CoRegenerateFreshDungeonRun()
    {
        ResolveRefs();
        if (_spawner == null || dungeonState == null || _playerHealth == null || _playerState == null ||
            _playerTransform == null)
        {
            Debug.LogError("DungeonBootstrap.RegenerateFreshDungeonRun: missing references.", this);
            yield break;
        }

        Time.timeScale = 1f;
        GameplayInputGate.SetPauseMenuOpen(false);
        GameplayInputGate.SetDungeonBootComplete(false);

        GamblingArmRuntimeState.ResetForNewRun();
        _playerHealth.PrepareForFreshRunAfterDungeonReset();
        _playerState.gold = 0;
        _playerState.healthPotionCount = _playerHealth.startingHealthPotions;

        GameplaySaveContext.Bind(dungeonState, _playerState, _playerTransform);

        dungeonState.GenerateNewDungeon(totalRooms, bossMinDistance, bossMaxDistance, merchantCount, treasureCount);
        _spawner.DestroySpawnedRooms();
        yield return null;
        _spawner.BuildDungeonAndPlacePlayer(false, default);
        PlayerSaveStore.PersistRunInProgress(dungeonState, _playerState, _playerTransform);
        GameplayInputGate.SetDungeonBootComplete(true);
        StartDungeonMusicIfConfigured();
    }
}
