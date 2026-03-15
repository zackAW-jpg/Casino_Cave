using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public PlayerStateSO state;

    private void Awake()
    {
        if (state == null)
        {
            Debug.LogError("PlayerHealth: PlayerStateSO is not assigned.", this);
            return;
        }

        state.currentHP = state.maxHP;
    }

    public void TakeDamage(int amount)
    {
        if (state == null) return;
        if (amount <= 0) return;

        state.currentHP -= amount;
        Debug.Log($"Player took {amount} damage, HP now {state.currentHP}");

        if (state.currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player died");
        // TODO: respawn, game over, etc.
    }
}