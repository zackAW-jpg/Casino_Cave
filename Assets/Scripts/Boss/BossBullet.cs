using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BossBullet : MonoBehaviour
{
    public int damage = 2;
    public float lifeTime = 8f;

    [Header("Walls")]
    public GameObject wallBreakVfxPrefab;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;

        if (ProjectileWallBreak.IsRoomWall(other))
        {
            ProjectileWallBreak.SpawnBreakVfx(ProjectileWallBreak.ContactPoint(other, transform.position), wallBreakVfxPrefab);
            Destroy(gameObject);
            return;
        }

        if (other.GetComponentInParent<BossHealth>() != null) return;
        if (!other.CompareTag("Player")) return;

        PlayerDodge dodge = other.GetComponentInParent<PlayerDodge>();
        if (dodge != null && dodge.IsInInvulnerableWindow)
        {
            Collider2D bulletCol = GetComponent<Collider2D>();
            if (bulletCol != null)
                Physics2D.IgnoreCollision(bulletCol, other, true);
            return;
        }

        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null)
            ph.TakeDamage(damage);

        Destroy(gameObject);
    }
}
