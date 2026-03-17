using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public class SecurityGuard : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float attackRange = 1f;
    public float attackCooldown = 0.7f;
    public int attackDamage = 1;
    [Tooltip("Knockback force applied to the player when this guard hits them.")]
    public float playerKnockbackForce = 4f;

    [Header("Set by DungeonRoomSpawner at runtime")]
    public Transform target;
    public Vector2Int roomCoord;
    public DungeonStateSO dungeonState;

    private Rigidbody2D _rb;
    private float _cooldownTimer;
    private float _knockbackTimer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Dynamic;
    }

    /// <summary>
    /// Called by CoinBullet via SendMessage when this guard is hit, so we don't overwrite velocity for a moment.
    /// </summary>
    public void OnKnockbackReceived()
    {
        _knockbackTimer = 0.15f;
    }

    private void Update()
    {
        if (target == null) return;

        // Let knockback velocity show; don't overwrite it until timer expires
        if (_knockbackTimer > 0f)
        {
            _knockbackTimer -= Time.deltaTime;
            return;
        }

        // Only chase/attack when the player is in this room
        if (dungeonState != null && dungeonState.currentRoomCoord != roomCoord)
        {
            ReturnToRoomCenter();
            return;
        }

        Vector2 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;

        if (distance > attackRange)
        {
            Vector2 dir = toTarget.normalized;
            _rb.linearVelocity = dir * moveSpeed;
        }
        else
        {
            _rb.linearVelocity = Vector2.zero;

            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0f)
            {
                TryAttackPlayer();
                _cooldownTimer = attackCooldown;
            }
        }
    }

    private void ReturnToRoomCenter()
    {
        if (transform.parent == null)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 center = transform.parent.position;
        Vector2 toCenter = center - (Vector2)transform.position;
        float distSq = toCenter.sqrMagnitude;

        if (distSq < 0.01f)
        {
            _rb.linearVelocity = Vector2.zero;
        }
        else
        {
            _rb.linearVelocity = toCenter.normalized * moveSpeed;
        }
    }

    private void TryAttackPlayer()
    {
        if (target == null) return;

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > attackRange + 0.1f) return;

        PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
            ApplyKnockbackToPlayer();
        }
    }

    private void ApplyKnockbackToPlayer()
    {
        if (playerKnockbackForce <= 0f) return;

        Rigidbody2D playerRb = target.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        Vector2 away = ((Vector2)target.position - (Vector2)transform.position).normalized;
        playerRb.AddForce(away * playerKnockbackForce, ForceMode2D.Impulse);
    }
}