using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class CoinBullet : MonoBehaviour
{
    [HideInInspector] public int damage;
    [HideInInspector] public bool isHeads;

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