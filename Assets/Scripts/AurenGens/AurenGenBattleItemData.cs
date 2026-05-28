using UnityEngine;

[CreateAssetMenu(menuName = "Casino Cave/AurenGens/Battle Item", fileName = "AurenGenBattleItem")]
public class AurenGenBattleItemData : ScriptableObject
{
    [Tooltip("Item name shown in battle.")]
    public string itemName = "Item";

    [Tooltip("Quick item text.")]
    [TextArea(1, 2)]
    public string description = "No description.";

    [Tooltip("Heal debt points.")]
    [Min(0)]
    public int healDebtPoints;

    [Tooltip("Bonus to your roll this round.")]
    public int roundRollBonus;

    [Tooltip("Penalty to enemy roll this round.")]
    public int enemyRoundRollPenalty;
}
