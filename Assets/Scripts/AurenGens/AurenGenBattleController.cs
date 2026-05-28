using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AurenGenBattleController : MonoBehaviour
{
    enum BattleState
    {
        PlayerChoice = 0,
        AwaitingForcedSwitch = 1,
        ResolvingRound = 2,
        Ended = 3
    }

    [System.Serializable]
    public class MoveButtonSlot
    {
        [Tooltip("Move button.")]
        public Button button;

        [Tooltip("Move button label.")]
        public TextMeshProUGUI label;
    }

    [System.Serializable]
    public class ItemButtonSlot
    {
        [Tooltip("Item button.")]
        public Button button;

        [Tooltip("Item button label.")]
        public TextMeshProUGUI label;
    }

    [System.Serializable]
    public class SwitchButtonSlot
    {
        [Tooltip("Bench switch button.")]
        public Button button;

        [Tooltip("Bench button label.")]
        public TextMeshProUGUI label;
    }

    [Header("Player roster")]
    [Tooltip("Player AurenGens in this test.")]
    public List<AurenGenDefinition> playerRoster = new List<AurenGenDefinition>();

    [Header("Enemy roster")]
    [Tooltip("Enemy AurenGens in this test.")]
    public List<AurenGenDefinition> enemyRoster = new List<AurenGenDefinition>();

    [Header("Battle items")]
    [Tooltip("Player item pool in this test.")]
    public List<AurenGenBattleItemData> playerItemPool = new List<AurenGenBattleItemData>();

    [Tooltip("Enemy item pool in this test.")]
    public List<AurenGenBattleItemData> enemyItemPool = new List<AurenGenBattleItemData>();

    [Header("Battleground views")]
    [Tooltip("Player side battler view.")]
    public AurenGenBattlerView playerView;

    [Tooltip("Enemy side battler view.")]
    public AurenGenBattlerView enemyView;

    [Header("Health UI")]
    [Tooltip("Player debt bar.")]
    public Slider playerDebtBar;

    [Tooltip("Enemy debt bar.")]
    public Slider enemyDebtBar;

    [Tooltip("Player name label.")]
    public TextMeshProUGUI playerNameLabel;

    [Tooltip("Enemy name label.")]
    public TextMeshProUGUI enemyNameLabel;

    [Tooltip("Player debt text.")]
    public TextMeshProUGUI playerDebtLabel;

    [Tooltip("Enemy debt text.")]
    public TextMeshProUGUI enemyDebtLabel;

    [Header("Narrative + move grid")]
    [Tooltip("Optional narrative + planning swap.")]
    public AurenGenBattleNarrativePresenter narrativePresenter;

    [Tooltip("Optional 6-slot move grid.")]
    public AurenGenBattleMoveGrid moveGrid;

    [Tooltip("Ride the Bus UI.")]
    public AurenGenRideTheBusUI rideTheBusUi;

    [Tooltip("Gamble enemy button.")]
    public Button gambleEnemyButton;

    [Tooltip("Center-field d20 machine (optional).")]
    public AurenGenBattleDiceMachine diceMachine;

    [Tooltip("Full-screen gamble table (optional).")]
    public AurenGenGambleTableUI gambleTableUi;

    [Header("Bottom options")]
    [Tooltip("Move button slots.")]
    public MoveButtonSlot[] moveButtons;

    [Tooltip("Inventory button.")]
    public Button inventoryButton;

    [Tooltip("Switch button.")]
    public Button switchButton;

    [Tooltip("Fight button.")]
    public Button fightButton;

    [Tooltip("Fight button label.")]
    public TextMeshProUGUI fightButtonLabel;

    [Header("Panels")]
    [Tooltip("Inventory panel root.")]
    public GameObject inventoryPanelRoot;

    [Tooltip("Inventory close button.")]
    public Button inventoryCloseButton;

    [Tooltip("Inventory item slots.")]
    public ItemButtonSlot[] inventoryItemButtons;

    [Tooltip("Switch panel root.")]
    public GameObject switchPanelRoot;

    [Tooltip("Switch panel close button.")]
    public Button switchCloseButton;

    [Tooltip("Switch bench slots.")]
    public SwitchButtonSlot[] switchBenchButtons;

    [Header("Battle feed")]
    [Tooltip("Battle log label.")]
    public TextMeshProUGUI battleLogLabel;

    [Tooltip("Round roll label.")]
    public TextMeshProUGUI rollSummaryLabel;

    [Tooltip("Parry guess display.")]
    public TextMeshProUGUI parryGuessLabel;

    [Tooltip("Parry guess input.")]
    public TMP_InputField parryGuessInput;

    [Header("Dummy tuning")]
    [Tooltip("Dummy fallback damage.")]
    [Min(1)]
    public int dummyFallbackDamage = 10;

    readonly List<AurenGenRuntimeUnit> _playerUnits = new List<AurenGenRuntimeUnit>();
    readonly List<AurenGenRuntimeUnit> _enemyUnits = new List<AurenGenRuntimeUnit>();
    AurenGenMoveData _fallbackMove;

    int _playerActiveIndex;
    int _enemyActiveIndex;
    BattleState _state;
    int _selectedMoveIndex = -1;
    int _selectedItemIndex = -1;
    int _enemySelectedMoveIndex = -1;
    int _enemySelectedItemIndex = -1;
    bool _playerMoveCancelledForRound;
    bool _enemyMoveCancelledForRound;
    AurenGenMoveData _selectedMoveData;
    readonly List<string> _preRollPresentationLines = new List<string>();
    readonly List<string> _diceRevealPresentationLines = new List<string>();

    void Awake()
    {
        BuildRuntimeTeams();
        BindStaticButtons();
        _state = BattleState.PlayerChoice;
        ClosePanels();
        HideDiceMachine();
        RefreshAllUI();
        WriteLog("Match started.");
    }

    void BuildRuntimeTeams()
    {
        _playerUnits.Clear();
        _enemyUnits.Clear();

        int playerCount = Mathf.Min(4, playerRoster.Count);
        for (int i = 0; i < playerCount; i++)
        {
            if (playerRoster[i] == null)
                continue;
            AurenGenRuntimeUnit unit = new AurenGenRuntimeUnit();
            unit.BeginMatch(playerRoster[i]);
            _playerUnits.Add(unit);
        }

        int enemyCount = Mathf.Min(4, enemyRoster.Count);
        for (int i = 0; i < enemyCount; i++)
        {
            if (enemyRoster[i] == null)
                continue;
            AurenGenRuntimeUnit unit = new AurenGenRuntimeUnit();
            unit.BeginMatch(enemyRoster[i]);
            _enemyUnits.Add(unit);
        }

        if (_playerUnits.Count == 0 || _enemyUnits.Count == 0)
        {
            WriteLog("Assign at least one player and one enemy AurenGen.");
            _state = BattleState.Ended;
            return;
        }

        _playerActiveIndex = FirstAliveIndex(_playerUnits);
        _enemyActiveIndex = FirstAliveIndex(_enemyUnits);
    }

    void BindStaticButtons()
    {
        if (inventoryButton != null)
            inventoryButton.onClick.AddListener(OnInventoryClicked);
        if (switchButton != null)
            switchButton.onClick.AddListener(OnSwitchClicked);
        if (inventoryCloseButton != null)
            inventoryCloseButton.onClick.AddListener(CloseInventoryPanel);
        if (switchCloseButton != null)
            switchCloseButton.onClick.AddListener(CloseSwitchPanel);
        if (fightButton != null)
            fightButton.onClick.AddListener(OnFightPressed);

        if (moveGrid == null)
        {
            for (int i = 0; i < moveButtons.Length; i++)
            {
                int captured = i;
                if (moveButtons[i] != null && moveButtons[i].button != null)
                    moveButtons[i].button.onClick.AddListener(() => OnMoveButtonPressed(captured));
            }
        }

        if (gambleEnemyButton != null)
            gambleEnemyButton.onClick.AddListener(OnGambleEnemyClicked);

        for (int i = 0; i < inventoryItemButtons.Length; i++)
        {
            int captured = i;
            if (inventoryItemButtons[i] != null && inventoryItemButtons[i].button != null)
                inventoryItemButtons[i].button.onClick.AddListener(() => OnInventoryItemPressed(captured));
        }

        for (int i = 0; i < switchBenchButtons.Length; i++)
        {
            int captured = i;
            if (switchBenchButtons[i] != null && switchBenchButtons[i].button != null)
                switchBenchButtons[i].button.onClick.AddListener(() => OnSwitchBenchPressed(captured));
        }
    }

    void OnInventoryClicked()
    {
        if (_state != BattleState.PlayerChoice)
            return;
        if (inventoryPanelRoot != null)
            inventoryPanelRoot.SetActive(true);
        if (switchPanelRoot != null)
            switchPanelRoot.SetActive(false);
    }

    void OnSwitchClicked()
    {
        if (_state != BattleState.PlayerChoice && _state != BattleState.AwaitingForcedSwitch)
            return;
        if (!CanCurrentPlayerUnitSwitch())
            return;
        if (switchPanelRoot != null)
            switchPanelRoot.SetActive(true);
        if (inventoryPanelRoot != null)
            inventoryPanelRoot.SetActive(false);
        RefreshSwitchBenchButtons();
    }

    void CloseInventoryPanel()
    {
        if (inventoryPanelRoot != null)
            inventoryPanelRoot.SetActive(false);
    }

    void CloseSwitchPanel()
    {
        if (_state == BattleState.AwaitingForcedSwitch)
        {
            WriteLog("You must switch in an active AurenGen.");
            return;
        }
        if (switchPanelRoot != null)
            switchPanelRoot.SetActive(false);
    }

    void ClosePanels()
    {
        CloseInventoryPanel();
        CloseSwitchPanel();
    }

    void OnGambleEnemyClicked()
    {
        if (_state != BattleState.PlayerChoice)
            return;
        StartCoroutine(CoGambleEnemyRound());
    }

    IEnumerator CoGambleEnemyRound()
    {
        AurenGenRuntimeUnit player = GetPlayerActiveUnit();
        if (player == null || player.IsBroken)
            yield break;

        if (gambleTableUi != null && gambleTableUi.slotRush != null)
        {
            if (narrativePresenter != null)
                narrativePresenter.SetNarrativeMode(true);
            yield return gambleTableUi.CoRunEnemyGamble(player, 100, 4f, narrativePresenter);
            if (narrativePresenter != null)
                narrativePresenter.SetNarrativeMode(false);
            RefreshHealthUI();
            if (gambleTableUi.LastHostWon && playerView != null)
                playerView.PlayHeal();
            yield break;
        }

        if (narrativePresenter != null)
        {
            narrativePresenter.SetNarrativeMode(true);
            yield return narrativePresenter.ShowAndAdvanceLine("Assign GambleTableUI + Slot Rush: 3 spins, total score ≥ 100 wins, heal 4× bet debt.");
            narrativePresenter.SetNarrativeMode(false);
        }
        else
            WriteLog("Assign GambleTableUI for Gamble the enemy.");
    }

    void OnMoveButtonPressed(int moveIndex)
    {
        if (_state != BattleState.PlayerChoice)
            return;
        if (_playerActiveIndex < 0 || _playerActiveIndex >= _playerUnits.Count)
            return;

        AurenGenRuntimeUnit attacker = _playerUnits[_playerActiveIndex];
        if (attacker.IsBroken || attacker.Definition == null)
            return;

        AurenGenMoveData move = GetMove(attacker.Definition, moveIndex);
        if (move == null)
            return;

        if (move.isDebtCollectorMove && !attacker.CanUseDebtCollector())
        {
            WriteLog("Debt Collector is not ready.");
            return;
        }

        _selectedMoveIndex = moveIndex;
        _selectedMoveData = move;
        WriteLog("Move selected: " + move.moveName + ". Press Fight!");
        RefreshAllUI();
    }

    public void OnMovePickedFromGrid(AurenGenMoveData move)
    {
        if (_state != BattleState.PlayerChoice)
            return;
        AurenGenRuntimeUnit attacker = GetPlayerActiveUnit();
        if (attacker == null || attacker.IsBroken || attacker.Definition == null || move == null)
            return;
        if (move.isDebtCollectorMove && !attacker.CanUseDebtCollector())
            return;
        _selectedMoveData = move;
        _selectedMoveIndex = -1;
        WriteLog("Move selected: " + move.moveName + ". Lock in.");
        RefreshAllUI();
    }

    void OnInventoryItemPressed(int itemIndex)
    {
        if (_state != BattleState.PlayerChoice)
            return;
        if (itemIndex < 0 || itemIndex >= playerItemPool.Count || playerItemPool[itemIndex] == null)
            return;
        if (_selectedItemIndex == itemIndex)
            _selectedItemIndex = -1;
        else
            _selectedItemIndex = itemIndex;
        RefreshInventoryButtons();
    }

    void OnFightPressed()
    {
        if (_state != BattleState.PlayerChoice)
            return;
        AurenGenRuntimeUnit player = GetPlayerActiveUnit();
        if (player == null || player.IsBroken || player.Definition == null)
            return;
        AurenGenMoveData move = _selectedMoveData;
        if (move == null && _selectedMoveIndex >= 0)
            move = GetMove(player.Definition, _selectedMoveIndex);
        if (move == null)
        {
            if (narrativePresenter != null)
                StartCoroutine(CoMustPickMove());
            else
                WriteLog("You must select a move.");
            return;
        }
        if (move.isDebtCollectorMove && !player.CanUseDebtCollector())
        {
            WriteLog("Debt Collector is not ready.");
            return;
        }
        if (!EnsureAliveEnemyActive())
        {
            EvaluateBattleEnd();
            return;
        }

        PickEnemyRoundChoices();
        ClosePanels();
        if (narrativePresenter != null)
            StartCoroutine(CoResolveRound(player, _enemyUnits[_enemyActiveIndex], move));
        else
            ResolveRound(player, _enemyUnits[_enemyActiveIndex], move);
    }

    IEnumerator CoMustPickMove()
    {
        if (narrativePresenter == null)
            yield break;
        narrativePresenter.SetNarrativeMode(true);
        yield return narrativePresenter.ShowAndAdvanceLine("You must select a move.");
        narrativePresenter.SetNarrativeMode(false);
    }

    IEnumerator CoResolveRound(AurenGenRuntimeUnit player, AurenGenRuntimeUnit enemy, AurenGenMoveData playerMove)
    {
        if (narrativePresenter != null)
            narrativePresenter.SetNarrativeMode(true);
        _state = BattleState.ResolvingRound;
        AurenGenMoveData enemyMove = GetEnemySelectedMove(enemy);
        AurenGenBattleItemData playerItem = GetSelectedPlayerItem();
        AurenGenBattleItemData enemyItem = GetSelectedEnemyItem();
        _playerMoveCancelledForRound = false;
        _enemyMoveCancelledForRound = false;
        player.ClearParryGuess();
        enemy.ClearParryGuess();
        if (parryGuessLabel != null)
            parryGuessLabel.text = "";

        ClearPresentationQueues();

        if (playerItem != null)
            yield return NarrLine("Player used item: " + playerItem.itemName + ".", true);
        if (enemyItem != null)
            yield return NarrLine("Enemy used item: " + enemyItem.itemName + ".", true);

        int playerRoundBonus = 0;
        int enemyRoundBonus = 0;
        ApplyItem(player, enemy, playerItem, ref playerRoundBonus, ref enemyRoundBonus);
        if (playerItem != null && playerItem.healDebtPoints > 0 && playerView != null)
            playerView.PlayHeal();
        ApplyItem(enemy, player, enemyItem, ref enemyRoundBonus, ref playerRoundBonus);
        if (enemyItem != null && enemyItem.healDebtPoints > 0 && enemyView != null)
            enemyView.PlayHeal();
        ExecuteMoveEffects(playerMove, AurenGenMoveEffectTiming.AfterItems, player, enemy, true, 0, 0, 0, 0);
        ExecuteMoveEffects(enemyMove, AurenGenMoveEffectTiming.AfterItems, enemy, player, false, 0, 0, 0, 0);
        ExecuteMoveEffects(playerMove, AurenGenMoveEffectTiming.AfterItemsPreRoll, player, enemy, true, 0, 0, 0, 0);
        ExecuteMoveEffects(enemyMove, AurenGenMoveEffectTiming.AfterItemsPreRoll, enemy, player, false, 0, 0, 0, 0);
        yield return CoDrainPresentationLines(_preRollPresentationLines, true);

        int playerRollRaw = player.TryConsumeLockedNextRawRoll(out int lockedPlayer) ? lockedPlayer : Random.Range(1, 21);
        int enemyRollRaw = enemy.TryConsumeLockedNextRawRoll(out int lockedEnemy) ? lockedEnemy : Random.Range(1, 21);

        ExecuteMoveEffects(playerMove, AurenGenMoveEffectTiming.BeforeRoll, player, enemy, true, playerRollRaw, enemyRollRaw, 0, 0);
        ExecuteMoveEffects(enemyMove, AurenGenMoveEffectTiming.BeforeRoll, enemy, player, false, enemyRollRaw, playerRollRaw, 0, 0);

        int playerRoll = playerRollRaw + player.ConsumeNextRollModifier() + playerRoundBonus;
        int enemyRoll = enemyRollRaw + enemy.ConsumeNextRollModifier() + enemyRoundBonus;

        ExecuteMoveEffects(playerMove, AurenGenMoveEffectTiming.AfterRoll, player, enemy, true, playerRollRaw, enemyRollRaw, playerRoll, enemyRoll);
        ExecuteMoveEffects(enemyMove, AurenGenMoveEffectTiming.AfterRoll, enemy, player, false, enemyRollRaw, playerRollRaw, enemyRoll, playerRoll);

        yield return CoDrainPresentationLines(_diceRevealPresentationLines, true);

        bool playerCosmetic = playerRoll != playerRollRaw || playerRoundBonus != 0;
        bool enemyCosmetic = enemyRoll != enemyRollRaw || enemyRoundBonus != 0;
        if (diceMachine != null)
        {
            diceMachine.SetVisible(true);
            yield return StartCoroutine(diceMachine.CoReveal(playerRoll, enemyRoll, playerCosmetic, enemyCosmetic));
        }

        if (rollSummaryLabel != null)
            rollSummaryLabel.text = "Player Roll: " + playerRoll + " | Enemy Roll: " + enemyRoll;

        yield return NarrLine("Player roll " + playerRoll + ", Enemy roll " + enemyRoll + ".", true);

        bool playerFirst = playerRoll >= enemyRoll;

        if (playerFirst)
        {
            if (!_playerMoveCancelledForRound)
                yield return StartCoroutine(CoExecuteRoundAttack(player, enemy, playerMove, playerRoll, true, true));
            else
                yield return NarrLine("Player move was cancelled.", true);
            if (!EvaluateBattleEnd() && !enemy.IsBroken)
            {
                if (!_enemyMoveCancelledForRound)
                    yield return StartCoroutine(CoExecuteRoundAttack(enemy, player, enemyMove, enemyRoll, false, true));
                else
                    yield return NarrLine("Enemy move was cancelled.", true);
            }
        }
        else
        {
            if (!_enemyMoveCancelledForRound)
                yield return StartCoroutine(CoExecuteRoundAttack(enemy, player, enemyMove, enemyRoll, false, true));
            else
                yield return NarrLine("Enemy move was cancelled.", true);
            if (!EvaluateBattleEnd() && !player.IsBroken)
            {
                if (!_playerMoveCancelledForRound)
                    yield return StartCoroutine(CoExecuteRoundAttack(player, enemy, playerMove, playerRoll, true, true));
                else
                    yield return NarrLine("Player move was cancelled.", true);
            }
        }

        player.ClearSwerveDodge();
        enemy.ClearSwerveDodge();

        if (EvaluateBattleEnd())
        {
            if (narrativePresenter != null)
                narrativePresenter.SetNarrativeMode(false);
            HideDiceMachine();
            yield break;
        }

        _selectedMoveIndex = -1;
        _selectedMoveData = null;
        _selectedItemIndex = -1;
        _enemySelectedMoveIndex = -1;
        _enemySelectedItemIndex = -1;

        if (GetPlayerActiveUnit() != null && GetPlayerActiveUnit().IsBroken && HasAliveBenchForPlayer())
        {
            _state = BattleState.AwaitingForcedSwitch;
            if (switchPanelRoot != null)
                switchPanelRoot.SetActive(true);
            yield return NarrLine("Active AurenGen broke. Switch from bench.", true);
            if (narrativePresenter != null)
                narrativePresenter.SetNarrativeMode(false);
            HideDiceMachine();
            RefreshAllUI();
            yield break;
        }

        _state = BattleState.PlayerChoice;
        if (narrativePresenter != null)
            narrativePresenter.SetNarrativeMode(false);
        HideDiceMachine();
        RefreshAllUI();
    }

    void ResolveRound(AurenGenRuntimeUnit player, AurenGenRuntimeUnit enemy, AurenGenMoveData playerMove)
    {
        _state = BattleState.ResolvingRound;
        AurenGenMoveData enemyMove = GetEnemySelectedMove(enemy);
        AurenGenBattleItemData playerItem = GetSelectedPlayerItem();
        AurenGenBattleItemData enemyItem = GetSelectedEnemyItem();
        _playerMoveCancelledForRound = false;
        _enemyMoveCancelledForRound = false;
        player.ClearParryGuess();
        enemy.ClearParryGuess();
        if (parryGuessLabel != null)
            parryGuessLabel.text = "";

        ClearPresentationQueues();

        WriteItemUse("Player", playerItem);
        WriteItemUse("Enemy", enemyItem);

        int playerRoundBonus = 0;
        int enemyRoundBonus = 0;
        ApplyItem(player, enemy, playerItem, ref playerRoundBonus, ref enemyRoundBonus);
        if (playerItem != null && playerItem.healDebtPoints > 0 && playerView != null)
            playerView.PlayHeal();
        ApplyItem(enemy, player, enemyItem, ref enemyRoundBonus, ref playerRoundBonus);
        if (enemyItem != null && enemyItem.healDebtPoints > 0 && enemyView != null)
            enemyView.PlayHeal();
        ExecuteMoveEffects(playerMove, AurenGenMoveEffectTiming.AfterItems, player, enemy, true, 0, 0, 0, 0);
        ExecuteMoveEffects(enemyMove, AurenGenMoveEffectTiming.AfterItems, enemy, player, false, 0, 0, 0, 0);
        ExecuteMoveEffects(playerMove, AurenGenMoveEffectTiming.AfterItemsPreRoll, player, enemy, true, 0, 0, 0, 0);
        ExecuteMoveEffects(enemyMove, AurenGenMoveEffectTiming.AfterItemsPreRoll, enemy, player, false, 0, 0, 0, 0);
        RunEnumerator(CoDrainPresentationLines(_preRollPresentationLines, false));

        int playerRollRaw = player.TryConsumeLockedNextRawRoll(out int lockedPlayer) ? lockedPlayer : Random.Range(1, 21);
        int enemyRollRaw = enemy.TryConsumeLockedNextRawRoll(out int lockedEnemy) ? lockedEnemy : Random.Range(1, 21);

        ExecuteMoveEffects(playerMove, AurenGenMoveEffectTiming.BeforeRoll, player, enemy, true, playerRollRaw, enemyRollRaw, 0, 0);
        ExecuteMoveEffects(enemyMove, AurenGenMoveEffectTiming.BeforeRoll, enemy, player, false, enemyRollRaw, playerRollRaw, 0, 0);

        int playerRoll = playerRollRaw + player.ConsumeNextRollModifier() + playerRoundBonus;
        int enemyRoll = enemyRollRaw + enemy.ConsumeNextRollModifier() + enemyRoundBonus;

        ExecuteMoveEffects(playerMove, AurenGenMoveEffectTiming.AfterRoll, player, enemy, true, playerRollRaw, enemyRollRaw, playerRoll, enemyRoll);
        ExecuteMoveEffects(enemyMove, AurenGenMoveEffectTiming.AfterRoll, enemy, player, false, enemyRollRaw, playerRollRaw, enemyRoll, playerRoll);

        RunEnumerator(CoDrainPresentationLines(_diceRevealPresentationLines, false));

        bool playerCosmetic = playerRoll != playerRollRaw || playerRoundBonus != 0;
        bool enemyCosmetic = enemyRoll != enemyRollRaw || enemyRoundBonus != 0;
        if (diceMachine != null)
        {
            diceMachine.SetVisible(true);
            RunEnumerator(diceMachine.CoReveal(playerRoll, enemyRoll, playerCosmetic, enemyCosmetic));
        }

        if (rollSummaryLabel != null)
            rollSummaryLabel.text = "Player Roll: " + playerRoll + " | Enemy Roll: " + enemyRoll;

        WriteLog("Player roll " + playerRoll + ", Enemy roll " + enemyRoll + ".");

        bool playerFirst = playerRoll >= enemyRoll;

        if (playerFirst)
        {
            if (!_playerMoveCancelledForRound)
                RunEnumerator(CoExecuteRoundAttack(player, enemy, playerMove, playerRoll, true, false));
            else
                WriteLog("Player move was cancelled.");
            if (!EvaluateBattleEnd() && !enemy.IsBroken)
            {
                if (!_enemyMoveCancelledForRound)
                    RunEnumerator(CoExecuteRoundAttack(enemy, player, enemyMove, enemyRoll, false, false));
                else
                    WriteLog("Enemy move was cancelled.");
            }
        }
        else
        {
            if (!_enemyMoveCancelledForRound)
                RunEnumerator(CoExecuteRoundAttack(enemy, player, enemyMove, enemyRoll, false, false));
            else
                WriteLog("Enemy move was cancelled.");
            if (!EvaluateBattleEnd() && !player.IsBroken)
            {
                if (!_playerMoveCancelledForRound)
                    RunEnumerator(CoExecuteRoundAttack(player, enemy, playerMove, playerRoll, true, false));
                else
                    WriteLog("Player move was cancelled.");
            }
        }

        player.ClearSwerveDodge();
        enemy.ClearSwerveDodge();

        if (EvaluateBattleEnd())
        {
            HideDiceMachine();
            return;
        }

        _selectedMoveIndex = -1;
        _selectedMoveData = null;
        _selectedItemIndex = -1;
        _enemySelectedMoveIndex = -1;
        _enemySelectedItemIndex = -1;

        if (GetPlayerActiveUnit() != null && GetPlayerActiveUnit().IsBroken && HasAliveBenchForPlayer())
        {
            _state = BattleState.AwaitingForcedSwitch;
            if (switchPanelRoot != null)
                switchPanelRoot.SetActive(true);
            WriteLog("Active AurenGen broke. Switch from bench.");
            RefreshAllUI();
            return;
        }

        _state = BattleState.PlayerChoice;
        HideDiceMachine();
        RefreshAllUI();
    }

    static void RunEnumerator(IEnumerator routine)
    {
        while (routine.MoveNext())
        {
        }
    }

    IEnumerator NarrLine(string text, bool useNarrative)
    {
        if (useNarrative && narrativePresenter != null)
            yield return narrativePresenter.ShowAndAdvanceLine(text);
        else
            WriteLog(text);
    }

    IEnumerator CoDrainPresentationLines(List<string> lines, bool useNarrative)
    {
        for (int i = 0; i < lines.Count; i++)
            yield return NarrLine(lines[i], useNarrative);
        lines.Clear();
    }

    public void EnqueuePreRollPresentation(string line)
    {
        if (string.IsNullOrEmpty(line))
            return;
        _preRollPresentationLines.Add(line);
    }

    public void EnqueueDiceRevealPresentation(string line)
    {
        if (string.IsNullOrEmpty(line))
            return;
        _diceRevealPresentationLines.Add(line);
    }

    void ClearPresentationQueues()
    {
        _preRollPresentationLines.Clear();
        _diceRevealPresentationLines.Clear();
    }

    void HideDiceMachine()
    {
        if (diceMachine != null)
            diceMachine.SetVisible(false);
    }

    IEnumerator CoExecuteRoundAttack(AurenGenRuntimeUnit attacker, AurenGenRuntimeUnit defender, AurenGenMoveData move, int roll, bool attackerIsPlayer, bool useNarrative)
    {
        if (defender.SwerveDodgeActive)
        {
            string dn = defender.Definition == null ? "Foe" : defender.Definition.displayName;
            yield return NarrLine(dn + " swerved the hit!", useNarrative);
            yield break;
        }

        attacker.MarkTurnStayedIn();
        ExecuteMoveEffects(move, AurenGenMoveEffectTiming.BeforeAttack, attacker, defender, attackerIsPlayer, 0, 0, roll, 0);
        bool guaranteedMiss = roll <= 1;
        bool guaranteedFromBuff = attacker.ConsumeGuaranteedNextAttack() && !HasParryEffect(move);
        bool isCrit = guaranteedFromBuff || roll >= 20;
        bool hit = guaranteedFromBuff || (!guaranteedMiss && roll >= Mathf.Max(2, move.hitThreshold));

        if (!hit)
        {
            string missName = attacker.Definition == null ? "Unknown" : attacker.Definition.displayName;
            yield return NarrLine(missName + " used " + move.moveName + " but missed.", useNarrative);
            yield break;
        }

        if (move.isRideTheBusMove && attackerIsPlayer && rideTheBusUi != null)
        {
            yield return rideTheBusUi.RunRide(attacker, useNarrative ? narrativePresenter : null);
            if (rideTheBusUi.LastRunFullSuccess)
            {
                int mult = attacker.GetRideBusDamageMultiplier();
                int dmg = mult * rideTheBusUi.LastBetAmount;
                int applied = defender.ApplyDamage(dmg);
                attacker.RideBusOnFullSuccess();
                if (attackerIsPlayer && playerView != null)
                    playerView.PlayAttack(move.isSpecialMove);
                if (!attackerIsPlayer && enemyView != null)
                    enemyView.PlayAttack(move.isSpecialMove);
                if (attackerIsPlayer && enemyView != null)
                    enemyView.PlayHit();
                if (!attackerIsPlayer && playerView != null)
                    playerView.PlayHit();
                if (defender.IsBroken)
                {
                    if (attackerIsPlayer && enemyView != null)
                        enemyView.PlayBreak();
                    if (!attackerIsPlayer && playerView != null)
                        playerView.PlayBreak();
                }
                string dn = defender.Definition == null ? "Unknown" : defender.Definition.displayName;
                yield return NarrLine("Ride the Bus dealt " + applied + " debt to " + dn + ".", useNarrative);
            }
            yield break;
        }

        int damage = Mathf.Max(0, Mathf.RoundToInt(move.baseDamage * attacker.DamageMultiplier()));
        if (isCrit)
            damage *= 2;
        int appliedDmg = defender.ApplyDamage(damage);

        if (move.isDebtCollectorMove)
            attacker.MarkDebtCollectorUsed();

        if (move.inflictsCondition != AurenGenConditionType.None && Random.value <= move.conditionChance)
        {
            if (move.conditionTargetSide == AurenGenEffectTargetSide.Self)
                attacker.SetCondition(move.inflictsCondition);
            else
                defender.SetCondition(move.inflictsCondition);
        }

        attacker.AddNextRollModifier(move.selfNextRollModifier);
        defender.AddNextRollModifier(-move.enemyNextRollModifier);

        if (move.isSwerveMove)
            attacker.ActivateSwerveDodge();

        if (attackerIsPlayer && playerView != null)
            playerView.PlayAttack(move.isSpecialMove || move.isDebtCollectorMove);
        if (!attackerIsPlayer && enemyView != null)
            enemyView.PlayAttack(move.isSpecialMove || move.isDebtCollectorMove);

        if (attackerIsPlayer && enemyView != null)
            enemyView.PlayHit();
        if (!attackerIsPlayer && playerView != null)
            playerView.PlayHit();

        if (defender.IsBroken)
        {
            if (attackerIsPlayer && enemyView != null)
                enemyView.PlayBreak();
            if (!attackerIsPlayer && playerView != null)
                playerView.PlayBreak();
        }

        string attackerName = attacker.Definition == null ? "Unknown" : attacker.Definition.displayName;
        string defenderName = defender.Definition == null ? "Unknown" : defender.Definition.displayName;
        string critText = isCrit ? " Critical." : "";
        yield return NarrLine(attackerName + " used " + move.moveName + ". " + defenderName + " lost " + appliedDmg + " debt." + critText, useNarrative);

        if (defender.IsBroken)
            yield return NarrLine(defenderName + " broke.", useNarrative);

        if (attackerIsPlayer && defender.IsBroken && EnsureAliveEnemyActive())
            yield return NarrLine("Enemy sent out " + _enemyUnits[_enemyActiveIndex].Definition.displayName + ".", useNarrative);
        if (!attackerIsPlayer && defender.IsBroken)
            yield return NarrLine("Your AurenGen broke.", useNarrative);

        ExecuteMoveEffects(move, AurenGenMoveEffectTiming.AfterAttack, attacker, defender, attackerIsPlayer, 0, 0, roll, 0);
        RefreshHealthUI();
    }

    void OnSwitchBenchPressed(int benchSlot)
    {
        if (_state != BattleState.PlayerChoice && _state != BattleState.AwaitingForcedSwitch)
            return;
        if (_playerActiveIndex < 0 || _playerActiveIndex >= _playerUnits.Count)
            return;
        if (!CanCurrentPlayerUnitSwitch())
            return;

        int targetIndex = GetBenchAliveIndexBySlot(benchSlot);
        if (targetIndex < 0 || targetIndex == _playerActiveIndex)
            return;

        AurenGenRuntimeUnit current = _playerUnits[_playerActiveIndex];
        if (_state == BattleState.PlayerChoice)
            current.MarkSwitchedOut();
        else
            current.MarkForcedOut();
        _playerActiveIndex = targetIndex;

        CloseSwitchPanel();

        AurenGenRuntimeUnit newActive = _playerUnits[_playerActiveIndex];
        WriteLog("Switched to " + newActive.Definition.displayName + ".");
        RefreshAllUI();

        if (_state == BattleState.AwaitingForcedSwitch)
            _state = BattleState.PlayerChoice;
        _selectedMoveIndex = -1;
        _selectedItemIndex = -1;
        RefreshAllUI();
    }

    void RefreshAllUI()
    {
        RefreshBattlerViews();
        RefreshHealthUI();
        RefreshMoveButtons();
        RefreshSwitchButtonState();
        RefreshFightButton();
        RefreshInventoryButtons();
        RefreshSwitchBenchButtons();
    }

    void RefreshBattlerViews()
    {
        if (_playerActiveIndex >= 0 && _playerActiveIndex < _playerUnits.Count && playerView != null)
            playerView.Bind(_playerUnits[_playerActiveIndex].Definition, true);
        if (_enemyActiveIndex >= 0 && _enemyActiveIndex < _enemyUnits.Count && enemyView != null)
            enemyView.Bind(_enemyUnits[_enemyActiveIndex].Definition, false);
    }

    void RefreshHealthUI()
    {
        if (_playerActiveIndex >= 0 && _playerActiveIndex < _playerUnits.Count)
            ApplyHealthToUI(_playerUnits[_playerActiveIndex], playerDebtBar, playerNameLabel, playerDebtLabel);
        if (_enemyActiveIndex >= 0 && _enemyActiveIndex < _enemyUnits.Count)
            ApplyHealthToUI(_enemyUnits[_enemyActiveIndex], enemyDebtBar, enemyNameLabel, enemyDebtLabel);
    }

    void ApplyHealthToUI(AurenGenRuntimeUnit unit, Slider bar, TextMeshProUGUI nameLabel, TextMeshProUGUI debtLabel)
    {
        if (unit == null || unit.Definition == null)
            return;

        if (nameLabel != null)
            nameLabel.text = unit.Definition.displayName;

        if (bar != null)
        {
            bar.minValue = 0f;
            bar.maxValue = unit.MaxDebtPoints();
            bar.value = unit.CurrentDebtPoints;
        }

        if (debtLabel != null)
            debtLabel.text = unit.CurrentDebtPoints + " / " + unit.MaxDebtPoints() + " DP";
    }

    void RefreshMoveButtons()
    {
        AurenGenRuntimeUnit active = GetPlayerActiveUnit();
        bool canInput = _state == BattleState.PlayerChoice && active != null && !active.IsBroken;

        if (moveGrid != null)
        {
            moveGrid.Rebuild(active, _selectedMoveData, OnMovePickedFromGrid, canInput);
            for (int i = 0; i < moveButtons.Length; i++)
            {
                MoveButtonSlot slot = moveButtons[i];
                if (slot != null && slot.button != null)
                    slot.button.interactable = false;
            }
        }
        else
        {
            for (int i = 0; i < moveButtons.Length; i++)
            {
                MoveButtonSlot slot = moveButtons[i];
                if (slot == null || slot.button == null)
                    continue;

                AurenGenMoveData move = active == null || active.Definition == null ? null : GetMove(active.Definition, i);
                bool hasMove = move != null;
                bool debtCollectorBlocked = hasMove && move.isDebtCollectorMove && !active.CanUseDebtCollector();
                slot.button.interactable = canInput && hasMove && !debtCollectorBlocked;

                if (slot.label != null)
                {
                    if (!hasMove)
                        slot.label.text = "Empty";
                    else if (move.isDebtCollectorMove && debtCollectorBlocked)
                        slot.label.text = move.moveName + " (Locked)";
                    else if (_selectedMoveIndex == i)
                        slot.label.text = "[X] " + move.moveName;
                    else
                        slot.label.text = move.moveName;
                }
            }
        }

        if (inventoryButton != null)
            inventoryButton.interactable = canInput;
    }

    void RefreshSwitchButtonState()
    {
        if (switchButton == null)
            return;
        bool allow = _state == BattleState.PlayerChoice || _state == BattleState.AwaitingForcedSwitch;
        switchButton.interactable = allow && CanCurrentPlayerUnitSwitch();
    }

    void RefreshFightButton()
    {
        bool hasMove = _selectedMoveData != null || _selectedMoveIndex >= 0;
        if (fightButton != null)
            fightButton.interactable = _state == BattleState.PlayerChoice && GetPlayerActiveUnit() != null && hasMove;
        if (fightButtonLabel != null)
        {
            if (!hasMove)
                fightButtonLabel.text = moveGrid != null ? "Lock In (Pick Move)" : "Fight! (Pick Move)";
            else
                fightButtonLabel.text = moveGrid != null ? "Lock In" : "Fight!";
        }
    }

    void RefreshInventoryButtons()
    {
        for (int i = 0; i < inventoryItemButtons.Length; i++)
        {
            ItemButtonSlot slot = inventoryItemButtons[i];
            if (slot == null || slot.button == null)
                continue;
            bool has = i >= 0 && i < playerItemPool.Count && playerItemPool[i] != null;
            slot.button.interactable = _state == BattleState.PlayerChoice && has;
            if (slot.label != null)
            {
                if (!has)
                    slot.label.text = "Empty";
                else if (_selectedItemIndex == i)
                    slot.label.text = "[X] " + playerItemPool[i].itemName;
                else
                    slot.label.text = playerItemPool[i].itemName;
            }
        }
    }

    void RefreshSwitchBenchButtons()
    {
        for (int i = 0; i < switchBenchButtons.Length; i++)
        {
            SwitchButtonSlot slot = switchBenchButtons[i];
            if (slot == null || slot.button == null)
                continue;

            int index = GetBenchAliveIndexBySlot(i);
            bool canSelect = (_state == BattleState.PlayerChoice || _state == BattleState.AwaitingForcedSwitch) && CanCurrentPlayerUnitSwitch() && index >= 0;
            slot.button.interactable = canSelect;

            if (slot.label != null)
            {
                if (index < 0)
                    slot.label.text = "Empty";
                else
                {
                    AurenGenRuntimeUnit unit = _playerUnits[index];
                    slot.label.text = unit.Definition.displayName + "  " + unit.CurrentDebtPoints + "/" + unit.MaxDebtPoints();
                }
            }
        }
    }

    bool CanCurrentPlayerUnitSwitch()
    {
        AurenGenRuntimeUnit active = GetPlayerActiveUnit();
        if (active == null)
            return false;
        if (_state == BattleState.AwaitingForcedSwitch)
            return HasAliveBenchForPlayer();
        if (active.IsBroken)
            return false;
        if (active.SwitchUsedThisMatch)
            return false;
        for (int i = 0; i < _playerUnits.Count; i++)
        {
            if (i == _playerActiveIndex)
                continue;
            if (!_playerUnits[i].IsBroken)
                return true;
        }
        return false;
    }

    bool HasAliveBenchForPlayer()
    {
        for (int i = 0; i < _playerUnits.Count; i++)
        {
            if (i == _playerActiveIndex)
                continue;
            if (!_playerUnits[i].IsBroken)
                return true;
        }
        return false;
    }

    AurenGenRuntimeUnit GetPlayerActiveUnit()
    {
        if (_playerActiveIndex < 0 || _playerActiveIndex >= _playerUnits.Count)
            return null;
        return _playerUnits[_playerActiveIndex];
    }

    int GetBenchAliveIndexBySlot(int slot)
    {
        int count = 0;
        for (int i = 0; i < _playerUnits.Count; i++)
        {
            if (i == _playerActiveIndex)
                continue;
            if (_playerUnits[i].IsBroken)
                continue;

            if (count == slot)
                return i;
            count++;
        }
        return -1;
    }

    AurenGenMoveData GetMove(AurenGenDefinition def, int index)
    {
        if (def == null || def.moves == null)
            return null;
        if (index < 0 || index >= def.moves.Length)
            return null;
        return def.moves[index];
    }

    AurenGenMoveData ChooseEnemyMove(AurenGenRuntimeUnit enemy)
    {
        if (enemy == null || enemy.Definition == null)
            return BuildFallbackMove();

        List<AurenGenMoveData> legal = new List<AurenGenMoveData>();
        for (int i = 0; i < enemy.Definition.moves.Length; i++)
        {
            AurenGenMoveData move = enemy.Definition.moves[i];
            if (move == null)
                continue;
            if (move.isDebtCollectorMove && !enemy.CanUseDebtCollector())
                continue;
            legal.Add(move);
        }

        if (legal.Count == 0)
            return BuildFallbackMove();

        return legal[Random.Range(0, legal.Count)];
    }

    AurenGenMoveData BuildFallbackMove()
    {
        if (_fallbackMove == null)
        {
            _fallbackMove = ScriptableObject.CreateInstance<AurenGenMoveData>();
            _fallbackMove.hideFlags = HideFlags.HideAndDontSave;
            _fallbackMove.moveName = "Dummy Hit";
            _fallbackMove.description = "Fallback";
            _fallbackMove.isSpecialMove = false;
            _fallbackMove.isDebtCollectorMove = false;
            _fallbackMove.inflictsCondition = AurenGenConditionType.None;
            _fallbackMove.conditionChance = 0f;
        }
        _fallbackMove.baseDamage = dummyFallbackDamage;
        return _fallbackMove;
    }

    bool EnsureAlivePlayerActive()
    {
        if (_playerActiveIndex < 0 || _playerActiveIndex >= _playerUnits.Count)
            return false;
        if (!_playerUnits[_playerActiveIndex].IsBroken)
            return true;
        int next = FirstAliveIndex(_playerUnits);
        if (next < 0)
            return false;
        _playerActiveIndex = next;
        RefreshAllUI();
        return true;
    }

    bool EnsureAliveEnemyActive()
    {
        if (_enemyActiveIndex < 0 || _enemyActiveIndex >= _enemyUnits.Count)
            return false;
        if (!_enemyUnits[_enemyActiveIndex].IsBroken)
            return true;
        int next = FirstAliveIndex(_enemyUnits);
        if (next < 0)
            return false;
        _enemyActiveIndex = next;
        RefreshAllUI();
        return true;
    }

    int FirstAliveIndex(List<AurenGenRuntimeUnit> team)
    {
        for (int i = 0; i < team.Count; i++)
        {
            if (!team[i].IsBroken)
                return i;
        }
        return -1;
    }

    bool EvaluateBattleEnd()
    {
        bool playerHasAlive = FirstAliveIndex(_playerUnits) >= 0;
        bool enemyHasAlive = FirstAliveIndex(_enemyUnits) >= 0;

        if (playerHasAlive && enemyHasAlive)
            return false;

        _state = BattleState.Ended;
        RefreshAllUI();

        if (playerHasAlive && !enemyHasAlive)
            WriteLog("You win.");
        else if (!playerHasAlive && enemyHasAlive)
            WriteLog("You lose.");
        else
            WriteLog("Draw.");

        return true;
    }

    void PickEnemyRoundChoices()
    {
        AurenGenRuntimeUnit enemy = _enemyUnits[_enemyActiveIndex];
        _enemySelectedMoveIndex = -1;
        List<int> legal = new List<int>();
        if (enemy != null && enemy.Definition != null && enemy.Definition.moves != null)
        {
            for (int i = 0; i < enemy.Definition.moves.Length; i++)
            {
                AurenGenMoveData move = enemy.Definition.moves[i];
                if (move == null)
                    continue;
                if (move.isDebtCollectorMove && !enemy.CanUseDebtCollector())
                    continue;
                legal.Add(i);
            }
        }
        if (legal.Count > 0)
            _enemySelectedMoveIndex = legal[Random.Range(0, legal.Count)];
        _enemySelectedItemIndex = enemyItemPool.Count > 0 && Random.value < 0.5f ? Random.Range(0, enemyItemPool.Count) : -1;
    }

    AurenGenMoveData GetEnemySelectedMove(AurenGenRuntimeUnit enemy)
    {
        if (enemy == null || enemy.Definition == null)
            return BuildFallbackMove();
        AurenGenMoveData move = GetMove(enemy.Definition, _enemySelectedMoveIndex);
        if (move == null)
            return ChooseEnemyMove(enemy);
        return move;
    }

    AurenGenBattleItemData GetSelectedPlayerItem()
    {
        if (_selectedItemIndex < 0 || _selectedItemIndex >= playerItemPool.Count)
            return null;
        return playerItemPool[_selectedItemIndex];
    }

    AurenGenBattleItemData GetSelectedEnemyItem()
    {
        if (_enemySelectedItemIndex < 0 || _enemySelectedItemIndex >= enemyItemPool.Count)
            return null;
        return enemyItemPool[_enemySelectedItemIndex];
    }

    void WriteItemUse(string side, AurenGenBattleItemData item)
    {
        if (item == null)
            return;
        WriteLog(side + " used item: " + item.itemName + ".");
    }

    void ApplyItem(AurenGenRuntimeUnit user, AurenGenRuntimeUnit enemy, AurenGenBattleItemData item, ref int userRoundBonus, ref int enemyRoundBonus)
    {
        if (item == null || user == null || enemy == null)
            return;
        user.HealDebt(item.healDebtPoints);
        userRoundBonus += item.roundRollBonus;
        enemyRoundBonus -= item.enemyRoundRollPenalty;
        RefreshHealthUI();
    }

    void ExecuteMoveEffects(
        AurenGenMoveData move,
        AurenGenMoveEffectTiming timing,
        AurenGenRuntimeUnit attacker,
        AurenGenRuntimeUnit defender,
        bool attackerIsPlayer,
        int attackerRawRoll,
        int defenderRawRoll,
        int attackerFinalRoll,
        int defenderFinalRoll)
    {
        if (move == null || move.effects == null)
            return;
        for (int i = 0; i < move.effects.Length; i++)
        {
            AurenGenMoveEffect effect = move.effects[i];
            if (effect == null || effect.timing != timing)
                continue;
            AurenGenMoveEffectContext context = new AurenGenMoveEffectContext();
            context.controller = this;
            context.attacker = attacker;
            context.defender = defender;
            context.move = move;
            context.attackerIsPlayer = attackerIsPlayer;
            context.attackerRawRoll = attackerRawRoll;
            context.defenderRawRoll = defenderRawRoll;
            context.attackerFinalRoll = attackerFinalRoll;
            context.defenderFinalRoll = defenderFinalRoll;
            context.cancelAttackerMove = false;
            context.cancelDefenderMove = false;
            effect.Execute(context);
            if (attackerIsPlayer)
            {
                if (context.cancelAttackerMove)
                    _playerMoveCancelledForRound = true;
                if (context.cancelDefenderMove)
                    _enemyMoveCancelledForRound = true;
            }
            else
            {
                if (context.cancelAttackerMove)
                    _enemyMoveCancelledForRound = true;
                if (context.cancelDefenderMove)
                    _playerMoveCancelledForRound = true;
            }
        }
    }

    bool HasParryEffect(AurenGenMoveData move)
    {
        if (move == null || move.effects == null)
            return false;
        for (int i = 0; i < move.effects.Length; i++)
        {
            if (move.effects[i] is AurenGenParryEffect)
                return true;
        }
        return false;
    }

    public int ResolveParryGuess(bool forPlayer, int enemyAIGuess)
    {
        if (!forPlayer)
            return Mathf.Clamp(enemyAIGuess, 1, 20);
        if (parryGuessInput == null)
            return Random.Range(1, 21);
        if (int.TryParse(parryGuessInput.text, out int parsed))
            return Mathf.Clamp(parsed, 1, 20);
        return Random.Range(1, 21);
    }

    public void ShowParryGuess(bool forPlayer, int guess)
    {
        if (parryGuessLabel == null)
            return;
        if (forPlayer)
            parryGuessLabel.text = "Parry Guess: " + guess;
        else
            parryGuessLabel.text = "Enemy Parry Guess: " + guess;
    }

    public void WriteBattleEvent(string text)
    {
        WriteLog(text);
    }

    void WriteLog(string text)
    {
        if (battleLogLabel != null && !string.IsNullOrEmpty(text))
            battleLogLabel.text = text;
    }
}
