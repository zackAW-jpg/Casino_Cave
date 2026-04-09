using UnityEngine;

/// <summary>
/// Slow projectile fired by the boss. Damages the player; ignores other layers by tag.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BossBullet : MonoBehaviour
{
    public int damage = 2;
    public float lifeTime = 8f;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;
        if (other.GetComponentInParent<BossHealth>() != null) return;
        if (!other.CompareTag("Player")) return;

        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null)
            ph.TakeDamage(damage);

        Destroy(gameObject);
    }
}
