using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// Slot roll → ready → left-click attack → (after attack duration) roll again.
/// Optional bottom-left <see cref="GamblingArmSlotHud"/> during Rolling; wire projectiles/VFX on this object.
/// </summary>
public class GamblingArmController : MonoBehaviour
{
    public enum Phase
    {
        Rolling,
        Ready,
        Attacking
    }

    [Header("Timing")]
    public float rollDurationSeconds = 1f;
    [Tooltip("Placeholder until real attack animations exist.")]
    public float punchAttackDuration = 0.28f;
    public float shurikenAttackDuration = 0.22f;
    public float gloveAttackDuration = 0.32f;
    public float hammerAttackDuration = 0.42f;

    [Header("Damage (before crit)")]
    public int punchDamage = 2;
    public int shurikenDamageEach = 1;
    public int gloveDamage = 2;
    public int hammerDamage = 3;

    [Header("Ranges & tuning")]
    public float punchRadius = 0.88f;
    public float punchReach = 1.2f;
    public float hammerShockwaveRadius = 1.8f;
    public float shurikenSpreadDegrees = 22f;
    public float projectileKnockback = 5f;
    [Tooltip("World speed for shuriken / glove projectiles (Rigidbody2D.linearVelocity).")]
    public float projectileSpeed = 14f;
    [Tooltip("Multiplies prefab scale so large sprites/PPU don't look huge. Set to 1 if your prefab is already sized.")]
    [Range(0.05f, 2f)] public float projectileVisualScaleTuning = 0.35f;

    [Header("Mushroom modifier")]
    public float mushroomRadiusMul = 1.55f;
    public float mushroomProjectileScaleMul = 1.45f;

    [Header("Crit")]
    [Tooltip("Multiplies base damage when left and right slot symbols match.")]
    public float critDamageMultiplier = 2f;

    [Header("Paddle modifier")]
    public float paddleKnockbackBonus = 9f;

    [Header("Prefabs — projectiles")]
    public GameObject shurikenPrefab;
    public GameObject gloveProjectilePrefab;

    [Header("Prefabs — ice variants (optional)")]
    [Tooltip("When the middle slot is Ice, this prefab is used for shurikens instead of Shuriken Prefab.")]
    public GameObject iceShurikenPrefab;
    [Tooltip("When the middle slot is Ice, this prefab is used for the glove shot instead of Glove Projectile Prefab.")]
    public GameObject iceGlovePrefab;

    [Header("Prefabs — loot")]
    [Tooltip("Dropped at enemy when Gold modifier hits.")]
    public GameObject goldCoinPrefab;

    [Header("Layers")]
    public LayerMask hitLayers = ~0;

    [Header("Roll UI")]
    [Tooltip("Optional: bottom-left slot Images — random spin then lock during Rolling. If null, only waits roll duration.")]
    [FormerlySerializedAs("rollDisplay")]
    public GamblingArmSlotHud slotHud;

    public Phase CurrentPhase { get; private set; } = Phase.Rolling;

    private GamblingArmRollResult _roll;
    private Coroutine _phaseRoutine;
    private Coroutine _deferredUnlockRoutine;
    private bool _critShakePlayedThisAttack;

    private Camera _cam;
    private Transform _firePoint;
    private GamblingArmVfx _vfx;

    private void Awake()
    {
        _cam = Camera.main;
        _vfx = GetComponent<GamblingArmVfx>();
        var shooting = GetComponent<PlayerShooting>();
        if (shooting != null && shooting.firePoint != null)
            _firePoint = shooting.firePoint;
    }

    private void OnEnable()
    {
        GamblingArmRuntimeState.SlotsUnlockedChanged += OnSlotsUnlockedChanged;
    }

    private void OnDisable()
    {
        GamblingArmRuntimeState.SlotsUnlockedChanged -= OnSlotsUnlockedChanged;
        if (_deferredUnlockRoutine != null)
        {
            StopCoroutine(_deferredUnlockRoutine);
            _deferredUnlockRoutine = null;
        }
    }

    private void Start()
    {
        ApplySlotHudActive();
        if (GamblingArmRuntimeState.SlotsUnlocked)
            BeginRollCycle();
    }

    private void OnSlotsUnlockedChanged()
    {
        if (_deferredUnlockRoutine != null)
        {
            StopCoroutine(_deferredUnlockRoutine);
            _deferredUnlockRoutine = null;
        }

        if (GamblingArmRuntimeState.SlotsUnlocked)
        {
            // Wait one frame so NPC dialogue can finish the same E-press without UI stealing input.
            _deferredUnlockRoutine = StartCoroutine(DeferredShowHudAndRoll());
        }
        else
        {
            ApplySlotHudActive();
            if (_phaseRoutine != null)
            {
                StopCoroutine(_phaseRoutine);
                _phaseRoutine = null;
            }
        }
    }

    private IEnumerator DeferredShowHudAndRoll()
    {
        yield return null;
        _deferredUnlockRoutine = null;
        if (!GamblingArmRuntimeState.SlotsUnlocked)
            yield break;

        ApplySlotHudActive();
        BeginRollCycle();
    }

    private void ApplySlotHudActive()
    {
        if (slotHud != null)
            slotHud.gameObject.SetActive(GamblingArmRuntimeState.SlotsUnlocked);
    }

    private void Update()
    {
        if (!GamblingArmRuntimeState.SlotsUnlocked)
            return;

        if (Mouse.current == null)
            return;
        if (CurrentPhase != Phase.Ready)
            return;
        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (_phaseRoutine != null)
            StopCoroutine(_phaseRoutine);
        _phaseRoutine = StartCoroutine(AttackThenRollRoutine());
    }

    private void BeginRollCycle()
    {
        if (_phaseRoutine != null)
            StopCoroutine(_phaseRoutine);
        _phaseRoutine = StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        CurrentPhase = Phase.Rolling;
        _roll = GamblingArmRollResult.RollNew();

        if (slotHud != null)
            yield return StartCoroutine(slotHud.PlayRollAnimation(_roll, rollDurationSeconds));
        else
        {
            float t = 0f;
            while (t < rollDurationSeconds)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        CurrentPhase = Phase.Ready;
        LogRollDebug();
    }

    private void LogRollDebug()
    {
#if UNITY_EDITOR
        Debug.Log($"[GamblingArm] Ready (dev: {_roll.Attack}, {_roll.Modifier}, R={_roll.CritSymbol}, crit={_roll.IsCrit})");
#endif
    }

    private IEnumerator AttackThenRollRoutine()
    {
        CurrentPhase = Phase.Attacking;
        _critShakePlayedThisAttack = false;

        float dur = GetAttackDurationPlaceholder(_roll.Attack);
        ExecuteAttack(_roll);

        yield return new WaitForSeconds(dur);
        BeginRollCycle();
    }

    private float GetAttackDurationPlaceholder(GamblingAttackType t)
    {
        switch (t)
        {
            case GamblingAttackType.Punch: return punchAttackDuration;
            case GamblingAttackType.ShurikenThrow: return shurikenAttackDuration;
            case GamblingAttackType.ExtendedPunch: return gloveAttackDuration;
            case GamblingAttackType.HammerSlam: return hammerAttackDuration;
            default: return 0.3f;
        }
    }

    /// <summary>Called by gambling projectiles when they damage an enemy. Triggers one crit screen shake.</summary>
    public void NotifyAttackHitEnemy(bool projectileWasCritRoll)
    {
        if (!projectileWasCritRoll || _critShakePlayedThisAttack)
            return;
        PlayCritCameraShake();
        _critShakePlayedThisAttack = true;
    }

    private static void PlayCritCameraShake()
    {
        if (CameraFollow2D.Instance != null)
            CameraFollow2D.Instance.PlayCritShake();
        else if (CameraShake.Instance != null)
            CameraShake.Instance.Shake();
    }

    /// <summary>Returns true if any valid target was hit (for crit shake fallback on melee).</summary>
    private bool ExecuteAttack(GamblingArmRollResult r)
    {
        Vector2 aim = GetAimDirection();
        bool mush = r.Modifier == GamblingModifierType.Mushroom;
        float rMul = mush ? mushroomRadiusMul : 1f;
        float pScale = mush ? mushroomProjectileScaleMul : 1f;

        switch (r.Attack)
        {
            case GamblingAttackType.Punch:
                return DoPunch(aim, r, rMul);
            case GamblingAttackType.ShurikenThrow:
                return DoShurikens(aim, r, pScale);
            case GamblingAttackType.ExtendedPunch:
                return DoGlove(aim, r, pScale);
            case GamblingAttackType.HammerSlam:
                return DoHammer(r, rMul);
            default:
                return false;
        }
    }

    private Vector2 GetAimDirection()
    {
        if (_cam == null || Mouse.current == null)
            return transform.right;
        Vector2 screen = Mouse.current.position.ReadValue();
        Vector3 w = _cam.ScreenToWorldPoint(screen);
        w.z = 0f;
        Vector2 d = ((Vector2)w - (Vector2)transform.position);
        return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.right;
    }

    private int CalcDamage(int baseDmg, GamblingArmRollResult r)
    {
        float mul = r.IsCrit ? critDamageMultiplier : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(baseDmg * mul));
    }

    private bool DoPunch(Vector2 aim, GamblingArmRollResult r, float radiusMul)
    {
        bool mushroomWide = r.Modifier == GamblingModifierType.Mushroom;
        _vfx?.PlayPunchTrail(aim, r.Modifier, mushroomWide, transform);

        float rad = punchRadius * radiusMul;
        Vector2 outer = (Vector2)transform.position + aim * punchReach;
        Vector2 inner = (Vector2)transform.position + aim * (punchReach * 0.48f);

        var dedup = new System.Collections.Generic.HashSet<Collider2D>();
        foreach (Collider2D c in Physics2D.OverlapCircleAll(outer, rad, hitLayers))
        {
            if (c != null) dedup.Add(c);
        }

        foreach (Collider2D c in Physics2D.OverlapCircleAll(inner, rad * 0.95f, hitLayers))
        {
            if (c != null) dedup.Add(c);
        }

        var hitList = new System.Collections.Generic.List<Collider2D>(dedup);
        return ApplyHits(hitList, r, CalcDamage(punchDamage, r), aim);
    }

    private bool DoHammer(GamblingArmRollResult r, float radiusMul)
    {
        bool mushroomWide = r.Modifier == GamblingModifierType.Mushroom;
        Vector3 center = transform.position;
        _vfx?.PlayShockwave(r.Modifier, mushroomWide, center);

        float rad = hammerShockwaveRadius * radiusMul;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, rad, hitLayers);
        return ApplyHits(new System.Collections.Generic.List<Collider2D>(hits), r, CalcDamage(hammerDamage, r), Vector2.up);
    }

    private bool ApplyHits(System.Collections.Generic.IReadOnlyList<Collider2D> hits, GamblingArmRollResult r, int damage, Vector2 kbDir)
    {
        bool any = false;
        var seen = new System.Collections.Generic.HashSet<int>();

        for (int i = 0; i < hits.Count; i++)
        {
            Collider2D c = hits[i];
            if (c == null || c.isTrigger) continue;

            if (c.GetComponentInParent<PlayerHealth>() != null)
                continue;

            BossHealth boss = c.GetComponentInParent<BossHealth>();
            Health health = c.GetComponentInParent<Health>();
            if (boss == null && health == null) continue;

            int id = boss != null ? boss.GetInstanceID() : health.GetInstanceID();
            if (!seen.Add(id)) continue;

            any = true;

            Rigidbody2D hitRb = c.attachedRigidbody;
            float kb = projectileKnockback;
            if (r.Modifier == GamblingModifierType.Paddle)
                kb += paddleKnockbackBonus;

            if (boss == null && hitRb != null && hitRb.bodyType == RigidbodyType2D.Dynamic)
            {
                Vector2 dir = kbDir.sqrMagnitude > 0.0001f ? kbDir : Vector2.right;
                hitRb.AddForce(dir * kb, ForceMode2D.Impulse);
                c.gameObject.SendMessage("OnKnockbackReceived", SendMessageOptions.DontRequireReceiver);
            }

            if (damage > 0)
            {
                if (boss != null)
                    boss.TakeDamage(damage);
                else if (health != null)
                    health.TakeDamage(damage);
            }

            GameObject modRoot = boss != null ? boss.gameObject : health.gameObject;
            GamblingArmHitProcessor.ApplyModifiersOnHit(
                modRoot,
                r.Modifier,
                health,
                boss,
                transform,
                goldCoinPrefab,
                _vfx);

            if (damage > 0)
            {
                Vector3 hitPos = c.ClosestPoint(transform.position);
                _vfx?.SpawnEnemyHitCross(hitPos, r.Modifier);
            }

            if (r.IsCrit && !_critShakePlayedThisAttack)
            {
                PlayCritCameraShake();
                _critShakePlayedThisAttack = true;
            }
        }

        return any;
    }

    private bool DoShurikens(Vector2 aim, GamblingArmRollResult r, float scaleMul)
    {
        float[] angles = { -shurikenSpreadDegrees, 0f, shurikenSpreadDegrees };
        Vector2 baseAim = aim;

        GameObject pref = ResolveProjectilePrefab(shurikenPrefab, iceShurikenPrefab, r.Modifier);
        if (pref == null)
        {
            Debug.LogWarning("GamblingArm: assign shurikenPrefab (and iceShurikenPrefab if using Ice).");
            return false;
        }

        for (int s = 0; s < 3; s++)
        {
            Vector2 dir = Rotate(baseAim, angles[s] * Mathf.Deg2Rad);
            SpawnProjectile(pref, dir, r, CalcDamage(shurikenDamageEach, r), scaleMul);
        }

        return false;
    }

    private bool DoGlove(Vector2 aim, GamblingArmRollResult r, float scaleMul)
    {
        GameObject pref = ResolveProjectilePrefab(gloveProjectilePrefab, iceGlovePrefab, r.Modifier);
        if (pref == null)
        {
            Debug.LogWarning("GamblingArm: assign gloveProjectilePrefab.");
            return false;
        }

        SpawnProjectile(pref, aim, r, CalcDamage(gloveDamage, r), scaleMul);
        return false;
    }

    private static GameObject ResolveProjectilePrefab(GameObject @default, GameObject iceOverride, GamblingModifierType mod)
    {
        if (mod == GamblingModifierType.Ice && iceOverride != null)
            return iceOverride;
        return @default;
    }

    private void SpawnProjectile(GameObject prefab, Vector2 dir, GamblingArmRollResult r, int damage, float scaleMul)
    {
        Vector3 pos = _firePoint != null ? _firePoint.position : transform.position;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.AngleAxis(ang, Vector3.forward);
        GameObject go = Instantiate(prefab, pos, rot);
        var gp = go.GetComponent<GamblingProjectile>();
        if (gp == null)
        {
            Debug.LogError("GamblingArm: prefab must have GamblingProjectile + Rigidbody2D + Collider2D (trigger).", prefab);
            Destroy(go);
            return;
        }

        float kb = projectileKnockback;
        gp.Initialize(
            damage,
            r.IsCrit,
            r.Modifier,
            dir,
            projectileSpeed,
            kb,
            paddleKnockbackBonus,
            transform,
            goldCoinPrefab,
            scaleMul,
            projectileVisualScaleTuning,
            this,
            _vfx);
    }

    private static Vector2 Rotate(Vector2 v, float radians)
    {
        float c = Mathf.Cos(radians);
        float s = Mathf.Sin(radians);
        return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
    }

    /// <summary>When present, coin gun is disabled — gambling arm owns primary fire (only after slots unlock).</summary>
    public bool BlocksCoinGun => enabled && GamblingArmRuntimeState.SlotsUnlocked;

    private void OnGUI()
    {
        if (!GamblingArmRuntimeState.SlotsUnlocked)
            return;

        if (slotHud != null)
            return;

        const float w = 420f;
        var rect = new Rect((Screen.width - w) * 0.5f, 8f, w, 72f);
        string line1 = "";
        string line2 = "";

        switch (CurrentPhase)
        {
            case Phase.Rolling:
                line1 = "…";
                break;
            case Phase.Ready:
                line1 = "Ready";
                line2 = "Click";
                break;
            case Phase.Attacking:
                line1 = "…";
                break;
        }

        GUI.Box(rect, GUIContent.none);
        GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 22f), line1, GetLabelStyle());
        if (!string.IsNullOrEmpty(line2))
            GUI.Label(new Rect(rect.x + 6f, rect.y + 28f, rect.width - 12f, 44f), line2, GetLabelStyle());
    }

    private static GUIStyle _labelStyle;

    private static GUIStyle GetLabelStyle()
    {
        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
        }

        return _labelStyle;
    }
}
