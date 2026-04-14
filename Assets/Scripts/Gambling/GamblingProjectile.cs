using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class GamblingProjectile : MonoBehaviour
{
    public int Damage { get; private set; }
    public bool IsCrit { get; private set; }
    public GamblingModifierType Modifier { get; private set; }
    public Vector2 KnockbackDir { get; private set; }
    public float BaseKnockback { get; private set; }
    public float PaddleBonusKnockback { get; private set; }
    public Transform PlayerRoot { get; private set; }
    public GameObject GoldCoinPrefab { get; private set; }
    private GamblingArmController _arm;
    private GamblingArmVfx _vfx;

    public float lifeTime = 4f;

    [Header("Walls")]
    public GameObject wallBreakVfxPrefab;

    public void Initialize(
        int damage,
        bool isCrit,
        GamblingModifierType modifier,
        Vector2 moveDirection,
        float moveSpeed,
        float baseKnockback,
        float paddleBonus,
        Transform playerRoot,
        GameObject goldPrefab,
        float scaleMul,
        float visualScaleTuning,
        bool applyRuntimeIceProjectileFx,
        GamblingArmController arm,
        GamblingArmVfx vfx)
    {
        Damage = damage;
        IsCrit = isCrit;
        Modifier = modifier;
        Vector2 dir = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : Vector2.right;
        KnockbackDir = dir;
        BaseKnockback = baseKnockback;
        PaddleBonusKnockback = paddleBonus;
        PlayerRoot = playerRoot;
        GoldCoinPrefab = goldPrefab;
        _arm = arm;
        _vfx = vfx;

        
        transform.localScale = transform.localScale * visualScaleTuning * scaleMul;

        if (modifier == GamblingModifierType.Fire)
            _vfx?.AttachProjectileFireFx(transform);

        if (applyRuntimeIceProjectileFx && modifier == GamblingModifierType.Ice)
        {
            _vfx?.AttachProjectileIceFx(transform);
            _vfx?.ApplyProjectileIceSpriteTint(transform);
        }

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.linearVelocity = dir * moveSpeed;
        }

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;
        if (other.CompareTag("Player")) return;

        if (ProjectileWallBreak.IsRoomWall(other))
        {
            ProjectileWallBreak.SpawnBreakVfx(ProjectileWallBreak.ContactPoint(other, transform.position), wallBreakVfxPrefab);
            Destroy(gameObject);
            return;
        }

        BossHealth boss = other.GetComponentInParent<BossHealth>();
        Health health = other.GetComponentInParent<Health>();
        if (boss == null && health == null)
            return;

        Rigidbody2D hitRb = other.attachedRigidbody;
        bool applyKb = boss == null && hitRb != null && hitRb.bodyType == RigidbodyType2D.Dynamic;

        float kb = BaseKnockback;
        if (Modifier == GamblingModifierType.Paddle)
            kb += PaddleBonusKnockback;

        if (applyKb)
        {
            Vector2 dir = KnockbackDir.sqrMagnitude > 0.0001f
                ? KnockbackDir
                : ((Vector2)hitRb.transform.position - (Vector2)transform.position).normalized;
            hitRb.AddForce(dir * kb, ForceMode2D.Impulse);
            other.gameObject.SendMessage("OnKnockbackReceived", SendMessageOptions.DontRequireReceiver);
        }

        if (Damage > 0)
        {
            if (boss != null)
                boss.TakeDamage(Damage);
            else if (health != null)
                health.TakeDamage(Damage);
        }

        GameObject modRoot = boss != null ? boss.gameObject : health.gameObject;
        Vector3 hitPos = other.ClosestPoint(transform.position);

        GamblingArmHitProcessor.ApplyModifiersOnHit(
            modRoot,
            Modifier,
            health,
            boss,
            PlayerRoot,
            GoldCoinPrefab,
            _vfx);

        if (Damage > 0)
        {
            _vfx?.SpawnEnemyHitCross(hitPos, Modifier);
            _arm?.NotifyAttackHitEnemy(IsCrit);
        }

        Destroy(gameObject);
    }
}
