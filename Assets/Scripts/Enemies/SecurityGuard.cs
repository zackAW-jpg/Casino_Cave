using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public class SecurityGuard : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float attackRange = 1f;
    public float attackCooldown = 0.7f;
    public int attackDamage = 1;
    [Tooltip("Knockback force applied to the player when this guard hits them.")]
    public float playerKnockbackForce = 4f;

    [Header("Room aggro")]
    [Tooltip("When the player is not in this room, the guard stays dormant (no movement).")]
    public bool dormantWhenPlayerOutsideRoom = true;
    [Tooltip("After the player enters this room, guards wait this long before they can move or attack.")]
    public float engageDelayAfterRoomEnter = 0.2f;

    [Header("Attack animation")]
    [Tooltip("Optional: assign an Animator Controller with a trigger named like Punch Attack.")]
    public Animator animator;
    [Tooltip("Animator trigger fired on each successful melee hit.")]
    public string punchAttackTrigger = "Punch";
    [Tooltip("Brief highlight if there is no Animator or no controller assigned.")]
    public bool punchFlashIfNoAnimator = true;

    [Header("Attack VFX")]
    [Tooltip("Spawned when a melee hit lands on the player (after dodge check).")]
    public GameObject attackVfxPrefab;

    [Tooltip("If set, VFX spawns at this transform. Otherwise spawn offset from the guard toward the player.")]
    public Transform attackVfxSpawnPoint;

    [Tooltip("World-units from guard toward player when no spawn point is set.")]
    public float attackVfxForwardDistance = 0.35f;

    [Header("Set by DungeonRoomSpawner at runtime")]
    public Transform target;
    public Vector2Int roomCoord;
    public DungeonStateSO dungeonState;

    /// <summary>Normalized chase direction this frame when moving toward the player; zero when idle/dormant/attacking.</summary>
    public Vector2 IntendedMoveDirection { get; private set; }

    private Rigidbody2D _rb;
    private float _cooldownTimer;
    private float _knockbackTimer;
    private float _roomEngageTimer;
    private bool _wasPlayerInRoom;
    private Coroutine _punchFlashCo;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Dynamic;
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Called by CoinBullet via SendMessage when this guard is hit, so we don't overwrite velocity for a moment.
    /// </summary>
    public void OnKnockbackReceived()
    {
        _knockbackTimer = 0.15f;
    }

    private void Update()
    {
        if (target == null) return;

        if (_knockbackTimer > 0f)
        {
            _knockbackTimer -= Time.deltaTime;
            IntendedMoveDirection = Vector2.zero;
            return;
        }

        bool playerInRoom = dungeonState == null || dungeonState.currentRoomCoord == roomCoord;

        if (dormantWhenPlayerOutsideRoom && !playerInRoom)
        {
            _wasPlayerInRoom = false;
            _roomEngageTimer = engageDelayAfterRoomEnter;
            IntendedMoveDirection = Vector2.zero;
            HoldDormant();
            return;
        }

        if (!_wasPlayerInRoom)
        {
            _wasPlayerInRoom = true;
            _roomEngageTimer = engageDelayAfterRoomEnter;
        }

        if (_roomEngageTimer > 0f)
        {
            _roomEngageTimer -= Time.deltaTime;
            IntendedMoveDirection = Vector2.zero;
            HoldDormant();
            return;
        }

        Vector2 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;

        float speedMul = 1f;
        if (TryGetComponent(out StatusEffectHost status))
            speedMul = status.MoveSpeedMultiplier;

        if (distance > attackRange)
        {
            Vector2 dir = toTarget.normalized;
            IntendedMoveDirection = dir;
            _rb.linearVelocity = dir * (moveSpeed * speedMul);
        }
        else
        {
            IntendedMoveDirection = Vector2.zero;
            _rb.linearVelocity = Vector2.zero;

            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0f)
            {
                TryAttackPlayer();
                _cooldownTimer = attackCooldown;
            }
        }
    }

    private void HoldDormant()
    {
        IntendedMoveDirection = Vector2.zero;
        _rb.linearVelocity = Vector2.zero;
    }

    private void TryAttackPlayer()
    {
        if (target == null) return;

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > attackRange + 0.1f) return;

        PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            return;

        PlayerDodge dodge = target.GetComponent<PlayerDodge>();
        if (dodge != null && dodge.IsInInvulnerableWindow)
            return;

        playerHealth.TakeDamage(attackDamage);
        SpawnAttackVfx();
        PlayPunchAttackAnimation();
        ApplyKnockbackToPlayer();
    }

    private void SpawnAttackVfx()
    {
        if (attackVfxPrefab == null)
            return;

        Vector3 pos;
        if (attackVfxSpawnPoint != null)
            pos = attackVfxSpawnPoint.position;
        else if (target != null)
        {
            Vector2 flat = (Vector2)target.position - (Vector2)transform.position;
            if (flat.sqrMagnitude > 0.0001f)
                pos = transform.position + (Vector3)(flat.normalized * attackVfxForwardDistance);
            else
                pos = transform.position;
        }
        else
            pos = transform.position;

        Instantiate(attackVfxPrefab, pos, Quaternion.identity);
    }

    private void PlayPunchAttackAnimation()
    {
        if (animator != null && animator.runtimeAnimatorController != null && !string.IsNullOrEmpty(punchAttackTrigger))
            animator.SetTrigger(punchAttackTrigger);
        else if (punchFlashIfNoAnimator && TryGetComponent(out SpriteRenderer sr))
        {
            if (_punchFlashCo != null)
                StopCoroutine(_punchFlashCo);
            _punchFlashCo = StartCoroutine(PunchFlashRoutine(sr));
        }
    }

    private IEnumerator PunchFlashRoutine(SpriteRenderer sr)
    {
        Color baseCol = sr.color;
        Color flash = baseCol;
        flash.r = Mathf.Min(1f, baseCol.r + 0.35f);
        flash.g = Mathf.Min(1f, baseCol.g + 0.35f);
        flash.b = Mathf.Min(1f, baseCol.b + 0.35f);
        const float dur = 0.1f;
        float e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float t = Mathf.Sin((e / dur) * Mathf.PI);
            sr.color = Color.Lerp(baseCol, flash, t);
            yield return null;
        }

        sr.color = baseCol;
        _punchFlashCo = null;
    }

    private void ApplyKnockbackToPlayer()
    {
        if (playerKnockbackForce <= 0f) return;

        Rigidbody2D playerRb = target.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        Vector2 away = ((Vector2)target.position - (Vector2)transform.position).normalized;
        playerRb.AddForce(away * playerKnockbackForce, ForceMode2D.Impulse);
    }
}
