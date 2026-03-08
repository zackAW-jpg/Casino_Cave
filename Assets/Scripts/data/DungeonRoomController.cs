using UnityEngine;

public class DungeonRoomController : MonoBehaviour
{
    [Header("Set by DungeonRoomSpawner at runtime")]
    public Vector2Int coord;
    public DungeonRoomSpawner spawner;
    public DungeonStateSO dungeonState;
    public Transform playerTransform;

    [Header("Door GameObjects")]
    public GameObject doorNorth;
    public GameObject doorEast;
    public GameObject doorSouth;
    public GameObject doorWest;

    [Header("Spawn Points")]
    public Transform spawnNorth;
    public Transform spawnEast;
    public Transform spawnSouth;
    public Transform spawnWest;

    public void SetupFromRoomState(RoomState room)
    {
        if (doorNorth != null) doorNorth.SetActive(room.doorNorth);
        if (doorEast != null) doorEast.SetActive(room.doorEast);
        if (doorSouth != null) doorSouth.SetActive(room.doorSouth);
        if (doorWest != null) doorWest.SetActive(room.doorWest);
    }


    public Transform GetSpawnForSide(DoorDirection side)
    {
        switch (side)
        {
            case DoorDirection.North: return spawnNorth;
            case DoorDirection.East: return spawnEast;
            case DoorDirection.South: return spawnSouth;
            case DoorDirection.West: return spawnWest;
            default: return null;
        }
    }

    public void TryMoveToAdjacentRoom(DoorDirection direction)
    {
        if (dungeonState == null)
        {
            Debug.LogError("DungeonRoomController has no DungeonStateSO assigned.", this);
            return;
        }

        if (spawner == null)
        {
            Debug.LogError("DungeonRoomController has no spawner assigned. Use DungeonRoomSpawner to build the dungeon.", this);
            return;
        }

        Vector2Int delta = GetDeltaForDirection(direction);
        Vector2Int targetCoord = coord + delta;

        if (!dungeonState.TryGetRoom(targetCoord, out _))
        {
            Debug.LogWarning($"No room exists at {targetCoord} for direction {direction}.", this);
            return;
        }

        DungeonRoomController targetRoom = spawner.GetRoomAt(targetCoord);
        if (targetRoom == null)
        {
            Debug.LogWarning($"No room instance at {targetCoord}.", this);
            return;
        }

        dungeonState.currentRoomCoord = targetCoord;
        MovePlayerToSpawnInNewRoom(direction, targetRoom);
    }

    private static Vector2Int GetDeltaForDirection(DoorDirection direction)
    {
        switch (direction)
        {
            case DoorDirection.North: return Vector2Int.up;
            case DoorDirection.East: return Vector2Int.right;
            case DoorDirection.South: return Vector2Int.down;
            case DoorDirection.West: return Vector2Int.left;
            default: return Vector2Int.zero;
        }
    }


    private void MovePlayerToSpawnInNewRoom(DoorDirection enteredFromDirection, DungeonRoomController targetRoom)
    {
        if (playerTransform == null) return;

        DoorDirection spawnSide = GetOppositeDirection(enteredFromDirection);
        Transform spawn = targetRoom.GetSpawnForSide(spawnSide);

        if (spawn != null)
        {
            playerTransform.position = spawn.position;
        }
    }

    private static DoorDirection GetOppositeDirection(DoorDirection direction)
    {
        switch (direction)
        {
            case DoorDirection.North: return DoorDirection.South;
            case DoorDirection.East: return DoorDirection.West;
            case DoorDirection.South: return DoorDirection.North;
            case DoorDirection.West: return DoorDirection.East;
            default: return direction;
        }
    }
}