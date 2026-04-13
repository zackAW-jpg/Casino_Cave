using UnityEngine;

public static class GamblingArmHitProcessor
{
    public const float CinderDelaySeconds = 1f;
    public const int CinderDamage = 1;
    public const float IceSlowDuration = 5f;

    public static void ApplyModifiersOnHit(
        GameObject hitRoot,
        GamblingModifierType modifier,
        Health health,
        BossHealth bossHealth,
        Transform playerRoot,
        GameObject goldCoinPrefab,
        GamblingArmVfx vfx = null)
    {
        StatusEffectHost host = hitRoot.GetComponent<StatusEffectHost>();
        if (host == null)
            host = hitRoot.AddComponent<StatusEffectHost>();

        switch (modifier)
        {
            case GamblingModifierType.Fire:
                if (health != null || bossHealth != null)
                {
                    host.ScheduleCinderDamage(CinderDamage, CinderDelaySeconds, health, bossHealth);
                    vfx?.SpawnCinderSmokeOnEnemy(hitRoot.transform);
                }
                break;
            case GamblingModifierType.Ice:
                host.ApplyIceSlow(IceSlowDuration);
                break;
            case GamblingModifierType.Gold:
                if (goldCoinPrefab != null && hitRoot.transform != null)
                {
                    Vector3 pos = hitRoot.transform.position + UnityEngine.Random.insideUnitSphere * 0.15f;
                    pos.z = 0f;
                    Object.Instantiate(goldCoinPrefab, pos, Quaternion.identity);
                }
                break;
        }
    }
}
