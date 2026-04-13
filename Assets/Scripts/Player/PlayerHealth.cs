using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public PlayerStateSO state;

    [Header("Health potions")]
    public int startingHealthPotions = 5;

    [Header("Audio (optional)")]
    public AudioSource hurtAudioSource;
    public AudioClip hurtSound;
    public AudioClip deathSound;

    [Header("Death presentation (optional)")]
    public Animator deathAnimator;
    public string deathAnimTrigger = "Death";
    public float deathAnimHoldSeconds = 0.85f;
    public float fallbackDeathHoldSeconds = 0.6f;

    public event Action<int, int> OnHealthChanged;

    
    public float LastDamageTime { get; private set; } = -1000f;

    [Header("Temporary HP")]
    public int tempHP = 0;

    bool _dead;

    private void Awake()
    {
        if (state == null)
        {
            Debug.LogError("PlayerHealth: PlayerStateSO is not assigned.", this);
            return;
        }

        if (ContinueLoadGuard.ConsumeSkipPlayerHealthInit())
        {
            tempHP = 0;
            RaiseHealthChanged();
            return;
        }

        state.currentHP = state.maxHP;
        tempHP = 0;
        state.healthPotionCount = startingHealthPotions;
        RaiseHealthChanged();
    }

    public void TakeDamage(int amount)
    {
        if (_dead) return;
        if (state == null) return;
        if (amount <= 0) return;

        if (TryGetComponent(out PlayerHealChannel healChannel))
            healChannel.CancelFromDamage();

        if (TryGetComponent(out PlayerDodge dodge))
        {
            if (dodge.IsInInvulnerableWindow)
                return;
            if (dodge.IsDodging)
                dodge.CancelDodge();
        }

        int dmgRemaining = amount;

        if (tempHP > 0)
        {
            int usedFromTemp = Mathf.Min(tempHP, dmgRemaining);
            tempHP -= usedFromTemp;
            dmgRemaining -= usedFromTemp;
        }

        state.currentHP -= dmgRemaining;

        LastDamageTime = Time.time;

        Debug.Log($"Player took {amount} damage. currentHP={state.currentHP}, tempHP={tempHP}");

        RaiseHealthChanged();

        gameObject.SendMessage("OnKnockbackReceived", SendMessageOptions.DontRequireReceiver);

        if (hurtAudioSource == null)
            hurtAudioSource = GetComponent<AudioSource>();
        if (state.currentHP > 0)
            SfxUtil.PlayOneShot(hurtSound, hurtAudioSource, transform.position);

        if (state.currentHP <= 0)
        {
            Die();
        }
    }

    public void HealWithTempOverflow(int amount)
    {
        if (state == null) return;
        if (amount <= 0) return;

        int healRemaining = amount;

        int missing = state.maxHP - state.currentHP;
        if (missing > 0)
        {
            int toCurrent = Mathf.Min(missing, healRemaining);
            state.currentHP += toCurrent;
            healRemaining -= toCurrent;
        }

        if (healRemaining > 0)
        {
            tempHP += healRemaining;
        }

        Debug.Log($"Player healed {amount}. currentHP={state.currentHP}, tempHP={tempHP}");

        RaiseHealthChanged();
    }

    public void RaiseHealthChanged()
    {
        if (state == null) return;
        OnHealthChanged?.Invoke(state.currentHP, state.maxHP);
    }

    
    public bool IsRecentlyDamaged(float windowSeconds)
    {
        return Time.time - LastDamageTime < windowSeconds;
    }

    private void Die()
    {
        if (_dead) return;
        _dead = true;
        Debug.Log("Player died");

        if (hurtAudioSource == null)
            hurtAudioSource = GetComponent<AudioSource>();
        SfxUtil.PlayOneShot(deathSound, hurtAudioSource, transform.position);

        if (GameplayMenusController.Instance != null)
            GameplayMenusController.Instance.BeginDeathSequence(this);
        else
            Debug.LogWarning("PlayerHealth: no GameplayMenusController in scene — death UI skipped.", this);
    }

    
    public void PrepareForFreshRunAfterDungeonReset()
    {
        _dead = false;
        if (state == null) return;
        state.currentHP = state.maxHP;
        tempHP = 0;
        RaiseHealthChanged();
    }

    public IEnumerator PlayDeathPresentationIfAny()
    {
        if (deathAnimator != null && !string.IsNullOrEmpty(deathAnimTrigger))
        {
            deathAnimator.SetTrigger(deathAnimTrigger);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, deathAnimHoldSeconds));
            yield break;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, fallbackDeathHoldSeconds));
    }
}
