/// <summary>Outcome of one full slot machine roll.</summary>
public struct GamblingArmRollResult
{
    public GamblingAttackType Attack;
    public GamblingModifierType Modifier;
    /// <summary>Right-slot symbol (same enum as attack). Crit if it matches <see cref="Attack"/>.</summary>
    public GamblingAttackType CritSymbol;
    public bool IsCrit;

    public static GamblingArmRollResult RollNew()
    {
        var attack = GamblingArmDefinitions.RandomAttackType();
        var mod = GamblingArmDefinitions.RandomModifierType();
        var critSym = GamblingArmDefinitions.RandomCritSymbol();
        bool crit = critSym == attack;
        return new GamblingArmRollResult
        {
            Attack = attack,
            Modifier = mod,
            CritSymbol = critSym,
            IsCrit = crit
        };
    }
}
