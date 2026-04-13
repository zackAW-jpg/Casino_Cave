using System;
using UnityEngine;

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

    
    public static void ResetForNewRun()
    {
        SetSlotsUnlocked(false);
    }

    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnDomainReload()
    {
        _slotsUnlocked = false;
    }
}
