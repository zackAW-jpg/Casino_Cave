using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Placeholder "slot machine" rush: 3 turns, cumulative score; win if total >= targetScore. Payout: heal bet * multiplier on win.
/// Replace visuals/audio later; logic is structured for swapping UI.
/// </summary>
[DisallowMultipleComponent]
public class AurenGenSlotRushMinigame : MonoBehaviour
{
    [Header("Bet")]
    public Slider betSlider;
    public Button confirmBetButton;
    public TextMeshProUGUI betHint;

    [Header("Play")]
    public Button spinButton;
    public TextMeshProUGUI reelsLine;
    public TextMeshProUGUI scoreLine;
    public int spinsPerGame = 3;
    public int minBetDebt = 1;

    int _pending = -1;

    public bool LastRunWon { get; private set; }

    void Awake()
    {
        if (confirmBetButton != null)
            confirmBetButton.onClick.AddListener(() => _pending = 99);
        if (spinButton != null)
            spinButton.onClick.AddListener(() => _pending = 1);
    }

    public IEnumerator CoRun(AurenGenRuntimeUnit bettor, int targetScore, float payoutMultiplier, AurenGenBattleNarrativePresenter narrative, TextMeshProUGUI status)
    {
        LastRunWon = false;
        if (bettor == null || bettor.IsBroken)
            yield break;

        int maxBet = Mathf.Max(minBetDebt, bettor.CurrentDebtPoints - 1);
        int minBet = Mathf.Min(minBetDebt, maxBet);
        if (maxBet < minBetDebt)
        {
            if (status != null)
                status.text = "Not enough debt to bet.";
            yield break;
        }

        if (betSlider != null)
        {
            betSlider.wholeNumbers = true;
            betSlider.minValue = minBet;
            betSlider.maxValue = maxBet;
            betSlider.value = minBet;
        }
        if (betHint != null)
            betHint.text = "Bet debt (" + minBet + "–" + maxBet + "). Win if total score ≥ " + targetScore + ".";

        _pending = -1;
        while (_pending != 99)
            yield return null;
        int bet = betSlider != null ? (int)betSlider.value : minBet;
        bet = Mathf.Clamp(bet, minBet, maxBet);
        if (!bettor.SpendDebt(bet))
            yield break;

        if (narrative != null)
            yield return narrative.ShowAndAdvanceLine("Bet " + bet + " debt on the slots.");

        int total = 0;
        for (int turn = 0; turn < spinsPerGame; turn++)
        {
            if (scoreLine != null)
                scoreLine.text = "Score: " + total + " / " + targetScore + "  (spin " + (turn + 1) + "/" + spinsPerGame + ")";
            _pending = -1;
            while (_pending != 1)
                yield return null;
            int spin = Random.Range(5, 36);
            total += spin;
            if (reelsLine != null)
                reelsLine.text = "Spin: +" + spin;
            if (narrative != null)
                yield return narrative.ShowAndAdvanceLine("Spin " + (turn + 1) + ": +" + spin + " (total " + total + ").");
        }

        bool won = total >= targetScore;
        LastRunWon = won;
        if (won)
        {
            int heal = Mathf.RoundToInt(bet * payoutMultiplier);
            bettor.HealDebt(heal);
            if (narrative != null)
                yield return narrative.ShowAndAdvanceLine("Win! Restored " + heal + " debt (" + payoutMultiplier + "× bet).");
        }
        else if (narrative != null)
        {
            yield return narrative.ShowAndAdvanceLine("Loss. Total " + total + " (needed " + targetScore + ").");
        }
    }
}
