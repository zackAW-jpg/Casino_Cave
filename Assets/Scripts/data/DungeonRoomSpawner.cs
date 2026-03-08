using System.Collections.Generic;
using UnityEngine;

public class DungeonRoomSpawner : MonoBehaviour
{
    public DungeonStateSO dungeonState;
    public GameObject roomPrefab;
    public float roomWorldSize = 20f;
    public Transform playerTransform;

    [Header("Start room spawn")]
    [Tooltip("Which spawn point in the start room (0,0) to place the player at.")]
    public DoorDirection startRoomSpawnSide = DoorDirection.North;

    private Dictionary<Vector2Int, DungeonRoomController> _roomInstances = new Dictionary<Vector2Int, DungeonRoomController>();

  
    private void BuildDungeon()
    {
        _roomInstances.Clear();

        foreach (RoomState roomState in dungeonState.rooms)
        {
            Vector3 worldPos = new Vector3(
                roomState.coord.x * roomWorldSize,
                roomState.coord.y * roomWorldSize,
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
        }

        Debug.Log($"DungeonRoomSpawner: Spawned {_roomInstances.Count} rooms.");
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
        PlacePlayerInStartRoom();
    }
}
