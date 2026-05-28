using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
[DisallowMultipleComponent]
public class AurenGenBattleNarrativePresenter : MonoBehaviour
{
    [Header("Roots")]
    [Tooltip("Planning UI root (moves + buttons).")]
    public GameObject planningRoot;

    [Tooltip("Narrative replaces bottom during round.")]
    public GameObject narrativeRoot;

    [Tooltip("Dim overlay optional.")]
    public CanvasGroup dimOverlay;

    [Header("Narrative")]
    [Tooltip("Main body TMP.")]
    public TextMeshProUGUI narrativeBody;

    [Tooltip("Hint for advance.")]
    public TextMeshProUGUI continueHint;

    [Tooltip("Shown only while planning (bottom hint for shift tooltips).")]
    public TextMeshProUGUI planningBottomHint;

    [Tooltip("Default text for planning hint.")]
    [TextArea(1, 2)]
    public string planningBottomHintDefault = "Hold Shift while hovering over an option for more information!";

    [Tooltip("Chars per second typewriter.")]
    [Min(1)]
    public float typewriterCharsPerSecond = 80f;

    bool _narrativeMode;

    public bool IsNarrativeMode => _narrativeMode;

    public void SetPlanningVisible(bool visible)
    {
        if (planningRoot != null)
            planningRoot.SetActive(visible);
        RefreshPlanningHint(visible);
    }

    void RefreshPlanningHint(bool planningVisible)
    {
        if (planningBottomHint == null)
            return;
        bool show = planningVisible && !_narrativeMode;
        planningBottomHint.gameObject.SetActive(show);
        if (show)
            planningBottomHint.text = string.IsNullOrEmpty(planningBottomHintDefault)
                ? "Hold Shift while hovering over an option for more information!"
                : planningBottomHintDefault;
    }

    public void SetNarrativeMode(bool narrative)
    {
        _narrativeMode = narrative;
        if (planningRoot != null)
            planningRoot.SetActive(!narrative);
        if (narrativeRoot != null)
            narrativeRoot.SetActive(narrative);
        if (dimOverlay != null)
        {
            dimOverlay.blocksRaycasts = narrative;
            dimOverlay.alpha = narrative ? 0.35f : 0f;
        }
        if (continueHint != null)
            continueHint.gameObject.SetActive(narrative);
        RefreshPlanningHint(planningRoot != null && planningRoot.activeSelf);
    }

    public IEnumerator ShowAndAdvanceLine(string line)
    {
        if (narrativeBody != null)
            narrativeBody.text = "";
        yield return Typewriter(line);
        yield return WaitAdvance();
    }

    IEnumerator Typewriter(string full)
    {
        if (narrativeBody == null || string.IsNullOrEmpty(full))
            yield break;
        narrativeBody.text = "";
        float delay = 1f / typewriterCharsPerSecond;
        for (int i = 0; i < full.Length; i++)
        {
            narrativeBody.text += full[i];
            if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            {
                narrativeBody.text = full;
                yield break;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                narrativeBody.text = full;
                yield break;
            }
            yield return new WaitForSecondsRealtime(delay);
        }
    }

    IEnumerator WaitAdvance()
    {
        while (true)
        {
            if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                yield break;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                yield break;
            yield return null;
        }
    }
}
