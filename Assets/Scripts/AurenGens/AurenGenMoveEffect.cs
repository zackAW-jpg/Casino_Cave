using UnityEngine;

public enum AurenGenMoveEffectTiming
{
    AfterItems = 0,
    BeforeRoll = 1,
    AfterRoll = 2,
    BeforeAttack = 3,
    AfterAttack = 4,
    /// <summary>After item effects resolve, before any d20 is rolled. Use context.QueuePreRollPresentation to insert narrative; skipped if queue empty.</summary>
    AfterItemsPreRoll = 5
}

public enum AurenGenEffectTargetSide
{
    Self = 0,
    Enemy = 1
}

public abstract class AurenGenMoveEffect : ScriptableObject
{
    [Tooltip("When this effect resolves.")]
    public AurenGenMoveEffectTiming timing = AurenGenMoveEffectTiming.AfterRoll;

    public virtual void Execute(AurenGenMoveEffectContext context)
    {
    }
}

public class AurenGenMoveEffectContext
{
    public AurenGenBattleController controller;
    public AurenGenRuntimeUnit attacker;
    public AurenGenRuntimeUnit defender;
    public AurenGenMoveData move;
    public bool attackerIsPlayer;
    public int attackerRawRoll;
    public int defenderRawRoll;
    public int attackerFinalRoll;
    public int defenderFinalRoll;
    public bool cancelAttackerMove;
    public bool cancelDefenderMove;

    public void QueuePreRollPresentation(string line)
    {
        controller?.EnqueuePreRollPresentation(line);
    }

    public void QueueDiceRevealPresentation(string line)
    {
        controller?.EnqueueDiceRevealPresentation(line);
    }
}
