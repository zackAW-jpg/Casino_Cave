using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDodge : MonoBehaviour
{
    [Header("Dodge")]
    public float dodgeDuration = 0.45f;
    public float dodgeSpeed = 14f;
    public float cooldown = 1.1f;
    [Range(0f, 1f)] public float iframeStartNormalized = 0.22f;
    [Range(0f, 1f)] public float iframeEndNormalized = 0.62f;

    private Rigidbody2D _rb;
    private PlayerMovement _movement;
    private PlayerHealChannel _healChannel;
    private float _dodgeStartTime;
    private float _cooldownUntil;
    private Vector2 _lockedDir;
    private bool _dodging;

    public bool IsDodging => _dodging;
    public bool IsInInvulnerableWindow { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _movement = GetComponent<PlayerMovement>();
        _healChannel = GetComponent<PlayerHealChannel>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            TryStartDodge();
    }

    private void FixedUpdate()
    {
        if (!_dodging)
            return;

        float elapsed = Time.time - _dodgeStartTime;
        if (elapsed >= dodgeDuration)
        {
            EndDodge();
            return;
        }

        float n = elapsed / dodgeDuration;
        IsInInvulnerableWindow = n >= iframeStartNormalized && n < iframeEndNormalized;
        _rb.linearVelocity = _lockedDir * dodgeSpeed;
    }

    private void TryStartDodge()
    {
        if (!GameplayInputGate.PlayerWorldActionsEnabled)
            return;
        if (_dodging)
            return;
        if (_healChannel != null && _healChannel.IsChannelingHeal)
            return;
        if (Time.time < _cooldownUntil)
            return;

        Vector2 dir = GetDodgeDirection();
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.down;

        _lockedDir = dir.normalized;
        _dodging = true;
        _dodgeStartTime = Time.time;
        IsInInvulnerableWindow = false;
    }

    private Vector2 GetDodgeDirection()
    {
        if (_movement != null)
        {
            Vector2 cur = _movement.CurrentMoveInput;
            if (cur.sqrMagnitude > 0.01f)
                return cur.normalized;
            if (_movement.LastNonZeroMoveDir.sqrMagnitude > 0.01f)
                return _movement.LastNonZeroMoveDir;
        }

        return Vector2.down;
    }

    private void EndDodge()
    {
        if (!_dodging)
            return;

        _dodging = false;
        IsInInvulnerableWindow = false;
        _cooldownUntil = Time.time + cooldown;
        _rb.linearVelocity = Vector2.zero;
    }

    
    public void CancelDodge()
    {
        if (!_dodging)
            return;

        _dodging = false;
        IsInInvulnerableWindow = false;
        _cooldownUntil = Time.time + cooldown;
        _rb.linearVelocity = Vector2.zero;
    }
}
