using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private float _knockbackTimer;
    private PlayerControls controls;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new PlayerControls();
    }

    void OnEnable()
    {
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Disable();
    }

    /// <summary>
    /// Called by PlayerHealth via SendMessage when the player takes damage, so we don't overwrite velocity for a moment.
    /// </summary>
    public void OnKnockbackReceived()
    {
        _knockbackTimer = 0.2f;
    }

    void Update()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        if (_knockbackTimer > 0f)
        {
            _knockbackTimer -= Time.fixedDeltaTime;
            return;
        }

        if (moveInput.sqrMagnitude > 0.01f)
            rb.linearVelocity = moveInput.normalized * moveSpeed;
        else
            rb.linearVelocity = Vector2.zero;
    }
}
