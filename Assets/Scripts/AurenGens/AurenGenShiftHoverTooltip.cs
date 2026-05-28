using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class AurenGenShiftHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Shown when Shift+hover.")]
    public GameObject tooltipRoot;

    [Tooltip("Tooltip body.")]
    public TextMeshProUGUI tooltipBody;

    [Tooltip("Inspector description.")]
    [TextArea(2, 8)]
    public string description = "";

    bool _hover;

    void Update()
    {
        if (tooltipRoot == null)
            return;
        bool shift = Keyboard.current != null &&
                     (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        tooltipRoot.SetActive(shift && _hover && !string.IsNullOrEmpty(description));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hover = true;
        if (tooltipBody != null)
            tooltipBody.text = description;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hover = false;
        if (tooltipRoot != null)
            tooltipRoot.SetActive(false);
    }
}
