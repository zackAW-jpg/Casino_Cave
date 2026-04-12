using System;
using UnityEngine;

/// <summary>
/// Session flag: slot HUD + gambling arm stay off until something (e.g. tutorial NPC) unlocks them.
/// </summary>
public static class GamblingArmRuntimeState
{
    private static bool _slotsUnlocked;

    public static bool SlotsUnlocked => _slotsUnlocked;

    public static event Action SlotsUnlockedChanged;

    public static void SetSlotsUnlocked(bool value)
    {
        if (_slotsUnlocked == value)
            return;

        _slotsUnlocked = value;
        SlotsUnlockedChanged?.Invoke();
    }

    /// <summary>Call when starting a new run / scene so the tutorial gate applies again.</summary>
    public static void ResetForNewRun()
    {
        SetSlotsUnlocked(false);
    }

    /// <summary>Clears static state when the domain reloads (normal Play Mode). Needed so a previous run cannot leave slots unlocked.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnDomainReload()
    {
        _slotsUnlocked = false;
    }
}
