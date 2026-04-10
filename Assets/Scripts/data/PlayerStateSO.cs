using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerState",
    menuName = "Game Data/Player State"
)]
public class PlayerStateSO : ScriptableObject
{
    [Header("Health")]
    public int maxHP = 10;
    public int currentHP = 10;

    [Header("Currency")]
    public int gold = 0;

    [Header("Health potions")]
    [Tooltip("Runtime stack size; initialized from PlayerHealth.startingHealthPotions at run start.")]
    public int healthPotionCount;
    public int healthPotionHealAmount = 4;

    [Header("Stats")]
    public int damage = 1;
}
