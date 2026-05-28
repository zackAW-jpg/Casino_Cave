using UnityEngine;

[CreateAssetMenu(menuName = "Casino Cave/AurenGens/Definition", fileName = "AurenGenDefinition")]
public class AurenGenDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique id for this AurenGen.")]
    public string aurenGenId = "aurengen_id";

    [Tooltip("Name shown in battle.")]
    public string displayName = "AurenGen";

    [Tooltip("Visual variant label.")]
    public string variantName = "Base";

    [Header("Stats")]
    [Tooltip("Max debt points.")]
    [Min(1)]
    public int maxDebtPoints = 100;

    [Tooltip("Small variant power modifier.")]
    [Range(0.75f, 1.5f)]
    public float damageMultiplier = 1f;

    [Header("Ultimate")]
    [Tooltip("Can use Debt Collector.")]
    public bool isUltimateVariant;

    [Header("Battle visuals")]
    [Tooltip("Sprite shown on player side.")]
    public Sprite backSprite;

    [Tooltip("Sprite shown on enemy side.")]
    public Sprite frontSprite;

    [Tooltip("Animator used on player side.")]
    public RuntimeAnimatorController backAnimatorController;

    [Tooltip("Animator used on enemy side.")]
    public RuntimeAnimatorController frontAnimatorController;

    [Header("Moves")]
    [Tooltip("Moves (max 6). Grid slots use each move's menuSlotIndex.")]
    public AurenGenMoveData[] moves = new AurenGenMoveData[6];
}
