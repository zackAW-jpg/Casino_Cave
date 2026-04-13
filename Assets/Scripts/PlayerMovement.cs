using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private float _knockbackTimer;
    private PlayerControls controls;
    private PlayerDodge _dodge;

    /// <summary>Last facing used for dodge when there is no movement input this frame.</summary>
    public Vector2 LastNonZeroMoveDir { get; private set; } = Vector2.down;

    /// <summary>Input read this frame (after Update read).</summary>
    public Vector2 CurrentMoveInput => moveInput;

    /// <summary>Input from the previous frame (for directional sprite / diagonal rules).</summary>
    public Vector2 PreviousFrameMoveInput { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.angularVelocity = 0f;
        controls = new PlayerControls();
        _dodge = GetComponent<PlayerDodge>();
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
        PreviousFrameMoveInput = moveInput;
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        if (moveInput.sqrMagnitude > 0.01f)
            LastNonZeroMoveDir = moveInput.normalized;
    }

    void FixedUpdate()
    {
        if (_dodge != null && _dodge.IsDodging)
            return;

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
