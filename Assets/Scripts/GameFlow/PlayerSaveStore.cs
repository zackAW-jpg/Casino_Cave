using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class PlayerSaveDto
{
    public int version = 1;
    public bool runInProgress;
    public int highScoreChips;
    public bool slotsUnlocked;
    public int gold;
    public int maxHP;
    public int currentHP;
    public int healthPotionCount;
    public int healthPotionHealAmount;
    public int damage;
    public float playerX;
    public float playerY;
    public float playerZ;
    public int roomCoordX;
    public int roomCoordY;
    public RoomSaveDto[] rooms;
}

[Serializable]
public class RoomSaveDto
{
    public int cx;
    public int cy;
    public bool doorN;
    public bool doorE;
    public bool doorS;
    public bool doorW;
    public int eventType;
    public bool visited;
    public bool cleared;
    public bool bossDefeated;
    public int dist;
}

public static class PlayerSaveStore
{
    const string FileName = "casino_cave_save_slot.json";

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSaveFileOnDisk => File.Exists(FilePath);

    
    public static Vector3 LastLoadedPlayerPosition { get; private set; }

    public static bool TryLoad(out PlayerSaveDto dto)
    {
        dto = null;
        if (!HasSaveFileOnDisk)
            return false;

        try
        {
            string json = File.ReadAllText(FilePath);
            dto = JsonUtility.FromJson<PlayerSaveDto>(json);
            return dto != null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PlayerSaveStore: failed to read save — {e.Message}");
            return false;
        }
    }

    public static bool CanContinue()
    {
        if (!TryLoad(out PlayerSaveDto dto))
            return false;
        return dto.runInProgress;
    }

    public static int ReadHighScoreOnly()
    {
        if (!TryLoad(out PlayerSaveDto dto))
            return 0;
        return dto.highScoreChips;
    }

    public static void ApplyContinueToScriptables(DungeonStateSO dungeon, PlayerStateSO player, out Vector3 playerWorld)
    {
        if (!TryLoad(out PlayerSaveDto dto))
        {
            playerWorld = Vector3.zero;
            return;
        }

        ApplyDungeon(dungeon, dto);
        ApplyPlayerState(player, dto);
        playerWorld = new Vector3(dto.playerX, dto.playerY, dto.playerZ);
        LastLoadedPlayerPosition = playerWorld;
        GamblingArmRuntimeState.SetSlotsUnlocked(dto.slotsUnlocked);
    }

    public static void WriteSnapshot(
        DungeonStateSO dungeon,
        PlayerStateSO player,
        Transform playerTransform,
        bool runInProgress,
        int highScoreChips)
    {
        if (dungeon == null || player == null)
            return;

        var dto = new PlayerSaveDto
        {
            version = 1,
            runInProgress = runInProgress,
            highScoreChips = highScoreChips,
            slotsUnlocked = GamblingArmRuntimeState.SlotsUnlocked,
            gold = player.gold,
            maxHP = player.maxHP,
            currentHP = player.currentHP,
            healthPotionCount = player.healthPotionCount,
            healthPotionHealAmount = player.healthPotionHealAmount,
            damage = player.damage,
            playerX = playerTransform != null ? playerTransform.position.x : 0f,
            playerY = playerTransform != null ? playerTransform.position.y : 0f,
            playerZ = playerTransform != null ? playerTransform.position.z : 0f,
            roomCoordX = dungeon.currentRoomCoord.x,
            roomCoordY = dungeon.currentRoomCoord.y,
            rooms = ExportRooms(dungeon.rooms)
        };

        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(dto, prettyPrint: true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PlayerSaveStore: failed to write save — {e.Message}");
        }
    }

    public static void PersistRunInProgress(DungeonStateSO dungeon, PlayerStateSO player, Transform playerTransform)
    {
        int high = 0;
        if (TryLoad(out PlayerSaveDto dto) && dto != null)
            high = dto.highScoreChips;
        WriteSnapshot(dungeon, player, playerTransform, runInProgress: true, highScoreChips: high);
    }

    public static void MarkRunAbandoned(DungeonStateSO dungeon, PlayerStateSO player, Transform playerTransform)
    {
        int high = 0;
        if (TryLoad(out PlayerSaveDto dto) && dto != null)
            high = dto.highScoreChips;
        WriteSnapshot(dungeon, player, playerTransform, runInProgress: false, highScoreChips: high);
    }

    public static void RecordVictory(int chipsThisRun, DungeonStateSO dungeon, PlayerStateSO player, Transform playerTransform)
    {
        int high = chipsThisRun;
        if (TryLoad(out PlayerSaveDto dto) && dto != null)
            high = Mathf.Max(high, dto.highScoreChips);
        WriteSnapshot(dungeon, player, playerTransform, runInProgress: false, highScoreChips: high);
    }

    static void ApplyDungeon(DungeonStateSO dungeon, PlayerSaveDto dto)
    {
        if (dto.rooms == null || dto.rooms.Length == 0)
            return;

        var list = new List<RoomState>(dto.rooms.Length);
        for (int i = 0; i < dto.rooms.Length; i++)
            list.Add(FromDto(dto.rooms[i]));

        dungeon.ApplySavedDungeon(list, new Vector2Int(dto.roomCoordX, dto.roomCoordY));
    }

    static void ApplyPlayerState(PlayerStateSO player, PlayerSaveDto dto)
    {
        player.gold = dto.gold;
        player.maxHP = dto.maxHP;
        player.currentHP = dto.currentHP;
        player.healthPotionCount = dto.healthPotionCount;
        player.healthPotionHealAmount = dto.healthPotionHealAmount;
        player.damage = dto.damage;
    }

    static RoomState FromDto(RoomSaveDto d)
    {
        return new RoomState
        {
            coord = new Vector2Int(d.cx, d.cy),
            doorNorth = d.doorN,
            doorEast = d.doorE,
            doorSouth = d.doorS,
            doorWest = d.doorW,
            eventType = (RoomEventType)d.eventType,
            visited = d.visited,
            cleared = d.cleared,
            bossDefeated = d.bossDefeated,
            distanceFromStart = d.dist
        };
    }

    static RoomSaveDto[] ExportRooms(List<RoomState> rooms)
    {
        if (rooms == null)
            return Array.Empty<RoomSaveDto>();

        var arr = new RoomSaveDto[rooms.Count];
        for (int i = 0; i < rooms.Count; i++)
        {
            RoomState r = rooms[i];
            arr[i] = new RoomSaveDto
            {
                cx = r.coord.x,
                cy = r.coord.y,
                doorN = r.doorNorth,
                doorE = r.doorEast,
                doorS = r.doorSouth,
                doorW = r.doorWest,
                eventType = (int)r.eventType,
                visited = r.visited,
                cleared = r.cleared,
                bossDefeated = r.bossDefeated,
                dist = r.distanceFromStart
            };
        }

        return arr;
    }
}
