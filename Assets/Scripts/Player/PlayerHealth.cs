using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public PlayerStateSO state;

    /// <summary>Fired after HP changes (damage, heal, etc.). Arguments: currentHP, maxHP.</summary>
    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        if (state == null)
        {
            Debug.LogError("PlayerHealth: PlayerStateSO is not assigned.", this);
            return;
        }

        state.currentHP = state.maxHP;
        RaiseHealthChanged();
    }

    public void TakeDamage(int amount)
    {
        if (state == null) return;
        if (amount <= 0) return;

        state.currentHP -= amount;
        Debug.Log($"Player took {amount} damage, HP now {state.currentHP}");

        RaiseHealthChanged();

        gameObject.SendMessage("OnKnockbackReceived", SendMessageOptions.DontRequireReceiver);

        if (state.currentHP <= 0)
        {
            Die();
        }
    }

    /// <summary>Call when healing or changing max HP at runtime.</summary>
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