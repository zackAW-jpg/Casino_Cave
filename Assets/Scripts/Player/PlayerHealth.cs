using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public PlayerStateSO state;

    [Header("Health potions")]
    [Tooltip("How many potions at a new run (written to PlayerStateSO.healthPotionCount in Awake).")]
    public int startingHealthPotions = 5;

    public event Action<int, int> OnHealthChanged;

    /// <summary>Time.time when damage was last applied (after temp HP). Used to avoid accidental door transitions during knockback.</summary>
    public float LastDamageTime { get; private set; } = -1000f;

    [Header("Temporary HP")]
    [Tooltip("Runtime-only temporary HP that is consumed before currentHP.")]
    public int tempHP = 0;

    private void Awake()
    {
        if (state == null)
        {
            Debug.LogError("PlayerHealth: PlayerStateSO is not assigned.", this);
            return;
        }

        state.currentHP = state.maxHP;
        tempHP = 0;
        state.healthPotionCount = startingHealthPotions;
        RaiseHealthChanged();
    }

    public void TakeDamage(int amount)
    {
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

    /// <summary>Door / transitions can use this to ignore triggers right after hits + knockback.</summary>
    public bool IsRecentlyDamaged(float windowSeconds)
    {
        return Time.time - LastDamageTime < windowSeconds;
    }

    private void Die()
    {
        Debug.Log("Player died");
        // TODO: respawn, game over, etc.
    }
}