using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public class SecurityGuard : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float attackRange = 1f;
    public float attackCooldown = 0.7f;
    public int attackDamage = 1;

    public Transform target; // player

    private Rigidbody2D _rb;
    private float _cooldownTimer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (target == null) return;

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

    private void TryAttackPlayer()
    {
        if (target == null) return;

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > attackRange + 0.1f) return;

        PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
    }
}