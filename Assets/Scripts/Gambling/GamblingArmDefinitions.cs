using System.Collections.Generic;
using UnityEngine;

public static class GamblingArmDefinitions
{
    public static readonly IReadOnlyList<GamblingAttackType> AttackTypes = new[]
    {
        GamblingAttackType.Punch,
        GamblingAttackType.ShurikenThrow,
        GamblingAttackType.ExtendedPunch,
        GamblingAttackType.HammerSlam
    };

    public static readonly IReadOnlyList<GamblingModifierType> Modifiers = new[]
    {
        GamblingModifierType.Mushroom,
        GamblingModifierType.Fire,
        GamblingModifierType.Ice,
        GamblingModifierType.Paddle,
        GamblingModifierType.Gold
    };

    public static GamblingAttackType RandomAttackType()
    {
        var list = AttackTypes;
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    public static GamblingModifierType RandomModifierType()
    {
        var list = Modifiers;
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    public static GamblingAttackType RandomCritSymbol()
    {
        
        return RandomAttackType();
    }
}
