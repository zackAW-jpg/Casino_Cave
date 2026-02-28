using UnityEngine;

public class DungeonBootstrap : MonoBehaviour
{
    public DungeonStateSO dungeonState;

    public int totalRooms = 25;
    public int bossMinDistance = 6;
    public int bossMaxDistance = 10;
    public int merchantCount = 1;
    public int treasureCount = 2;

    void Start()
    {
        if (dungeonState == null)
        {
            Debug.LogError("DungeonBootstrap: dungeonState is not assigned.");
            return;
        }

        dungeonState.GenerateNewDungeon(totalRooms, bossMinDistance, bossMaxDistance, merchantCount, treasureCount);
    }
}