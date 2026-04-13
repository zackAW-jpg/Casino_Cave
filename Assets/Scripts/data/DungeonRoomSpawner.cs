using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DungeonRoomSpawner : MonoBehaviour
{
    public DungeonStateSO dungeonState;
    public GameObject roomPrefab;

    [Header("Layout (center-to-center between rooms)")]
    [Tooltip("Vertical spacing between room centers.")]
    [FormerlySerializedAs("roomWorldSize")]
    public float roomSpacingY = 20f;

    [Tooltip("Horizontal spacing between room centers. Leave 0 to use Auto Balance, or same as Y if Auto Balance is off.")]
    public float roomSpacingX = 0f;

    [Tooltip("When roomSpacingX is 0, compute X so the horizontal gap between room edges matches the vertical gap (good when the room art is wider than tall).")]
    public bool autoBalanceHorizontalSpacing = true;

    public Transform playerTransform;

    [Header("Merchant NPCs")]
    public MerchantSpawnTableSO merchantSpawnTable;
    public float merchantSpawnRadius = 3f;

    public GameObject coinPrefab;
    public int coinsPerTreasureRoom = 10;
    public GameObject securityGuardPrefab;

    [Header("Boss")]
    [Tooltip("Spawned in the room marked RoomEventType.Boss.")]
    public GameObject bossPrefab;
    [Tooltip("Spawned in the boss room after the boss is defeated (or when loading a save with boss already defeated).")]
    public GameObject caveExitBeaconPrefab;
    public BossHUD bossHUD;
    [Tooltip("Coins dropped around the boss when it dies (uses the same prefab as treasure rooms if coinPrefab is set).")]
    public int bossCoinsOnDeath = 18;
    public float bossCoinSpawnRadius = 2.5f;

    [Header("Start room spawn")]
    [Tooltip("Which spawn point in the start room (0,0) to place the player at.")]
    public DoorDirection startRoomSpawnSide = DoorDirection.North;

    private Dictionary<Vector2Int, DungeonRoomController> _roomInstances = new Dictionary<Vector2Int, DungeonRoomController>();

  
    private void BuildDungeon()
    {
        _roomInstances.Clear();

        float sx = ResolveRoomSpacingX();
        float sy = roomSpacingY;

        foreach (RoomState roomState in dungeonState.rooms)
        {
            Vector3 worldPos = new Vector3(
                roomState.coord.x * sx,
                roomState.coord.y * sy,
                0f
            );

            GameObject instance = Instantiate(roomPrefab, worldPos, Quaternion.identity, transform);

            DungeonRoomController ctrl = instance.GetComponent<DungeonRoomController>();
            if (ctrl == null)
            {
                Debug.LogWarning($"Room prefab has no DungeonRoomController at coord {roomState.coord}. Skipping.", instance);
                continue;
            }

            ctrl.coord = roomState.coord;
            ctrl.spawner = this;
            ctrl.dungeonState = dungeonState;
            ctrl.playerTransform = playerTransform;
            ctrl.SetupFromRoomState(roomState);

            _roomInstances[roomState.coord] = ctrl;

            // Spawn one security guard in enemy rooms
            if (securityGuardPrefab != null && roomState.eventType == RoomEventType.NormalEnemies)
            {
                Vector3 guardPos = instance.transform.position; // center of the room
                GameObject guard = Instantiate(securityGuardPrefab, guardPos, Quaternion.identity, instance.transform);
                SecurityGuard guardAI = guard.GetComponent<SecurityGuard>();
                if (guardAI != null)
                {
                    if (playerTransform != null)
                        guardAI.target = playerTransform;
                    guardAI.roomCoord = roomState.coord;
                    guardAI.dungeonState = dungeonState;
                }
            }
            // Spawn coins in treasure rooms
            if (coinPrefab != null && roomState.eventType == RoomEventType.Treasure)
            {
                for (int i = 0; i < coinsPerTreasureRoom; i++)
                {
                    // Random position within a circle around the room center
                    Vector2 offset = Random.insideUnitCircle * (Mathf.Min(sx, sy) * 0.3f);
                    Vector3 coinPos = instance.transform.position + new Vector3(offset.x, offset.y, 0f);

                    Instantiate(coinPrefab, coinPos, Quaternion.identity, instance.transform);
                }
            }
            // Spawn merchant NPC in merchant rooms
            if (roomState.eventType == RoomEventType.Merchant && merchantSpawnTable != null)
            {
                GameObject merchantPrefab = merchantSpawnTable.PickPrefab();
                if (merchantPrefab != null)
                {
                    Vector2 offset = Random.insideUnitCircle * merchantSpawnRadius;
                    Vector3 pos = instance.transform.position + new Vector3(offset.x, offset.y, 0f);

                    Instantiate(merchantPrefab, pos, Quaternion.identity, instance.transform);
                }
            }
            if (roomState.eventType == RoomEventType.Boss)
            {
                if (roomState.bossDefeated)
                {
                    if (caveExitBeaconPrefab != null)
                    {
                        Vector3 p = instance.transform.position;
                        Instantiate(caveExitBeaconPrefab, p, Quaternion.identity, instance.transform);
                    }
                }
                else if (bossPrefab == null)
                {
                    Debug.LogWarning(
                        $"DungeonRoomSpawner: Boss room at {roomState.coord} but bossPrefab is not assigned.",
                        this);
                }
                else
                {
                    Vector3 bossPos = instance.transform.position;
                    GameObject boss = Instantiate(bossPrefab, bossPos, Quaternion.identity, instance.transform);
                    BossHealth bh = boss.GetComponentInChildren<BossHealth>(true);
                    BossController bc = boss.GetComponentInChildren<BossController>(true);
                    if (bh == null)
                        Debug.LogWarning("Boss prefab should include BossHealth (on root or child).", boss);
                    if (bc == null)
                        Debug.LogWarning("Boss prefab should include BossController.", boss);
                    if (bc != null)
                    {
                        bc.dungeonState = dungeonState;
                        bc.roomCoord = roomState.coord;
                        bc.bossHUD = bossHUD;
                    }
                    if (bh != null && coinPrefab != null)
                    {
                        bh.coinPrefab = coinPrefab;
                        bh.coinsOnDeath = bossCoinsOnDeath;
                        bh.coinSpawnRadius = bossCoinSpawnRadius;
                    }

                    if (bh != null && caveExitBeaconPrefab != null)
                    {
                        RoomState rs = roomState;
                        Transform roomRoot = instance.transform;
                        bh.OnDeath += () => HandleBossDefeated(rs, roomRoot);
                    }
                }
            }
        }

        Debug.Log($"DungeonRoomSpawner: Spawned {_roomInstances.Count} rooms (spacing X={sx}, Y={sy}).");
    }

    void HandleBossDefeated(RoomState bossRoom, Transform roomRoot)
    {
        bossRoom.bossDefeated = true;
        if (caveExitBeaconPrefab != null)
        {
            Vector3 p = roomRoot.position;
            Instantiate(caveExitBeaconPrefab, p, Quaternion.identity, roomRoot);
        }

        GameplaySaveContext.PersistRun();
    }

    /// <summary>Destroys instantiated room roots parented under this spawner.</summary>
    public void DestroySpawnedRooms()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        _roomInstances.Clear();
    }

    /// <summary>Effective horizontal center spacing (after auto-balance or manual roomSpacingX).</summary>
    public float GetEffectiveSpacingX() => ResolveRoomSpacingX();

    /// <summary>Vertical center spacing.</summary>
    public float GetEffectiveSpacingY() => roomSpacingY;

    private float ResolveRoomSpacingX()
    {
        if (roomSpacingX > 0f)
            return roomSpacingX;

        if (!autoBalanceHorizontalSpacing || roomPrefab == null)
            return roomSpacingY;

        if (!TryGetRoomFootprint(roomPrefab, out float width, out float height))
            return roomSpacingY;

        if (height < 0.001f || width < 0.001f)
            return roomSpacingY;

        float gapY = roomSpacingY - height;
        return width + Mathf.Max(0f, gapY);
    }

    /// <summary>
    /// Axis-aligned footprint of all SpriteRenderers under the prefab (world-oriented while evaluating the prefab).
    /// </summary>
    private static bool TryGetRoomFootprint(GameObject prefab, out float width, out float height)
    {
        width = height = 0f;
        SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0)
            return false;

        bool init = false;
        Bounds enc = default;
        foreach (SpriteRenderer sr in renderers)
        {
            if (!sr.enabled) continue;
            if (sr.GetComponentInParent<RoomFootprintIgnore>() != null)
                continue;
            Bounds b = sr.bounds;
            if (b.size.sqrMagnitude < 1e-8f) continue;
            if (!init)
            {
                enc = b;
                init = true;
            }
            else
                enc.Encapsulate(b);
        }

        if (!init)
            return false;

        width = enc.size.x;
        height = enc.size.y;
        return true;
    }

    private void PlacePlayerInStartRoom()
    {
        if (playerTransform == null) return;

        DungeonRoomController startRoom = GetRoomAt(Vector2Int.zero);
        if (startRoom == null) return;

        Transform spawn = startRoom.GetSpawnForSide(startRoomSpawnSide);
        if (spawn != null)
        {
            playerTransform.position = spawn.position;
        }
    }

    public DungeonRoomController GetRoomAt(Vector2Int coord)
    {
        return _roomInstances.TryGetValue(coord, out DungeonRoomController ctrl) ? ctrl : null;
    }

    public void BuildDungeonAndPlacePlayer()
    {
        BuildDungeonAndPlacePlayer(placeAtSavedWorldPosition: false, savedWorldPosition: default);
    }

    public void BuildDungeonAndPlacePlayer(bool placeAtSavedWorldPosition, Vector3 savedWorldPosition)
    {
        if (dungeonState == null)
        {
            Debug.LogError("DungeonRoomSpawner: dungeonState is not assigned.", this);
            return;
        }

        if (roomPrefab == null)
        {
            Debug.LogError("DungeonRoomSpawner: roomPrefab is not assigned.", this);
            return;
        }

        if (dungeonState.rooms == null || dungeonState.rooms.Count == 0)
        {
            Debug.LogError("DungeonRoomSpawner: Dungeon has no rooms. Call this after DungeonBootstrap has generated.", this);
            return;
        }

        BuildDungeon();

        if (placeAtSavedWorldPosition && playerTransform != null)
            playerTransform.position = savedWorldPosition;
        else
            PlacePlayerInStartRoom();
    }
}
