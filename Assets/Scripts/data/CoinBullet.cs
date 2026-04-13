using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class CoinBullet : MonoBehaviour
{
    [HideInInspector] public int damage;
    [HideInInspector] public bool isHeads;
    [HideInInspector] public Vector2 knockbackDirection;
    public float knockbackForce = 5f;

    [Header("Walls")]
    public GameObject wallBreakVfxPrefab;

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

        
        if (other.CompareTag("Player")) return;

        if (ProjectileWallBreak.IsRoomWall(other))
        {
            ProjectileWallBreak.SpawnBreakVfx(ProjectileWallBreak.ContactPoint(other, transform.position), wallBreakVfxPrefab);
            Destroy(gameObject);
            return;
        }

        BossHealth bossHealth = other.GetComponentInParent<BossHealth>();

        
        Rigidbody2D hitRb = other.attachedRigidbody;
        if (bossHealth == null && hitRb != null && hitRb.bodyType == RigidbodyType2D.Dynamic)
        {
            Vector2 dir = knockbackDirection.sqrMagnitude > 0.0001f
                ? knockbackDirection.normalized
                : ((Vector2)hitRb.transform.position - (Vector2)transform.position).normalized;

            hitRb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
            other.gameObject.SendMessage("OnKnockbackReceived", SendMessageOptions.DontRequireReceiver);
        }

        if (damage > 0)
        {
            if (bossHealth != null)
                bossHealth.TakeDamage(damage);
            else
            {
                var health = other.GetComponent<Health>();
                if (health != null)
                    health.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}
