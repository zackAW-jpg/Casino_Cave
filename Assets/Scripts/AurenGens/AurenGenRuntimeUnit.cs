using UnityEngine;

[System.Serializable]
public class AurenGenRuntimeUnit
{
    public AurenGenDefinition Definition { get; private set; }
    public int CurrentDebtPoints { get; private set; }
    public bool IsBroken { get; private set; }
    public bool SwitchUsedThisMatch { get; private set; }
    public bool DebtCollectorUsedThisMatch { get; private set; }
    public int ConsecutiveTurnsActive { get; private set; }
    public AurenGenConditionType Condition { get; private set; }
    public int NextRollModifier { get; private set; }
    bool _hasLockedNextRawRoll;
    int _lockedNextRawRoll;
    bool _guaranteedNextAttack;
    bool _hasParryGuess;
    int _parryGuess;
    public bool SwerveDodgeActive { get; private set; }
    public int RideBusCompleteStreak { get; private set; }
    public bool RideBusNeedsBuyIn { get; private set; }
    public int RideBusLastBet { get; private set; }

    public void BeginMatch(AurenGenDefinition definition)
    {
        Definition = definition;
        CurrentDebtPoints = definition == null ? 1 : Mathf.Max(1, definition.maxDebtPoints);
        IsBroken = false;
        SwitchUsedThisMatch = false;
        DebtCollectorUsedThisMatch = false;
        ConsecutiveTurnsActive = 0;
        Condition = AurenGenConditionType.None;
        NextRollModifier = 0;
        _hasLockedNextRawRoll = false;
        _lockedNextRawRoll = 0;
        _guaranteedNextAttack = false;
        _hasParryGuess = false;
        _parryGuess = 0;
        SwerveDodgeActive = false;
        RideBusCompleteStreak = 0;
        RideBusNeedsBuyIn = true;
        RideBusLastBet = 0;
    }

    public int MaxDebtPoints()
    {
        return Definition == null ? 1 : Mathf.Max(1, Definition.maxDebtPoints);
    }

    public float DamageMultiplier()
    {
        if (Definition == null)
            return 1f;
        return Mathf.Max(0.01f, Definition.damageMultiplier);
    }

    public void MarkTurnStayedIn()
    {
        ConsecutiveTurnsActive++;
    }

    public void MarkSwitchedOut()
    {
        SwitchUsedThisMatch = true;
        ConsecutiveTurnsActive = 0;
    }

    public void MarkForcedOut()
    {
        ConsecutiveTurnsActive = 0;
    }

    public bool CanUseDebtCollector()
    {
        if (Definition == null)
            return false;
        if (!Definition.isUltimateVariant)
            return false;
        if (DebtCollectorUsedThisMatch)
            return false;
        return ConsecutiveTurnsActive >= 3;
    }

    public void MarkDebtCollectorUsed()
    {
        DebtCollectorUsedThisMatch = true;
    }

    public int ApplyDamage(int amount)
    {
        if (IsBroken)
            return 0;
        int applied = Mathf.Max(0, amount);
        CurrentDebtPoints -= applied;
        if (CurrentDebtPoints <= 0)
        {
            CurrentDebtPoints = 0;
            IsBroken = true;
            Condition = AurenGenConditionType.None;
        }
        return applied;
    }

    public void SetCondition(AurenGenConditionType condition)
    {
        if (IsBroken)
            return;
        Condition = condition;
    }

    public void AddNextRollModifier(int amount)
    {
        NextRollModifier += amount;
    }

    public int ConsumeNextRollModifier()
    {
        int value = NextRollModifier;
        NextRollModifier = 0;
        return value;
    }

    public void HealDebt(int amount)
    {
        if (IsBroken)
            return;
        if (amount <= 0)
            return;
        CurrentDebtPoints = Mathf.Min(MaxDebtPoints(), CurrentDebtPoints + amount);
    }

    public void LockNextRawRoll(int rawRoll)
    {
        _hasLockedNextRawRoll = true;
        _lockedNextRawRoll = Mathf.Clamp(rawRoll, 1, 20);
    }

    public bool TryConsumeLockedNextRawRoll(out int rawRoll)
    {
        if (_hasLockedNextRawRoll)
        {
            _hasLockedNextRawRoll = false;
            rawRoll = _lockedNextRawRoll;
            return true;
        }
        rawRoll = 0;
        return false;
    }

    public void GrantGuaranteedNextAttack()
    {
        _guaranteedNextAttack = true;
    }

    public bool ConsumeGuaranteedNextAttack()
    {
        if (!_guaranteedNextAttack)
            return false;
        _guaranteedNextAttack = false;
        return true;
    }

    public void SetParryGuess(int guess)
    {
        _hasParryGuess = true;
        _parryGuess = Mathf.Clamp(guess, 1, 20);
    }

    public bool TryGetParryGuess(out int guess)
    {
        if (_hasParryGuess)
        {
            guess = _parryGuess;
            return true;
        }
        guess = 0;
        return false;
    }

    public void ClearParryGuess()
    {
        _hasParryGuess = false;
    }

    public void ClearSwerveDodge()
    {
        SwerveDodgeActive = false;
    }

    public void ActivateSwerveDodge()
    {
        SwerveDodgeActive = true;
    }

    public bool TryConsumeSwerveDodge()
    {
        if (!SwerveDodgeActive)
            return false;
        SwerveDodgeActive = false;
        return true;
    }

    public void RideBusOnCardFail()
    {
        RideBusCompleteStreak = 0;
        RideBusNeedsBuyIn = true;
    }

    public void RideBusOnFullSuccess()
    {
        RideBusCompleteStreak++;
        RideBusNeedsBuyIn = true;
    }

    public void RideBusSetBet(int betDebt)
    {
        RideBusLastBet = Mathf.Max(0, betDebt);
        RideBusNeedsBuyIn = false;
    }

    public int GetRideBusDamageMultiplier()
    {
        int idx = Mathf.Clamp(RideBusCompleteStreak, 0, 3);
        int[] table = { 2, 4, 10, 40 };
        return table[idx];
    }

    public int PreviewRideBusDamage(int betDebt)
    {
        return Mathf.Max(0, betDebt * GetRideBusDamageMultiplier());
    }

    public bool SpendDebt(int amount)
    {
        if (IsBroken || amount <= 0)
            return false;
        if (CurrentDebtPoints <= amount)
            return false;
        CurrentDebtPoints -= amount;
        if (CurrentDebtPoints <= 0)
        {
            CurrentDebtPoints = 0;
            IsBroken = true;
            Condition = AurenGenConditionType.None;
        }
        return !IsBroken;
    }
}
