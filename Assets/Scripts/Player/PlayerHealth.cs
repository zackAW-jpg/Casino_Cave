using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public PlayerStateSO state;

    public event Action<int, int> OnHealthChanged;

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
        RaiseHealthChanged();
    }

    public void TakeDamage(int amount)
    {
        if (state == null) return;
        if (amount <= 0) return;

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

    private void Die()
    {
        Debug.Log("Player died");
        // TODO: respawn, game over, etc.
    }
}