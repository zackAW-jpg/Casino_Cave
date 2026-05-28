using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct RideBusCard
{
    public int Rank;
    public int Suit;
    public bool IsRed => Suit == 1 || Suit == 2;
}

[DisallowMultipleComponent]
public class AurenGenRideTheBusUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject panelRoot;

    [Header("Bet")]
    public Slider betSlider;
    public Button confirmBetButton;
    public TextMeshProUGUI betHint;

    [Header("Step UI")]
    public TextMeshProUGUI stepTitle;
    public Button redButton;
    public Button blackButton;
    public Button higherButton;
    public Button lowerButton;
    public Button insideButton;
    public Button outsideButton;
    public Button suit0;
    public Button suit1;
    public Button suit2;
    public Button suit3;

    int _pending = -1;
    RideBusCard _c0;
    RideBusCard _c1;
    RideBusCard _c2;
    RideBusCard _c3;

    void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        Wire(redButton, 0);
        Wire(blackButton, 1);
        Wire(higherButton, 2);
        Wire(lowerButton, 3);
        Wire(insideButton, 4);
        Wire(outsideButton, 5);
        Wire(suit0, 10);
        Wire(suit1, 11);
        Wire(suit2, 12);
        Wire(suit3, 13);
        if (confirmBetButton != null)
            confirmBetButton.onClick.AddListener(() => _pending = 99);
    }

    void Wire(Button b, int code)
    {
        if (b == null)
            return;
        int c = code;
        b.onClick.AddListener(() => _pending = c);
    }

    public IEnumerator RunRide(AurenGenRuntimeUnit rider, AurenGenBattleNarrativePresenter narrative)
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
        LastRunFullSuccess = false;
        LastBetAmount = 0;
        int minBet = 10;
        int maxBet = Mathf.Max(minBet, rider.CurrentDebtPoints - 1);
        int bet = minBet;
        if (rider.RideBusNeedsBuyIn)
        {
            if (betSlider != null)
            {
                betSlider.minValue = minBet;
                betSlider.maxValue = maxBet;
                betSlider.wholeNumbers = true;
                betSlider.value = minBet;
            }
            _pending = -1;
            if (betHint != null)
                betHint.text = "Bet debt (" + minBet + "–" + maxBet + ").";
            yield return WaitUntilBetConfirmed();
            bet = betSlider != null ? (int)betSlider.value : minBet;
            bet = Mathf.Clamp(bet, minBet, maxBet);
            if (!rider.SpendDebt(bet))
            {
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                yield break;
            }
            rider.RideBusSetBet(bet);
            if (narrative != null)
                yield return narrative.ShowAndAdvanceLine("Bet " + bet + " debt.");
        }
        else
            bet = Mathf.Max(10, rider.RideBusLastBet);

        DrawFourCards();
        if (stepTitle != null)
            stepTitle.text = "Red or Black?";
        SetStep1Buttons(true);
        _pending = -1;
        yield return WaitChoice();
        bool ok1 = (_pending == 0 && _c0.IsRed) || (_pending == 1 && !_c0.IsRed);
        SetStep1Buttons(false);
        if (!ok1)
        {
            rider.RideBusOnCardFail();
            LastRunFullSuccess = false;
            if (narrative != null)
                yield return narrative.ShowAndAdvanceLine("Ride the Bus failed.");
            if (panelRoot != null)
                panelRoot.SetActive(false);
            yield break;
        }
        if (narrative != null)
            yield return narrative.ShowAndAdvanceLine("Correct.");

        if (stepTitle != null)
            stepTitle.text = "Higher or lower than " + _c0.Rank + "?";
        SetStep2Buttons(true);
        _pending = -1;
        yield return WaitChoice();
        bool high = _pending == 2;
        bool ok2 = (high && _c1.Rank > _c0.Rank) || (!high && _c1.Rank < _c0.Rank);
        if (_c1.Rank == _c0.Rank)
            ok2 = false;
        SetStep2Buttons(false);
        if (!ok2)
        {
            rider.RideBusOnCardFail();
            LastRunFullSuccess = false;
            if (narrative != null)
                yield return narrative.ShowAndAdvanceLine("Ride the Bus failed.");
            if (panelRoot != null)
                panelRoot.SetActive(false);
            yield break;
        }
        if (narrative != null)
            yield return narrative.ShowAndAdvanceLine("Correct.");

        int lo = Mathf.Min(_c0.Rank, _c1.Rank);
        int hi = Mathf.Max(_c0.Rank, _c1.Rank);
        if (stepTitle != null)
            stepTitle.text = "Inside " + lo + "-" + hi + " or outside?";
        SetStep3Buttons(true);
        _pending = -1;
        yield return WaitChoice();
        bool inside = _pending == 4;
        bool inRange = _c2.Rank >= lo && _c2.Rank <= hi;
        bool ok3 = inside == inRange;
        SetStep3Buttons(false);
        if (!ok3)
        {
            rider.RideBusOnCardFail();
            LastRunFullSuccess = false;
            if (narrative != null)
                yield return narrative.ShowAndAdvanceLine("Ride the Bus failed.");
            if (panelRoot != null)
                panelRoot.SetActive(false);
            yield break;
        }
        if (narrative != null)
            yield return narrative.ShowAndAdvanceLine("Correct.");

        if (stepTitle != null)
            stepTitle.text = "Pick suit (0♠ 1♥ 2♦ 3♣).";
        SetSuitButtons(true);
        _pending = -1;
        yield return WaitChoice();
        int suitGuess = _pending - 10;
        bool ok4 = suitGuess == _c3.Suit;
        SetSuitButtons(false);
        if (!ok4)
        {
            rider.RideBusOnCardFail();
            if (narrative != null)
                yield return narrative.ShowAndAdvanceLine("Ride the Bus failed.");
            LastRunFullSuccess = false;
            if (panelRoot != null)
                panelRoot.SetActive(false);
            yield break;
        }
        LastBetAmount = bet;
        LastRunFullSuccess = true;
        if (narrative != null)
            yield return narrative.ShowAndAdvanceLine("Ride the Bus cleared!");
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public bool LastRunFullSuccess { get; private set; }
    public int LastBetAmount { get; private set; }

    IEnumerator WaitUntilBetConfirmed()
    {
        _pending = -1;
        while (_pending != 99)
            yield return null;
    }

    IEnumerator WaitChoice()
    {
        while (_pending < 0)
            yield return null;
    }

    void DrawFourCards()
    {
        for (int i = 0; i < 4; i++)
        {
            RideBusCard c = new RideBusCard { Rank = Random.Range(2, 15), Suit = Random.Range(0, 4) };
            if (i == 0)
                _c0 = c;
            else if (i == 1)
                _c1 = c;
            else if (i == 2)
                _c2 = c;
            else
                _c3 = c;
        }
    }

    void SetStep1Buttons(bool on)
    {
        if (redButton != null)
            redButton.gameObject.SetActive(on);
        if (blackButton != null)
            blackButton.gameObject.SetActive(on);
    }

    void SetStep2Buttons(bool on)
    {
        if (higherButton != null)
            higherButton.gameObject.SetActive(on);
        if (lowerButton != null)
            lowerButton.gameObject.SetActive(on);
    }

    void SetStep3Buttons(bool on)
    {
        if (insideButton != null)
            insideButton.gameObject.SetActive(on);
        if (outsideButton != null)
            outsideButton.gameObject.SetActive(on);
    }

    void SetSuitButtons(bool on)
    {
        if (suit0 != null)
            suit0.gameObject.SetActive(on);
        if (suit1 != null)
            suit1.gameObject.SetActive(on);
        if (suit2 != null)
            suit2.gameObject.SetActive(on);
        if (suit3 != null)
            suit3.gameObject.SetActive(on);
    }
}
