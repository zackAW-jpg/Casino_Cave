using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class CoinBullet : MonoBehaviour
{
    [HideInInspector] public int damage;
    [HideInInspector] public bool isHeads;
    [HideInInspector] public Vector2 knockbackDirection;
    public float knockbackForce = 5f;

    public float lifeTime = 5f;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;

        // Ignore hitting the player who fired us
        if (other.CompareTag("Player")) return;

        // Apply knockback to any rigidbody we hit
        Rigidbody2D hitRb = other.attachedRigidbody;
        if (hitRb != null)
        {
            Vector2 dir = knockbackDirection.sqrMagnitude > 0.0001f
                ? knockbackDirection.normalized
                : (hitRb.position - (Vector2)transform.position).normalized;

            hitRb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
        }

        if (damage > 0)
        {
            var health = other.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}