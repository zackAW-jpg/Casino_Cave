using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple NPC dialogue: same E / Space / LMB advance as <see cref="NpcDialogueSequenceTrigger"/> (Matilda),
/// but no Yes/No phase — only lines. Slot machine unlocks when the Nth line <em>starts</em> (default: line 3 = index 2).
/// Sprites: add <see cref="NpcDialogueSpriteLayers"/> on the same NPC (sprite swap and/or pose child layers); this script calls it each step.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NpcMultiLineDialogueTrigger : MonoBehaviour
{
    public SpeechBubble speechBubble;

    [Header("Dialogue (same advance keys as Matilda)")]
    [Tooltip("Lines in order. Size = how many E-presses before dialogue ends.")]
    public string[] dialogueLines =
    {
        "Line 1.",
        "Line 2.",
        "Line 3 — slot arm unlocks when this line starts (default).",
        "Line 4."
    };

    [Header("Slot machine")]
    [Tooltip("When this line index starts (0 = first line, 1 = second, 2 = third), the gambling arm unlocks. Default 2 = line 3.")]
    [Min(0)]
    public int slotUnlockAtLineStartIndex = 2;

    [Header("Engagement")]
    [Tooltip("Applied at runtime to a CircleCollider2D on this object (world units). Larger = talk from farther away.")]
    [Min(0.1f)]
    public float engagementRadius = 2f;

    [Header("Refs")]
    [Tooltip("Optional: stacked pose children. If null, GetComponent<NpcDialogueSpriteLayers>() is used.")]
    public NpcDialogueSpriteLayers spriteLayers;

    [Header("One-shot")]
    [Tooltip("If true, after the player advances past the last line, entering the trigger will not start dialogue again.")]
    public bool disableInteractionAfterDialogueComplete = true;

    [Header("Input (new Input System)")]
    public Key advanceKey = Key.E;

    [Tooltip("If off, only keyboard advances dialogue. Left-click often fires the same frame as gameplay / UI and can skip lines.")]
    public bool advanceWithLeftClick;

    [Tooltip("After each advance, wait until E/Space (and mouse if used) are released so one physical press cannot skip multiple lines in the same frame or while held.")]
    public bool requireReleaseBeforeNextAdvance = true;

    [Header("Audio (optional)")]
    [Tooltip("NPC talk blip / UI tick each time the player advances a line (E / Space).")]
    public AudioClip advanceLineSound;
    public AudioSource audioSource;

    [Header("Speech bubble")]
    public bool disableAutoHideOnSpeechBubble = true;

    private Collider2D _col;
    private Coroutine _routine;
    private Transform _playerTransform;
    private bool _dialogueExhausted;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;

        if (_col is CircleCollider2D circle)
            circle.radius = engagementRadius;

        if (speechBubble == null)
            speechBubble = GetComponentInChildren<SpeechBubble>();

        if (spriteLayers == null)
            spriteLayers = GetComponent<NpcDialogueSpriteLayers>();

        if (speechBubble != null && disableAutoHideOnSpeechBubble)
            speechBubble.autoHideAfterSeconds = 0f;

        if (dialogueLines != null && dialogueLines.Length > 0 && slotUnlockAtLineStartIndex >= dialogueLines.Length)
            Debug.LogWarning($"{name}: slotUnlockAtLineStartIndex ({slotUnlockAtLineStartIndex}) is >= dialogue line count ({dialogueLines.Length}). Unlock will never run.", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (disableInteractionAfterDialogueComplete && _dialogueExhausted)
            return;

        _playerTransform = other.transform;

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(RunDialogue());
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (other.transform != _playerTransform)
            return;

        StopDialogue();
    }

    private void StopDialogue()
    {
        _playerTransform = null;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (speechBubble != null)
            speechBubble.Hide();

        if (spriteLayers != null)
            spriteLayers.OnDialogueEnded();
    }

    private bool IsInRange() => _playerTransform != null;

    private bool WasAdvancePressed()
    {
        if (Keyboard.current == null) return false;

        if (Keyboard.current[advanceKey].wasPressedThisFrame) return true;
        if (Keyboard.current.spaceKey.wasPressedThisFrame) return true;

        if (advanceWithLeftClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        return false;
    }

    /// <summary>
    /// True while any key that can advance dialogue is held (not wasPressed — avoids same-frame double advance).
    /// </summary>
    private bool AnyAdvanceKeyHeld()
    {
        if (Keyboard.current == null) return false;

        if (Keyboard.current[advanceKey].isPressed) return true;
        if (Keyboard.current.spaceKey.isPressed) return true;

        if (advanceWithLeftClick && Mouse.current != null && Mouse.current.leftButton.isPressed)
            return true;

        return false;
    }

    private void PlayAdvanceLineSound()
    {
        if (advanceLineSound == null) return;

        if (audioSource != null)
            audioSource.PlayOneShot(advanceLineSound);
        else
            AudioSource.PlayClipAtPoint(advanceLineSound, transform.position, 1f);
    }

    private static string NormalizeLine(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        return raw.Replace("\\n", "\n");
    }

    private IEnumerator RunDialogue()
    {
        if (speechBubble == null)
        {
            Debug.LogError($"{name}: Assign SpeechBubble (child) or speechBubble reference.", this);
            yield break;
        }

        if (dialogueLines == null || dialogueLines.Length == 0)
            yield break;

        // Don't treat input from the same frame as trigger entry as "advance first line".
        yield return null;

        for (int i = 0; i < dialogueLines.Length; i++)
        {
            if (!IsInRange()) yield break;

            if (i == slotUnlockAtLineStartIndex)
            {
                GamblingArmRuntimeState.SetSlotsUnlocked(true);
                GameplaySaveContext.PersistRun();
            }

            if (spriteLayers != null)
                spriteLayers.SetStep(i);

            string t = NormalizeLine(dialogueLines[i]);
            if (string.IsNullOrWhiteSpace(t))
                t = "…";

            speechBubble.Show(t);

            // Same-frame advance: wasPressedThisFrame can still be true the frame Show() runs; wait until next frame before listening.
            yield return new WaitForEndOfFrame();
            if (!IsInRange()) yield break;

            while (IsInRange() && !WasAdvancePressed())
                yield return null;

            if (!IsInRange()) yield break;

            // wasPressedThisFrame is true for the whole frame — without this, the same press advances the next line immediately.
            yield return null;
            if (!IsInRange()) yield break;

            if (requireReleaseBeforeNextAdvance)
            {
                while (IsInRange() && AnyAdvanceKeyHeld())
                    yield return null;
                if (!IsInRange()) yield break;
            }

            if (i < dialogueLines.Length - 1)
                PlayAdvanceLineSound();
        }

        if (disableInteractionAfterDialogueComplete)
            _dialogueExhausted = true;

        StopDialogue();
    }
}
