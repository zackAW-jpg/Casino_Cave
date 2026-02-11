using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DungeonState", menuName = "Game Data/Dungeon State")]
public class DungeonStateSO : ScriptableObject
{
    [Header("Runtime: where the player currently is (grid coord)")]
    public Vector2Int currentRoomCoord = Vector2Int.zero;

    [Header("Saved rooms (Unity can serialize lists)")]
    public List<RoomState> rooms = new List<RoomState>();

    // Fast lookup table (Unity does NOT serialize Dictionary, so this is runtime-only)
    private Dictionary<Vector2Int, RoomState> roomLookup = new Dictionary<Vector2Int, RoomState>();

    private void OnEnable()
    {
        RebuildLookup();
    }

    public void GenerateNewDungeon(int totalRooms, int bossMinDistance, int bossMaxDistance, int merchantCount, int treasureCount)
    {
        // 1) Reset old data
        rooms.Clear();
        roomLookup.Clear();

        // 2) Create the start room at (0,0)
        CreateRoom(Vector2Int.zero, RoomEventType.Start);
        currentRoomCoord = Vector2Int.zero;

        // 3) Grow the dungeon by adding adjacent rooms until we reach totalRooms
        // We keep a list of existing coords so we can expand from them
        List<Vector2Int> existingCoords = new List<Vector2Int>();
        existingCoords.Add(Vector2Int.zero);

        // A small safety limit so we don't infinite-loop if constraints are impossible
        int safety = 0;

        while (rooms.Count < totalRooms && safety < 100000)
        {
            safety++;

            // Pick a random existing room to branch from
            Vector2Int from = existingCoords[Random.Range(0, existingCoords.Count)];

            // Pick a random direction to try
            Vector2Int delta = GetRandomCardinalDelta();
            Vector2Int next = from + delta;

            // If a room already exists there, skip
            if (roomLookup.ContainsKey(next))
                continue;

            // Create a normal room there
            CreateRoom(next, RoomEventType.NormalEnemies);
            existingCoords.Add(next);
        }

        // 4) Compute distances from start using BFS (door-count distance)
        ComputeDistancesFromStart();

        // 5) Choose boss room in a controlled distance range
        RoomState bossRoom = PickBossRoom(bossMinDistance, bossMaxDistance);
        if (bossRoom != null)
        {
            bossRoom.eventType = RoomEventType.Boss;
        }

        // 6) Choose merchant rooms (avoid start and boss)
        AssignSpecialRooms(RoomEventType.Merchant, merchantCount);

        // 7) Choose treasure rooms (avoid start, boss, merchant)
        AssignSpecialRooms(RoomEventType.Treasure, treasureCount);

        // 8) After coords are final, compute doors from adjacency
        ComputeDoorsFromAdjacency();
    }

    public bool TryGetRoom(Vector2Int coord, out RoomState room)
    {
        return roomLookup.TryGetValue(coord, out room);
    }

    private RoomState CreateRoom(Vector2Int coord, RoomEventType type)
    {
        RoomState room = new RoomState();
        room.coord = coord;
        room.eventType = type;

        rooms.Add(room);
        roomLookup.Add(coord, room);

        return room;
    }

    private void RebuildLookup()
    {
        roomLookup.Clear();

        for (int i = 0; i < rooms.Count; i++)
        {
            // If duplicate coords exist (shouldn't), later ones overwrite earlier ones
            roomLookup[rooms[i].coord] = rooms[i];
        }
    }

    private Vector2Int GetRandomCardinalDelta()
    {
        int r = Random.Range(0, 4);

        if (r == 0) return Vector2Int.up;
        if (r == 1) return Vector2Int.right;
        if (r == 2) return Vector2Int.down;
        return Vector2Int.left;
    }

    private void ComputeDistancesFromStart()
    {
        // Reset distances
        for (int i = 0; i < rooms.Count; i++)
        {
            rooms[i].distanceFromStart = -1;
        }

        Queue<RoomState> queue = new Queue<RoomState>();

        RoomState start = roomLookup[Vector2Int.zero];
        start.distanceFromStart = 0;

        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            RoomState current = queue.Dequeue();

            // Check four neighbors
            TryVisitNeighbor(current, Vector2Int.up, queue);
            TryVisitNeighbor(current, Vector2Int.right, queue);
            TryVisitNeighbor(current, Vector2Int.down, queue);
            TryVisitNeighbor(current, Vector2Int.left, queue);
        }
    }

    private void TryVisitNeighbor(RoomState current, Vector2Int delta, Queue<RoomState> queue)
    {
        Vector2Int neighborCoord = current.coord + delta;

        if (!roomLookup.TryGetValue(neighborCoord, out RoomState neighbor))
            return;

        if (neighbor.distanceFromStart != -1)
            return;

        neighbor.distanceFromStart = current.distanceFromStart + 1;
        queue.Enqueue(neighbor);
    }

    private RoomState PickBossRoom(int minDist, int maxDist)
    {
        // Collect candidates in distance range, not the start room
        List<RoomState> candidates = new List<RoomState>();

        for (int i = 0; i < rooms.Count; i++)
        {
            RoomState r = rooms[i];

            if (r.eventType == RoomEventType.Start)
                continue;

            if (r.distanceFromStart >= minDist && r.distanceFromStart <= maxDist)
                candidates.Add(r);
        }

        // If none in range, fall back to the farthest room
        if (candidates.Count == 0)
        {
            RoomState farthest = null;

            for (int i = 0; i < rooms.Count; i++)
            {
                RoomState r = rooms[i];

                if (r.eventType == RoomEventType.Start)
                    continue;

                if (farthest == null || r.distanceFromStart > farthest.distanceFromStart)
                    farthest = r;
            }

            return farthest;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private void AssignSpecialRooms(RoomEventType specialType, int count)
    {
        int assigned = 0;
        int safety = 0;

        while (assigned < count && safety < 100000)
        {
            safety++;

            RoomState candidate = rooms[Random.Range(0, rooms.Count)];

            // Avoid overriding important rooms
            if (candidate.eventType == RoomEventType.Start)
                continue;

            if (candidate.eventType == RoomEventType.Boss)
                continue;

            // Avoid placing on a room that is already special
            if (candidate.eventType != RoomEventType.NormalEnemies)
                continue;

            candidate.eventType = specialType;
            assigned++;
        }
    }

    private void ComputeDoorsFromAdjacency()
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            RoomState r = rooms[i];

            r.doorNorth = roomLookup.ContainsKey(r.coord + Vector2Int.up);
            r.doorEast = roomLookup.ContainsKey(r.coord + Vector2Int.right);
            r.doorSouth = roomLookup.ContainsKey(r.coord + Vector2Int.down);
            r.doorWest = roomLookup.ContainsKey(r.coord + Vector2Int.left);
        }
    }
}
