using System;
using UnityEngine;

/// <summary>
/// Boss HP (separate from generic <see cref="Health"/>). Drives <see cref="BossHUD"/> via events.
/// </summary>
public class BossHealth : MonoBehaviour
{
    public int maxHP = 40;
    public int currentHP;

    [Header("UI")]
    [Tooltip("Shown in the boss HUD next to the health bar.")]
    public string displayName = "The Burnt One";

    [Header("Loot on death")]
    [Tooltip("If set, this many coins spawn in a ring when the boss dies (often assigned by DungeonRoomSpawner).")]
    public GameObject coinPrefab;
    public int coinsOnDeath = 15;
    public float coinSpawnRadius = 2.5f;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;

    private void Awake()
    {
        currentHP = maxHP;
        RaiseChanged();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        currentHP -= amount;
        if (currentHP < 0) currentHP = 0;

        Debug.Log($"{name} took {amount} damage, HP now {currentHP}");

        RaiseChanged();

        if (currentHP <= 0)
            Die();
    }

    private void Die()
    {
        SpawnDeathCoins();
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    private void SpawnDeathCoins()
    {
        if (coinPrefab == null || coinsOnDeath <= 0)
            return;

        Vector3 origin = transform.position;
        Transform parent = transform.parent;

        for (int i = 0; i < coinsOnDeath; i++)
        {
            Vector2 offset = Random.insideUnitCircle * coinSpawnRadius;
            Vector3 pos = origin + new Vector3(offset.x, offset.y, 0f);
            Instantiate(coinPrefab, pos, Quaternion.identity, parent);
        }
    }

    private void RaiseChanged()
    {
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }
}
