using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hold R to drink a health potion after a 1s channel. Moving is allowed; dodge and attack are blocked.
/// Interrupted by damage: no potion consumed. On success: consumes one potion and heals up to max HP.
/// </summary>
public class PlayerHealChannel : MonoBehaviour
{
    [Header("Channel")]
    [Tooltip("Seconds before the potion is consumed and heal is applied.")]
    public float channelDuration = 1f;

    private PlayerHealth _health;
    private PlayerDodge _dodge;
    private Coroutine _routine;
    private bool _interrupted;
    private bool _requireReleaseBeforeNextHeal;

    public bool IsChannelingHeal { get; private set; }

    private void Awake()
    {
        _health = GetComponent<PlayerHealth>();
        _dodge = GetComponent<PlayerDodge>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (_requireReleaseBeforeNextHeal)
        {
            if (!Keyboard.current.rKey.isPressed)
                _requireReleaseBeforeNextHeal = false;
            return;
        }

        if (!Keyboard.current.rKey.wasPressedThisFrame)
            return;

        TryStartHeal();
    }

    private void TryStartHeal()
    {
        if (IsChannelingHeal)
            return;
        if (_health == null || _health.state == null)
            return;
        if (_dodge != null && _dodge.IsDodging)
            return;

        PlayerStateSO state = _health.state;
        if (state.healthPotionCount <= 0)
            return;
        if (state.currentHP >= state.maxHP)
            return;

        _interrupted = false;
        _requireReleaseBeforeNextHeal = true;
        if (_routine != null)
            StopCoroutine(_routine);
        _routine = StartCoroutine(HealChannelRoutine());
    }

    private IEnumerator HealChannelRoutine()
    {
        IsChannelingHeal = true;
        float elapsed = 0f;
        while (elapsed < channelDuration)
        {
            if (_interrupted)
            {
                IsChannelingHeal = false;
                _routine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!_interrupted)
            ApplySuccessfulHeal();

        IsChannelingHeal = false;
        _routine = null;
    }

    private void ApplySuccessfulHeal()
    {
        if (_health == null || _health.state == null)
            return;

        PlayerStateSO state = _health.state;
        if (state.healthPotionCount <= 0)
            return;

        int missing = state.maxHP - state.currentHP;
        if (missing <= 0)
            return;

        state.healthPotionCount--;
        int heal = Mathf.Min(state.healthPotionHealAmount, missing);
        state.currentHP += heal;
        _health.RaiseHealthChanged();
    }

    /// <summary>Called from <see cref="PlayerHealth.TakeDamage"/> before damage is applied.</summary>
    public void CancelFromDamage()
    {
        if (!IsChannelingHeal)
            return;

        _interrupted = true;
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        IsChannelingHeal = false;
    }
}
