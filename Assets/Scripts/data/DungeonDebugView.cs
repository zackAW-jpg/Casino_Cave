using System.Collections.Generic;
using UnityEngine;

public class DungeonDebugView : MonoBehaviour
{
    [Header("Data")]
    public DungeonStateSO dungeonState;

    [Header("Visual Prefab")]
    public SpriteRenderer markerPrefab;

    [Header("Layout")]
    public float roomWorldSize = 20f;

    // We keep track of spawned markers so we can clear and redraw
    private readonly List<SpriteRenderer> spawnedMarkers = new List<SpriteRenderer>();

    void Start()
    {
        DrawDungeon();
    }

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

            Vector3 worldPos = new Vector3(
                room.coord.x * roomWorldSize,
                room.coord.y * roomWorldSize,
                0f
            );

            SpriteRenderer marker = Instantiate(markerPrefab, worldPos, Quaternion.identity, transform);
            marker.name = $"Room_{room.coord.x}_{room.coord.y}_{room.eventType}";

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