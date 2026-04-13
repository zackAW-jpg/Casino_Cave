using UnityEngine;

/// <summary>
/// Central flags so movement, combat, pause, and beacon prompts do not fight each other.
/// </summary>
public static class GameplayInputGate
{
    public static bool DungeonBootComplete { get; private set; }
    public static int MenuModalDepth { get; private set; }
    static bool _pauseMenuOpen;

    public static bool PlayerWorldActionsEnabled =>
        DungeonBootComplete && MenuModalDepth == 0 && !_pauseMenuOpen;

    /// <summary>Esc is allowed when boot finished and no modal overlays (death, intro, victory, leave prompt).</summary>
    public static bool CanTogglePause =>
        DungeonBootComplete && MenuModalDepth == 0;

    public static void SetDungeonBootComplete(bool value) => DungeonBootComplete = value;

    public static void PushMenuModal() => MenuModalDepth++;

    public static void PopMenuModal() => MenuModalDepth = Mathf.Max(0, MenuModalDepth - 1);

    public static void SetPauseMenuOpen(bool open)
    {
        _pauseMenuOpen = open;
        Time.timeScale = open ? 0f : 1f;
    }

    public static void ResetForTitleOrFreshSession()
    {
        DungeonBootComplete = false;
        MenuModalDepth = 0;
        _pauseMenuOpen = false;
        Time.timeScale = 1f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnDomainReload() => ResetForTitleOrFreshSession();
}
