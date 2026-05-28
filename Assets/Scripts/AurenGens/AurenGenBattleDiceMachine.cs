using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Center-field d20 presentation: player value left, enemy value right.
/// Wire Animator optional triggers Roll, PlayerRollSkin, EnemyRollSkin (names configurable).
/// Swap sprites/materials on child renderers in your own animation clips.
/// </summary>
[DisallowMultipleComponent]
public class AurenGenBattleDiceMachine : MonoBehaviour
{
    [Header("Values (TMP fallback)")]
    public TextMeshProUGUI playerDiceValueLabel;
    public TextMeshProUGUI enemyDiceValueLabel;

    [Header("Animator (optional)")]
    public Animator machineAnimator;
    public string rollTrigger = "Roll";
    public string playerSkinTrigger = "PlayerRollSkin";
    public string enemySkinTrigger = "EnemyRollSkin";

    [Header("Timing")]
    [Min(0f)]
    public float revealHoldSeconds = 0.6f;

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void ApplySkinHints(bool playerRollCosmetic, bool enemyRollCosmetic)
    {
        if (machineAnimator == null)
            return;
        if (playerRollCosmetic && !string.IsNullOrEmpty(playerSkinTrigger))
            machineAnimator.SetTrigger(playerSkinTrigger);
        if (enemyRollCosmetic && !string.IsNullOrEmpty(enemySkinTrigger))
            machineAnimator.SetTrigger(enemySkinTrigger);
    }

    public IEnumerator CoReveal(int playerFace1to20, int enemyFace1to20, bool playerCosmetic, bool enemyCosmetic)
    {
        if (playerDiceValueLabel != null)
            playerDiceValueLabel.text = Mathf.Clamp(playerFace1to20, 1, 20).ToString();
        if (enemyDiceValueLabel != null)
            enemyDiceValueLabel.text = Mathf.Clamp(enemyFace1to20, 1, 20).ToString();

        if (machineAnimator != null && !string.IsNullOrEmpty(rollTrigger))
            machineAnimator.SetTrigger(rollTrigger);
        ApplySkinHints(playerCosmetic, enemyCosmetic);

        if (revealHoldSeconds > 0f)
            yield return new WaitForSecondsRealtime(revealHoldSeconds);
    }
}
