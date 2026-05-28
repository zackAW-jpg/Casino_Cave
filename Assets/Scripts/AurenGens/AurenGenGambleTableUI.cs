using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen gamble host: dim overlay + table root. Add minigame modules (e.g. slot rush) for combat / merchants.
/// </summary>
[DisallowMultipleComponent]
public class AurenGenGambleTableUI : MonoBehaviour
{
    [Header("Chrome")]
    public CanvasGroup dimmer;
    public GameObject tableRoot;
    public Button closeWithoutPlayButton;
    public TextMeshProUGUI statusLine;

    [Header("Slot rush (enemy placeholder)")]
    public AurenGenSlotRushMinigame slotRush;

    public bool LastHostWon { get; private set; }

    void Awake()
    {
        SetOpen(false);
        if (closeWithoutPlayButton != null)
            closeWithoutPlayButton.onClick.AddListener(() => { });
    }

    public void SetOpen(bool open)
    {
        if (dimmer != null)
        {
            dimmer.alpha = open ? 0.75f : 0f;
            dimmer.blocksRaycasts = open;
            dimmer.interactable = open;
        }
        if (tableRoot != null)
            tableRoot.SetActive(open);
    }

    public IEnumerator CoRunEnemyGamble(AurenGenRuntimeUnit bettor, int targetScore, float payoutMultiplier, AurenGenBattleNarrativePresenter narrative)
    {
        LastHostWon = false;
        SetOpen(true);
        if (slotRush != null)
        {
            yield return slotRush.CoRun(bettor, targetScore, payoutMultiplier, narrative, statusLine);
            LastHostWon = slotRush.LastRunWon;
        }
        SetOpen(false);
    }
}
