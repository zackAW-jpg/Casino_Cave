using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Run HUD: HP bar (with hurt feedback) and gold. Assign PlayerStateSO + optional PlayerHealth on the player.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Data")]
    public PlayerStateSO playerState;

    [Header("Player (for damage events)")]
    [Tooltip("Player root with PlayerHealth. If null, HP bar still updates from state but hurt animation only runs when health drops (polled).")]
    public PlayerHealth playerHealth;

    [Header("HP bar")]
    public Image hpFillImage;
    [Tooltip("Image type must be Filled, Fill Method Horizontal.")]
    public Image hpHurtOverlay;
    [Tooltip("Optional: bar root for a small scale punch on damage.")]
    public RectTransform hpBarRoot;

    [Header("Gold")]
    public TextMeshProUGUI goldText;

    [Header("Tuning")]
    [Tooltip("Seconds for HP fill to catch up to actual HP after a change.")]
    public float hpFillLerpDuration = 0.2f;
    [Tooltip("Hurt overlay max alpha.")]
    public float hurtFlashPeakAlpha = 0.45f;
    public float hurtFlashDuration = 0.25f;
    public float hurtScalePunch = 1.06f;
    public float hurtScalePunchDuration = 0.12f;

    private int _lastKnownHp;
    private float _displayedHp;
    private Coroutine _hurtRoutine;
    private Coroutine _scaleRoutine;
    private int _lastGold;

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= HandleHealthChanged;
    }

    private void Start()
    {
        if (playerState == null)
        {
            Debug.LogError("PlayerHUD: assign PlayerStateSO.", this);
            enabled = false;
            return;
        }

        _lastKnownHp = playerState.currentHP;
        _displayedHp = playerState.currentHP;
        _lastGold = playerState.gold;

        ApplyHpFillImmediate();
        if (hpHurtOverlay != null)
        {
            Color c = hpHurtOverlay.color;
            c.a = 0f;
            hpHurtOverlay.color = c;
        }

        RefreshGoldText();

        if (playerHealth == null)
            Debug.LogWarning("PlayerHUD: PlayerHealth not assigned — hurt flash only works if you assign it.", this);
    }

    private void Update()
    {
        if (playerState == null) return;

        // Smooth fill toward actual HP
        if (hpFillImage != null && playerState.maxHP > 0)
        {
            float target = playerState.currentHP;
            float hpPerSecond = playerState.maxHP / Mathf.Max(0.01f, hpFillLerpDuration);
            _displayedHp = Mathf.MoveTowards(_displayedHp, target, hpPerSecond * Time.deltaTime);
            hpFillImage.fillAmount = Mathf.Clamp01(_displayedHp / playerState.maxHP);
        }

        if (playerHealth == null)
        {
            if (playerState.currentHP < _lastKnownHp)
                PlayHurtFeedback();
            _lastKnownHp = playerState.currentHP;
        }

        if (playerState.gold != _lastGold)
        {
            _lastGold = playerState.gold;
            RefreshGoldText();
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (current < _lastKnownHp)
            PlayHurtFeedback();
        _lastKnownHp = current;
    }

    private void PlayHurtFeedback()
    {
        if (hpHurtOverlay != null)
        {
            if (_hurtRoutine != null)
                StopCoroutine(_hurtRoutine);
            _hurtRoutine = StartCoroutine(HurtFlashRoutine());
        }

        if (hpBarRoot != null)
        {
            if (_scaleRoutine != null)
                StopCoroutine(_scaleRoutine);
            _scaleRoutine = StartCoroutine(ScalePunchRoutine());
        }
    }

    private IEnumerator HurtFlashRoutine()
    {
        Color c = hpHurtOverlay.color;
        c.a = hurtFlashPeakAlpha;
        hpHurtOverlay.color = c;

        float t = 0f;
        while (t < hurtFlashDuration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(hurtFlashPeakAlpha, 0f, t / hurtFlashDuration);
            hpHurtOverlay.color = c;
            yield return null;
        }

        c.a = 0f;
        hpHurtOverlay.color = c;
        _hurtRoutine = null;
    }

    private IEnumerator ScalePunchRoutine()
    {
        Vector3 baseScale = hpBarRoot.localScale;
        float half = hurtScalePunchDuration * 0.5f;
        float t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            hpBarRoot.localScale = Vector3.Lerp(baseScale, baseScale * hurtScalePunch, t / half);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            hpBarRoot.localScale = Vector3.Lerp(baseScale * hurtScalePunch, baseScale, t / half);
            yield return null;
        }

        hpBarRoot.localScale = baseScale;
        _scaleRoutine = null;
    }

    private void ApplyHpFillImmediate()
    {
        if (hpFillImage == null || playerState.maxHP <= 0) return;
        hpFillImage.fillAmount = Mathf.Clamp01((float)playerState.currentHP / playerState.maxHP);
    }

    private void RefreshGoldText()
    {
        if (goldText != null)
            goldText.text = $"Gold: {playerState.gold}";
    }
}
