using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [Header("Setup")]
    public Transform firePoint;

    private Camera _cam;

    void Awake()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        if (_cam == null || Mouse.current == null)
            return;

        if (!GameplayInputGate.PlayerWorldActionsEnabled)
            return;

        AimAtMouse();
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
}
