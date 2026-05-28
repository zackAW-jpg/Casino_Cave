using UnityEngine;

[CreateAssetMenu(menuName = "Casino Cave/AurenGens/Effects/Parry", fileName = "AurenGenEffect_Parry")]
public class AurenGenParryEffect : AurenGenMoveEffect
{
    [Tooltip("Guess fallback for enemy AI.")]
    [Range(1, 20)]
    public int enemyAIGuess = 10;

    public override void Execute(AurenGenMoveEffectContext context)
    {
        if (context == null)
            return;

        if (timing == AurenGenMoveEffectTiming.BeforeRoll)
        {
            int guess = context.controller.ResolveParryGuess(context.attackerIsPlayer, enemyAIGuess);
            context.attacker.SetParryGuess(guess);
            context.controller.ShowParryGuess(context.attackerIsPlayer, guess);
            return;
        }

        if (timing == AurenGenMoveEffectTiming.AfterRoll)
        {
            if (!context.attacker.TryGetParryGuess(out int guess))
                return;
            bool success = context.defenderRawRoll == guess;
            string attackerName = context.attacker.Definition != null ? context.attacker.Definition.displayName : "AurenGen";
            if (success)
                context.QueueDiceRevealPresentation(attackerName + " parried for roll " + context.defenderRawRoll + "!");
            else
                context.QueueDiceRevealPresentation(attackerName + " parry failed (guess " + guess + ", roll " + context.defenderRawRoll + ").");
            if (!success)
                return;

            context.cancelDefenderMove = true;
            context.attacker.GrantGuaranteedNextAttack();
        }
    }
}
