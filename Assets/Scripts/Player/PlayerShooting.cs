using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [Header("Setup")]
    public Transform firePoint;         // where bullets come from
    public GameObject bulletPrefab;     // prefab with Rigidbody2D
    public float bulletSpeed = 15f;

    private Camera _cam;

    void Awake()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        if (_cam == null || Mouse.current == null)
            return;

        AimAtMouse();

        bool shootPressed = Mouse.current.leftButton.wasPressedThisFrame;

        if (shootPressed)
        {
            Shoot();
        }
    }

    void AimAtMouse()
    {
        if (firePoint == null)
            return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = _cam.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = firePoint.position.z;

        Vector2 dir = (mouseWorldPos - firePoint.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        firePoint.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void Shoot()
    {
        if (bulletPrefab == null)
            return;

        Vector3 spawnPos = (firePoint != null && firePoint.IsChildOf(transform))
            ? firePoint.position
            : transform.position;
        Quaternion spawnRot = firePoint != null ? firePoint.rotation : GetAimRotation();
        Vector2 aimDir = (firePoint != null ? firePoint.right : (Vector3)GetAimDirection()).normalized;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, spawnRot);
        Destroy(bullet, 5f);

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = aimDir * bulletSpeed;
        }
    }

    Vector2 GetAimDirection()
    {
        if (_cam == null) return Vector2.right;
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = _cam.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = transform.position.z;
        return (mouseWorldPos - transform.position).normalized;
    }

    Quaternion GetAimRotation()
    {
        Vector2 dir = GetAimDirection();
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        return Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
