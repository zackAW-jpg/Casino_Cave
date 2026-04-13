using UnityEngine;

/// <summary>
/// Wired at dungeon boot so room transitions and pickups can autosave without inspector chains.
/// </summary>
public static class GameplaySaveContext
{
    public static DungeonStateSO Dungeon;
    public static PlayerStateSO Player;
    public static Transform PlayerTransform;

    public static void Bind(DungeonStateSO dungeon, PlayerStateSO player, Transform playerTransform)
    {
        Dungeon = dungeon;
        Player = player;
        PlayerTransform = playerTransform;
    }

    public static void PersistRun()
    {
        if (Dungeon != null && Player != null)
            PlayerSaveStore.PersistRunInProgress(Dungeon, Player, PlayerTransform);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Dungeon = null;
        Player = null;
        PlayerTransform = null;
    }
}
