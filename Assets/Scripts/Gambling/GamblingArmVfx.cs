using UnityEngine;

[DisallowMultipleComponent]
public class GamblingArmVfx : MonoBehaviour
{
    [Header("Projectile attachments")]
    public GameObject projectileFireVfxPrefab;
    public GameObject projectileIceVfxPrefab;

    [Header("Projectile — Ice (when no dedicated ice prefab on GamblingArm)")]
    [Tooltip("Multiplies sprite color before blending toward vivid ice blue.")]
    public Color projectileIceSpriteTint = new Color(0.02f, 0.2f, 1f, 1f);

    [Tooltip("Final blend toward this saturated ice blue (alpha from sprite).")]
    public Color projectileIceVivid = new Color(0f, 0.35f, 1f, 1f);

    [Range(0f, 1f)]
    [Tooltip("How strongly to snap projectiles to vivid ice blue.")]
    public float projectileIceVividBlend = 0.94f;

    [Header("Cinder (on enemy when fire DoT applies)")]
    public GameObject cinderBlackSmokePrefab;

    [Header("Enemy hit crosses")]
    public GameObject hitCrossFire;
    public GameObject hitCrossIce;
    public GameObject hitCrossPlain;

    [Header("Punch arcs (360 spiral, rotated to aim)")]
    public GameObject punchTrailFire;
    public GameObject punchTrailIce;
    public GameObject punchTrailPlain;

    [Header("Hammer / shockwave")]
    public GameObject shockwaveFire;
    public GameObject shockwaveIce;
    public GameObject shockwaveGroundPlain;

    [Header("Lifetime")]
    public float defaultVfxLifetime = 2.5f;

    [Header("Mushroom wider VFX")]
    public float mushroomTrailScaleMul = 1.35f;
    public float mushroomShockwaveScaleMul = 1.4f;

    [Header("Punch trail")]
    public Vector3 punchTrailLocalOffset = Vector3.zero;
    public float punchTrailRotationOffsetDegrees = 0f;

    
    public void AttachProjectileFireFx(Transform projectile)
    {
        if (projectileFireVfxPrefab == null || projectile == null)
            return;

        GameObject fx = Instantiate(projectileFireVfxPrefab, projectile);
        fx.transform.localPosition = Vector3.zero;
        fx.transform.localRotation = Quaternion.identity;
        Destroy(fx, defaultVfxLifetime + 2f);
    }

    public void AttachProjectileIceFx(Transform projectile)
    {
        if (projectileIceVfxPrefab == null || projectile == null)
            return;

        GameObject fx = Instantiate(projectileIceVfxPrefab, projectile);
        fx.transform.localPosition = Vector3.zero;
        fx.transform.localRotation = Quaternion.identity;
        Destroy(fx, defaultVfxLifetime + 2f);
    }

    public void ApplyProjectileIceSpriteTint(Transform projectile)
    {
        if (projectile == null)
            return;

        foreach (SpriteRenderer sr in projectile.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color c = sr.color;
            Color baseTinted = c * projectileIceSpriteTint;
            Color vivid = new Color(projectileIceVivid.r, projectileIceVivid.g, projectileIceVivid.b, c.a);
            sr.color = Color.Lerp(baseTinted, vivid, projectileIceVividBlend);
        }
    }

    public void SpawnCinderSmokeOnEnemy(Transform enemyRoot)
    {
        if (cinderBlackSmokePrefab == null || enemyRoot == null)
            return;

        Vector3 p = enemyRoot.position + Vector3.up * 0.25f;
        GameObject go = Instantiate(cinderBlackSmokePrefab, p, Quaternion.identity, enemyRoot);
        Destroy(go, defaultVfxLifetime + 2f);
    }

    public void SpawnEnemyHitCross(Vector3 worldPosition, GamblingModifierType modifier)
    {
        GameObject prefab = null;
        switch (modifier)
        {
            case GamblingModifierType.Fire:
                prefab = hitCrossFire;
                break;
            case GamblingModifierType.Ice:
                prefab = hitCrossIce;
                break;
            default:
                prefab = hitCrossPlain;
                break;
        }

        if (prefab == null)
            return;

        GameObject go = Instantiate(prefab, worldPosition, Quaternion.identity);
        DestroyWhenDone(go);
    }

    
    public void PlayPunchTrail(Vector2 aimWorld, GamblingModifierType modifier, bool mushroomWide, Transform playerRoot)
    {
        Transform parent = playerRoot != null ? playerRoot : transform;
        GameObject prefab = null;
        switch (modifier)
        {
            case GamblingModifierType.Fire:
                prefab = punchTrailFire;
                break;
            case GamblingModifierType.Ice:
                prefab = punchTrailIce;
                break;
            default:
                prefab = punchTrailPlain;
                break;
        }

        if (prefab == null)
            return;

        
        const float punchTrailClockwiseQuarterTurn = -90f;
        float aimDeg = Mathf.Atan2(aimWorld.y, aimWorld.x) * Mathf.Rad2Deg + punchTrailClockwiseQuarterTurn + punchTrailRotationOffsetDegrees;
        Quaternion worldRot = Quaternion.AngleAxis(aimDeg, Vector3.forward);
        Vector3 worldPos = parent.TransformPoint(punchTrailLocalOffset);

        GameObject go = Instantiate(prefab, worldPos, worldRot);
        go.transform.SetParent(parent, true);
        
        go.transform.SetPositionAndRotation(worldPos, worldRot);
        if (mushroomWide)
            go.transform.localScale = go.transform.localScale * mushroomTrailScaleMul;

        DestroyWhenDone(go);
    }

    public void PlayShockwave(GamblingModifierType modifier, bool mushroomWide, Vector3 worldCenter)
    {
        GameObject prefab = null;
        switch (modifier)
        {
            case GamblingModifierType.Fire:
                prefab = shockwaveFire;
                break;
            case GamblingModifierType.Ice:
                prefab = shockwaveIce;
                break;
            default:
                prefab = shockwaveGroundPlain;
                break;
        }

        if (prefab == null)
            return;

        GameObject go = Instantiate(prefab, worldCenter, Quaternion.identity);
        if (mushroomWide)
            go.transform.localScale = go.transform.localScale * mushroomShockwaveScaleMul;

        DestroyWhenDone(go);
    }

    private void DestroyWhenDone(GameObject go)
    {
        if (go == null) return;
        var ps = go.GetComponentsInChildren<ParticleSystem>();
        float max = defaultVfxLifetime;
        foreach (var p in ps)
        {
            var m = p.main;
            float life = m.duration + m.startLifetime.constantMax + 0.5f;
            if (life > max) max = life;
        }

        Destroy(go, Mathf.Clamp(max, 0.5f, 8f));
    }
}
