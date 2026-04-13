using UnityEngine;

/// <summary>
/// Spawns gambling-attack VFX. Assign prefabs in the Inspector (paths documented in tooltips).
/// </summary>
[DisallowMultipleComponent]
public class GamblingArmVfx : MonoBehaviour
{
    [Header("Projectile attachments")]
    [Tooltip("Assets/VFXPACK_FIRE_WALLCOEUR/Prefab/VFX_Fire 1.prefab")]
    public GameObject projectileFireVfxPrefab;

    [Header("Cinder (on enemy when fire DoT applies)")]
    [Tooltip("Assets/VFXPACK_FIRE_WALLCOEUR/Prefab/VFX_BlackSmoke.prefab")]
    public GameObject cinderBlackSmokePrefab;

    [Header("Enemy hit crosses")]
    [Tooltip("CFXR4 Sword Hit FIRE (Cross)")]
    public GameObject hitCrossFire;
    [Tooltip("CFXR4 Sword Hit ICE (Cross)")]
    public GameObject hitCrossIce;
    [Tooltip("CFXR4 Sword Hit PLAIN (Cross)")]
    public GameObject hitCrossPlain;

    [Header("Punch arcs (360 spiral, rotated to aim)")]
    [Tooltip("CFXR4 Sword Trail FIRE (360 Spiral)")]
    public GameObject punchTrailFire;
    [Tooltip("CFXR4 Sword Trail ICE (360 Spiral)")]
    public GameObject punchTrailIce;
    [Tooltip("CFXR4 Sword Trail PLAIN (360 Spiral)")]
    public GameObject punchTrailPlain;

    [Header("Hammer / shockwave")]
    [Tooltip("CFXR3 Hit Fire B (Air)")]
    public GameObject shockwaveFire;
    [Tooltip("CFXR3 Hit Ice B (Air)")]
    public GameObject shockwaveIce;
    [Tooltip("CFXR2 Ground Hit (non fire/ice slam)")]
    public GameObject shockwaveGroundPlain;

    [Header("Lifetime")]
    [Tooltip("Seconds before destroying spawned one-shot VFX if they have no auto-destroy.")]
    public float defaultVfxLifetime = 2.5f;

    [Header("Mushroom wider VFX")]
    public float mushroomTrailScaleMul = 1.35f;
    public float mushroomShockwaveScaleMul = 1.4f;

    [Header("Punch trail")]
    [Tooltip("Spawn point offset in the player root's local space (moves with the player; aim does not move this, only VFX rotation).")]
    public Vector3 punchTrailLocalOffset = Vector3.zero;
    [Tooltip("Added on top of the built-in −90° (clockwise) art alignment. Tune if your prefab differs.")]
    public float punchTrailRotationOffsetDegrees = 0f;

    /// <summary>Child fire on moving fire-modifier projectiles.</summary>
    public void AttachProjectileFireFx(Transform projectile)
    {
        if (projectileFireVfxPrefab == null || projectile == null)
            return;

        GameObject fx = Instantiate(projectileFireVfxPrefab, projectile);
        fx.transform.localPosition = Vector3.zero;
        fx.transform.localRotation = Quaternion.identity;
        Destroy(fx, defaultVfxLifetime + 2f);
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

    /// <param name="playerRoot">Player transform — trail stays at this position (locked), only rotation follows aim.</param>
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

        // World aim angle in the XY plane (+X = 0°). CFX spiral reads +Y as the strike; −90° is one clockwise quarter-turn to match +X aim.
        const float punchTrailClockwiseQuarterTurn = -90f;
        float aimDeg = Mathf.Atan2(aimWorld.y, aimWorld.x) * Mathf.Rad2Deg + punchTrailClockwiseQuarterTurn + punchTrailRotationOffsetDegrees;
        Quaternion worldRot = Quaternion.AngleAxis(aimDeg, Vector3.forward);
        Vector3 worldPos = parent.TransformPoint(punchTrailLocalOffset);

        GameObject go = Instantiate(prefab, worldPos, worldRot);
        go.transform.SetParent(parent, true);
        // Parent scale/rotation can leave the child with a different world rotation than we passed into Instantiate; lock it.
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
