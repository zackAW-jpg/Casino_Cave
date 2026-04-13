using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class NpcDialogueSequenceTrigger : MonoBehaviour
{
    public SpeechBubble speechBubble;

    [Header("Input (new Input System)")]
    public Key advanceKey = Key.E;
    public Key yesKey = Key.Y;
    public Key noKey = Key.N;

    [Tooltip("Extra text shown after the last line, while waiting for Yes/No.")]
    [TextArea(2, 4)]
    public string yesNoPromptSuffix = "\n\n[Y] Yes    [N] No";

    [Header("Timing")]
    public float postChoiceHideSeconds = 0.8f;

    [Header("Audio (optional)")]
    [Tooltip("NPC talk blip when advancing between lines (not when opening the Yes/No prompt).")]
    public AudioClip advanceLineSound;
    [Tooltip("If set, uses PlayOneShot; otherwise plays at this NPC's position.")]
    public AudioSource audioSource;

    private Collider2D _col;
    private Coroutine _routine;

    private Transform _playerTransform;
    private PlayerHealth _playerHealth;

    private INpcDealDialogueHandler _handler;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;

        if (speechBubble == null)
            speechBubble = GetComponentInChildren<SpeechBubble>();

        _handler = GetComponentInChildren<INpcDealDialogueHandler>();

        if (_handler == null)
            Debug.LogError($"{name}: No component implementing INpcDealDialogueHandler found under this NPC.", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _playerTransform = other.transform;
        _playerHealth = other.GetComponentInParent<PlayerHealth>();

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
        _playerHealth = null;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (speechBubble != null)
            speechBubble.Hide();
    }

    private bool IsInRange()
    {
        return _playerTransform != null;
    }

    private bool WasAdvancePressed()
    {
        if (Keyboard.current == null) return false;

        if (Keyboard.current[advanceKey].wasPressedThisFrame) return true;
        if (Keyboard.current.spaceKey.wasPressedThisFrame) return true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;

        return false;
    }

    private bool WasYesPressed()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current[yesKey].wasPressedThisFrame;
    }

    private bool WasNoPressed()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current[noKey].wasPressedThisFrame;
    }

    private void PlayAdvanceLineSound()
    {
        if (advanceLineSound == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(advanceLineSound);
            return;
        }

        AudioSource.PlayClipAtPoint(advanceLineSound, transform.position, 1f);
    }

    private IEnumerator RunDialogue()
    {
        if (_handler == null)
            yield break;

        string[] lines = _handler.GetDialogueLines();
        if (lines == null || lines.Length == 0)
            yield break;

        // Multi-line dialogue
        for (int i = 0; i < lines.Length; i++)
        {
            if (!IsInRange()) yield break;

            if (speechBubble != null)
                speechBubble.Show(lines[i]);

            // Wait for "next dialogue box"
            while (IsInRange() && !WasAdvancePressed())
                yield return null;

            if (!IsInRange()) yield break;

            if (advanceLineSound != null && i < lines.Length - 1)
                PlayAdvanceLineSound();
        }

        // Deal decision
        if (!IsInRange()) yield break;

        string lastLine = lines[lines.Length - 1];
        if (speechBubble != null)
            speechBubble.Show(lastLine + yesNoPromptSuffix);

        while (IsInRange())
        {
            if (WasYesPressed())
            {
                string result;
                _handler.OnDealYes(_playerHealth, out result);

                if (speechBubble != null && !string.IsNullOrEmpty(result))
                    speechBubble.Show(result);

                yield return new WaitForSecondsRealtime(postChoiceHideSeconds);
                StopDialogue();
                yield break;
            }

            if (WasNoPressed())
            {
                string result;
                _handler.OnDealNo(_playerHealth, out result);

                if (speechBubble != null && !string.IsNullOrEmpty(result))
                    speechBubble.Show(result);

                yield return new WaitForSecondsRealtime(postChoiceHideSeconds);
                StopDialogue();
                yield break;
            }

            yield return null;
        }
    }
}

// Implement this on the NPC (Matilda for now).
public interface INpcDealDialogueHandler
{
    string[] GetDialogueLines();
    void OnDealYes(PlayerHealth player, out string resultText);
    void OnDealNo(PlayerHealth player, out string resultText);
}