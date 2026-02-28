using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float lifeTime = 3f;

    private Vector2 _velocity;

    void Start()
    {
        if (lifeTime > 0f)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    void Update()
    {
        transform.position += (Vector3)_velocity * Time.deltaTime;
    }

    public void Launch(Vector2 direction, float speed)
    {
        _velocity = direction.normalized * speed;
    }
}

