using UnityEngine;

/// <summary>
/// Lets <see cref="DungeonBootstrap"/> hydrate the save before <see cref="PlayerHealth.Awake"/> runs default init.
/// </summary>
public static class ContinueLoadGuard
{
    static bool _pendingSkipPlayerHealthInit;

    public static bool ConsumeSkipPlayerHealthInit()
    {
        if (!_pendingSkipPlayerHealthInit)
            return false;
        _pendingSkipPlayerHealthInit = false;
        return true;
    }

    public static void MarkPendingSkipPlayerHealthInit() => _pendingSkipPlayerHealthInit = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => _pendingSkipPlayerHealthInit = false;
}
