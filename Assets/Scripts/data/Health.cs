using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHP = 5;
    public int currentHP;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        currentHP -= amount;
        Debug.Log($"{name} took {amount} damage, HP now {currentHP}");

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} died");
        Destroy(gameObject);
    }
}