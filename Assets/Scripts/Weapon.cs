using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Projectile")]
    public Projectile projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 15f;

    [Header("Firing")]
    public float fireRate = 5f;

    private float _cooldown;

    void Update()
    {
        if (_cooldown > 0f)
        {
            _cooldown -= Time.deltaTime;
        }
    }

    public void TryFire()
    {
        if (_cooldown > 0f)
        {
            return;
        }

        Fire();
        _cooldown = fireRate > 0f ? 1f / fireRate : 0f;
    }

    private void Fire()
    {
        if (projectilePrefab == null || firePoint == null)
        {
            return;
        }

        Projectile proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        proj.Launch(firePoint.right, projectileSpeed);
    }
}

