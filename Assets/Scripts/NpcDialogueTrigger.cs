using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class NpcDialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [TextArea(2, 6)]
    public string dialogueText = "Hello, traveler!";

    [Header("Speech bubble (optional)")]
    public SpeechBubble speechBubble;

    [Header("Events")]
    public UnityEvent onDialogueTriggered;

    private Collider2D _col;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
        if (speechBubble == null)
            speechBubble = GetComponentInChildren<SpeechBubble>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (speechBubble != null)
            speechBubble.Show(dialogueText);
        else
            Debug.LogWarning("NpcDialogueTrigger: No SpeechBubble assigned or found on children. Assign it or add a child with SpeechBubble.", this);
        onDialogueTriggered?.Invoke();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (speechBubble != null && speechBubble.hideWhenPlayerLeaves)
            speechBubble.Hide();
    }
}
