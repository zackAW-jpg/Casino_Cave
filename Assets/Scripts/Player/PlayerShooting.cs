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

        if (Mouse.current.leftButton.wasPressedThisFrame)
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
        if (bulletPrefab == null || firePoint == null)
            return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = (Vector2)firePoint.right * bulletSpeed;
        }
    }
}
