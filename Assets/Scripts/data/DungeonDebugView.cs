using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DungeonDebugView : MonoBehaviour
{
    [Header("Data")]
    public DungeonStateSO dungeonState;
    [Header("Visual Prefab")]
    public SpriteRenderer markerPrefab;

    [Header("Layout (match DungeonRoomSpawner)")]
    [Tooltip("If set, marker positions use the same spacing as this spawner (recommended).")]
    public DungeonRoomSpawner layoutFromSpawner;

    [Tooltip("Vertical spacing between room centers / markers (used when Layout From Spawner is empty).")]
    [FormerlySerializedAs("roomWorldSize")]
    public float roomSpacingY = 20f;

    [Tooltip("Horizontal spacing when not using Layout From Spawner. 0 = use roomSpacingY on X too.")]
    public float roomSpacingX = 0f;

    private readonly List<SpriteRenderer> spawnedMarkers = new List<SpriteRenderer>();

    /// <summary>
    /// Called from <see cref="DungeonBootstrap"/> after rooms are spawned. Call manually if needed.
    /// </summary>
    public void DrawDungeon()
    {
        if (dungeonState == null)
        {
            Debug.LogError("DungeonDebugView: dungeonState is not assigned.");
            return;
        }

        if (markerPrefab == null)
        {
            Debug.LogError("DungeonDebugView: markerPrefab is not assigned.");
            return;
        }

        ClearDungeonMarkers();

        for (int i = 0; i < dungeonState.rooms.Count; i++)
        {
            RoomState room = dungeonState.rooms[i];

            float sx, sy;
            if (layoutFromSpawner != null)
            {
                sx = layoutFromSpawner.GetEffectiveSpacingX();
                sy = layoutFromSpawner.GetEffectiveSpacingY();
            }
            else
            {
                sx = roomSpacingX > 0f ? roomSpacingX : roomSpacingY;
                sy = roomSpacingY;
            }

            Vector3 worldPos;
            DungeonRoomController roomInstance =
                layoutFromSpawner != null ? layoutFromSpawner.GetRoomAt(room.coord) : null;

            if (roomInstance != null)
            {
                worldPos = roomInstance.transform.position;
            }
            else
            {
                worldPos = new Vector3(
                    room.coord.x * sx,
                    room.coord.y * sy,
                    0f
                );
                if (layoutFromSpawner != null && layoutFromSpawner.roomPrefab != null)
                    worldPos += GetRoomPrefabVisualCenterOffset(layoutFromSpawner.roomPrefab);
            }

            SpriteRenderer marker = Instantiate(markerPrefab, worldPos, Quaternion.identity, transform);
            marker.name = $"Room_{room.coord.x}_{room.coord.y}_{room.eventType}";

            SnapSpritePivotToPosition(marker, worldPos);
            // Choose a color based on room type (debug visualization)
            marker.color = GetColorForRoom(room.eventType);

            spawnedMarkers.Add(marker);
        }

        Debug.Log($"DungeonDebugView: Drew {spawnedMarkers.Count} room markers.");
    }

    private void ClearDungeonMarkers()
    {
        for (int i = 0; i < spawnedMarkers.Count; i++)
        {
            if (spawnedMarkers[i] != null)
            {
                Destroy(spawnedMarkers[i].gameObject);
            }
        }

        spawnedMarkers.Clear();
    }

    /// <summary>
    /// Offset from room prefab root to the center of its combined sprite bounds (same idea as spawner footprint).
    /// </summary>
    private static Vector3 GetRoomPrefabVisualCenterOffset(GameObject prefab)
    {
        SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0)
            return Vector3.zero;

        bool init = false;
        Bounds enc = default;
        foreach (SpriteRenderer sr in renderers)
        {
            if (!sr.enabled) continue;
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
            return Vector3.zero;

        return enc.center - prefab.transform.position;
    }

    private static void SnapSpritePivotToPosition(SpriteRenderer sr, Vector3 worldPoint)
    {
        if (sr == null) return;
        Vector3 delta = worldPoint - sr.bounds.center;
        sr.transform.position += delta;
    }

    private Color GetColorForRoom(RoomEventType type)
    {
        // These are just debug colors; tweak as you like
        switch (type)
        {
            case RoomEventType.Start: return Color.green;
            case RoomEventType.Boss: return Color.red;
            case RoomEventType.Merchant: return Color.cyan;
            case RoomEventType.Treasure: return Color.yellow;
            case RoomEventType.NormalEnemies: return Color.white;
            default: return Color.gray;
        }
    }
}