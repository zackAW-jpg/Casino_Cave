using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DungeonState", menuName = "Game Data/Dungeon State")]
public class DungeonStateSO : ScriptableObject
{
    [Header("Where the player is in the dungeon grid")]
    public Vector2Int currentRoomCoord = Vector2Int.zero;

    [Header("All generated rooms (for now, stored as a list)")]
    public List<RoomState> rooms = new List<RoomState>();

    public bool TryGetRoom(Vector2Int coord, out RoomState room)
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i].coord == coord)
            {
                room = rooms[i];
                return true;
            }
        }

        room = null;
        return false;
    }

    public RoomState GetOrCreateRoom(Vector2Int coord)
    {
        if (TryGetRoom(coord, out RoomState existing))
            return existing;

        RoomState created = new RoomState();
        created.coord = coord;

        rooms.Add(created);
        return created;
    }

    public void ClearAllRooms()
    {
        rooms.Clear();
        currentRoomCoord = Vector2Int.zero;
    }
}
