using UnityEngine;

[CreateAssetMenu(menuName = "Casino Cave/AurenGens/Effects/Lockjaw", fileName = "AurenGenEffect_Lockjaw")]
public class AurenGenLockjawEffect : AurenGenMoveEffect
{
    [Tooltip("Who gets the locked roll.")]
    public AurenGenEffectTargetSide targetSide = AurenGenEffectTargetSide.Enemy;

    public override void Execute(AurenGenMoveEffectContext context)
    {
        if (context == null)
            return;
        if (timing != AurenGenMoveEffectTiming.AfterRoll)
            return;

        AurenGenRuntimeUnit target = targetSide == AurenGenEffectTargetSide.Self ? context.attacker : context.defender;
        int lockedValue = targetSide == AurenGenEffectTargetSide.Self ? context.attackerRawRoll : context.defenderRawRoll;
        if (target == null)
            return;
        target.LockNextRawRoll(lockedValue);
        string msg = targetSide == AurenGenEffectTargetSide.Self
            ? "Lockjaw locked your next roll at " + lockedValue + "."
            : "Lockjaw locked the enemy next roll at " + lockedValue + ".";
        context.QueueDiceRevealPresentation(msg);
    }
}
