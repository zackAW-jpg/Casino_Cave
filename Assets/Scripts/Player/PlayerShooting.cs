using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [Header("Setup")]
    public Transform weaponPivot;
    public Camera mainCamera;

    [Header("Weapons")]
    public List<Weapon> weapons = new List<Weapon>();
    public int startingWeaponIndex = 0;

    private int _currentWeaponIndex = -1;

    void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    void Start()
    {
        SelectWeapon(startingWeaponIndex);
    }

    void Update()
    {
        if (weaponPivot != null)
        {
            AimAtMouse();
        }

        HandleShootInput();
        HandleWeaponSelectionInput();
    }

    private void AimAtMouse()
    {
        if (mainCamera == null || Mouse.current == null)
        {
            return;
        }

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(mainCamera.transform.position.z - weaponPivot.position.z)));

        Vector2 direction = (mouseWorldPos - weaponPivot.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);

        if (angle > 90f || angle < -90f)
        {
            weaponPivot.localScale = new Vector3(1f, -1f, 1f);
        }
        else
        {
            weaponPivot.localScale = new Vector3(1f, 1f, 1f);
        }
    }

    private void HandleShootInput()
    {
        if (Mouse.current == null)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Weapon weapon = GetCurrentWeapon();
            if (weapon != null)
            {
                weapon.TryFire();
            }
        }
    }

    private void HandleWeaponSelectionInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            SelectWeapon(0);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            SelectWeapon(1);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            SelectWeapon(2);
        }
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            SelectWeapon(3);
        }
    }

    private void SelectWeapon(int index)
    {
        if (weapons == null || weapons.Count == 0)
        {
            _currentWeaponIndex = -1;
            return;
        }

        if (index < 0 || index >= weapons.Count)
        {
            return;
        }

        _currentWeaponIndex = index;

        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] == null) continue;
            weapons[i].gameObject.SetActive(i == _currentWeaponIndex);
        }
    }

    private Weapon GetCurrentWeapon()
    {
        if (_currentWeaponIndex < 0 || _currentWeaponIndex >= weapons.Count)
        {
            return null;
        }

        return weapons[_currentWeaponIndex];
    }
}

