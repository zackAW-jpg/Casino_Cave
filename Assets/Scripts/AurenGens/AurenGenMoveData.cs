using UnityEngine;

[CreateAssetMenu(menuName = "Casino Cave/AurenGens/Move", fileName = "AurenGenMove")]
public class AurenGenMoveData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Move name shown in battle.")]
    public string moveName = "Move";

    [Tooltip("Planning grid slot 1–6 (TL, TR, ML, MR, BL, BR).")]
    [Range(1, 6)]
    public int menuSlotIndex = 1;

    [Tooltip("Quick move text.")]
    [TextArea(1, 2)]
    public string description = "No description.";

    [Header("Power")]
    [Tooltip("Base debt damage.")]
    [Min(0)]
    public int baseDamage = 10;

    [Tooltip("Roll needed to land.")]
    [Min(2)]
    public int hitThreshold = 10;

    [Tooltip("Marks this as a special move.")]
    public bool isSpecialMove;

    [Tooltip("True for Debt Collector.")]
    public bool isDebtCollectorMove;

    [Tooltip("Ride the Bus minigame move.")]
    public bool isRideTheBusMove;

    [Tooltip("Swerve dodge on hit for this round.")]
    public bool isSwerveMove;

    [Header("Condition")]
    [Tooltip("Condition to apply on hit.")]
    public AurenGenConditionType inflictsCondition = AurenGenConditionType.None;

    [Tooltip("Chance to apply condition.")]
    [Range(0f, 1f)]
    public float conditionChance;

    [Tooltip("Who gets the condition.")]
    public AurenGenEffectTargetSide conditionTargetSide = AurenGenEffectTargetSide.Enemy;

    [Header("Roll effects")]
    [Tooltip("Bonus to your next roll.")]
    public int selfNextRollModifier;

    [Tooltip("Penalty to enemy next roll.")]
    public int enemyNextRollModifier;

    [Header("Effect modules")]
    [Tooltip("Additional move effect modules.")]
    public AurenGenMoveEffect[] effects;
}

public enum AurenGenConditionType
{
    None = 0,
    Good = 1,
    Bad = 2
}
