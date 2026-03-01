using UnityEngine;

public class DungeonRoomController : MonoBehaviour
{
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

    private void Start()
    {
        if (dungeonState == null)
        {
            Debug.LogError("DungeonRoomController has no DungeonStateSO assigned.", this);
            return;
        }

        UpdateRoomView();
        MovePlayerToSpawnInCurrentRoomOnStart();
    }

    public void TryMoveToAdjacentRoom(DoorDirection direction)
    {
        if (dungeonState == null)
        {
            Debug.LogError("DungeonRoomController has no DungeonStateSO assigned.", this);
            return;
        }

        Vector2Int current = dungeonState.currentRoomCoord;
        Vector2Int delta = GetDeltaForDirection(direction);
        Vector2Int targetCoord = current + delta;

        if (!dungeonState.TryGetRoom(targetCoord, out RoomState targetRoom))
        {
            Debug.LogWarning($"No room exists at {targetCoord} for direction {direction}.", this);
            return;
        }

        dungeonState.currentRoomCoord = targetCoord;

        UpdateRoomView();
        MovePlayerToSpawnInNewRoom(direction);
    }

    private Vector2Int GetDeltaForDirection(DoorDirection direction)
    {
        switch (direction)
        {
            case DoorDirection.North:
                return Vector2Int.up;
            case DoorDirection.East:
                return Vector2Int.right;
            case DoorDirection.South:
                return Vector2Int.down;
            case DoorDirection.West:
                return Vector2Int.left;
            default:
                return Vector2Int.zero;
        }
    }

    private void UpdateRoomView()
    {
        if (!dungeonState.TryGetRoom(dungeonState.currentRoomCoord, out RoomState room))
        {
            Debug.LogError($"No RoomState found at coord {dungeonState.currentRoomCoord}.", this);
            return;
        }

        if (doorNorth != null) doorNorth.SetActive(room.doorNorth);
        if (doorEast != null) doorEast.SetActive(room.doorEast);
        if (doorSouth != null) doorSouth.SetActive(room.doorSouth);
        if (doorWest != null) doorWest.SetActive(room.doorWest);
    }

    private void MovePlayerToSpawnInCurrentRoomOnStart()
    {
        if (playerTransform == null) return;

        if (!dungeonState.TryGetRoom(dungeonState.currentRoomCoord, out RoomState room))
        {
            return;
        }

        playerTransform.position = spawnNorth != null ? spawnNorth.position : playerTransform.position;
    }

    private void MovePlayerToSpawnInNewRoom(DoorDirection enteredFromDirection)
    {
        if (playerTransform == null) return;

        Transform spawn = null;

        switch (enteredFromDirection)
        {
            case DoorDirection.North:
                spawn = spawnSouth;
                break;
            case DoorDirection.East:
                spawn = spawnWest;
                break;
            case DoorDirection.South:
                spawn = spawnNorth;
                break;
            case DoorDirection.West:
                spawn = spawnEast;
                break;
        }

        if (spawn != null)
        {
            playerTransform.position = spawn.position;
        }
    }
}