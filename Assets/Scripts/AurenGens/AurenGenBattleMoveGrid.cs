using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AurenGenBattleMoveGrid : MonoBehaviour
{
    [Tooltip("Six anchors order 1..6 = TL,TR,ML,MR,BL,BR.")]
    public RectTransform[] slotAnchors = new RectTransform[6];

    [Tooltip("Button prefab: needs child TMPs named MoveName and HitLine.")]
    public Button moveButtonPrefab;

    readonly List<Button> _spawned = new List<Button>();

    public void ClearGrid()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
                Destroy(_spawned[i].gameObject);
        }
        _spawned.Clear();
    }

    public void Rebuild(AurenGenRuntimeUnit unit, AurenGenMoveData selected, System.Action<AurenGenMoveData> onPick, bool battleInputEnabled)
    {
        ClearGrid();
        if (unit == null || unit.Definition == null || unit.Definition.moves == null || moveButtonPrefab == null)
            return;
        for (int slot = 1; slot <= 6; slot++)
        {
            AurenGenMoveData move = FindMoveForSlot(unit, slot);
            if (move == null)
                continue;
            int anchorIndex = slot - 1;
            if (anchorIndex < 0 || anchorIndex >= slotAnchors.Length || slotAnchors[anchorIndex] == null)
                continue;
            Button btn = Instantiate(moveButtonPrefab, slotAnchors[anchorIndex]);
            RectTransform rt = btn.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
            }
            TextMeshProUGUI nameTmp = btn.transform.Find("MoveName")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI hitTmp = btn.transform.Find("HitLine")?.GetComponent<TextMeshProUGUI>();
            if (nameTmp != null)
            {
                string label = move.moveName;
                if (move.isRideTheBusMove)
                    label = "Ride the Bus " + (unit.RideBusCompleteStreak + 1);
                nameTmp.text = label;
            }
            if (hitTmp != null)
            {
                if (move.isRideTheBusMove && unit != null)
                {
                    int minBet = 10;
                    int maxBet = Mathf.Max(minBet, unit.CurrentDebtPoints - 1);
                    int previewBet = Mathf.Clamp(minBet, minBet, maxBet);
                    int preview = unit.PreviewRideBusDamage(previewBet);
                    hitTmp.text = "To hit: " + move.hitThreshold + "  Damage: " + preview + " (min bet)";
                }
                else
                    hitTmp.text = "To hit: " + move.hitThreshold;
            }
            bool debtLocked = move.isDebtCollectorMove && !unit.CanUseDebtCollector();
            btn.interactable = battleInputEnabled && !debtLocked;
            AurenGenShiftHoverTooltip tip = btn.GetComponent<AurenGenShiftHoverTooltip>();
            if (tip != null)
                tip.description = BuildTooltipText(unit, move);
            AurenGenMoveData captured = move;
            btn.onClick.AddListener(() => onPick?.Invoke(captured));
            if (selected == move)
            {
                var colors = btn.colors;
                colors.colorMultiplier = 1.2f;
                btn.colors = colors;
            }
            _spawned.Add(btn);
        }
    }

    static AurenGenMoveData FindMoveForSlot(AurenGenRuntimeUnit unit, int slot1to6)
    {
        if (unit.Definition.moves == null)
            return null;
        for (int i = 0; i < unit.Definition.moves.Length; i++)
        {
            AurenGenMoveData m = unit.Definition.moves[i];
            if (m == null)
                continue;
            if (m.menuSlotIndex != slot1to6)
                continue;
            if (m.isDebtCollectorMove && (unit.Definition == null || !unit.Definition.isUltimateVariant))
                continue;
            return m;
        }
        return null;
    }

    static string BuildTooltipText(AurenGenRuntimeUnit unit, AurenGenMoveData move)
    {
        if (move == null)
            return "";
        string core = string.IsNullOrEmpty(move.description) ? "No description." : move.description;
        if (!move.isDebtCollectorMove || unit == null || unit.Definition == null || !unit.Definition.isUltimateVariant)
            return core;
        if (unit.DebtCollectorUsedThisMatch)
            return core + "\n\nDebt Collector already used this match.";
        if (unit.CanUseDebtCollector())
            return core;
        int left = Mathf.Max(0, 3 - unit.ConsecutiveTurnsActive);
        return core + "\n\n" + left + " more active round(s) until Debt Collector unlocks.";
    }
}
