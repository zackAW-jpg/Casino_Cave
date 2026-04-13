using System.Collections;
using UnityEngine;

/// <summary>
/// Boss AI with two telegraphed moves:
/// 1) Ground slam: short jump telegraph, then slam hit on landing.
/// 2) Charge: lock player position, wind up, then dash to that locked point.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    [Header("Targets")]
    public Transform playerTransform;

    [Header("Refs")]
    public BossHealth bossHealth;

    [Header("Room gating (set by DungeonRoomSpawner)")]
    [Tooltip("Same asset as on the spawner; used like SecurityGuard to know when the player is in this room.")]
    public DungeonStateSO dungeonState;
    public Vector2Int roomCoord;
    [Tooltip("After the player enters the boss room, wait this long before movement and attacks.")]
    public float roomEngageDelaySeconds = 0.2f;
    [Tooltip("HUD binds when the player enters this boss room; unbinds when they leave or the boss dies.")]
    public BossHUD bossHUD;

    [Header("Movement")]
    public float moveSpeed = 2.2f;
    [Tooltip("Stop approaching when within this distance of the player.")]
    public float stopDistanceFromPlayer = 1.2f;

    [Header("Ground Slam")]
    public float slamTriggerDistance = 4f;
    [Tooltip("Time spent in the jump telegraph before landing.")]
    public float slamJumpTelegraph = 0.55f;
    public float slamCooldown = 4.2f;
    public float shockwaveRadius = 2.75f;
    public int slamDamage = 5;
    public float slamKnockbackForce = 10f;
    [Tooltip("Layer mask for slam hit test (default: everything).")]
    public LayerMask slamHitMask = ~0;
    [Header("Ground Slam VFX")]
    public GameObject slamTakeoffVfxPrefab;
    public GameObject slamLandVfxPrefab;
    [Tooltip("Optional spawn point for slam VFX. Uses boss position if null.")]
    public Transform slamVfxSpawnPoint;

    [Header("Charge")]
    [Tooltip("How long the boss pauses while charging up.")]
    public float chargeWindup = 0.45f;
    public float chargeSpeed = 11f;
    public float chargeStopDistance = 0.12f;
    public float chargeCooldown = 3.6f;
    [Tooltip("After locking player position, continue this many units beyond it.")]
    public float chargeOvershootDistance = 5f;
    public int chargeDamage = 4;
    [Tooltip("Hit radius sampled while dashing.")]
    public float chargeHitRadius = 0.7f;
    [Tooltip("Layer mask for charge hit test (default: everything).")]
    public LayerMask chargeHitMask = ~0;
    [Tooltip("Layers to query while charging. Wall stopping uses RoomWall component checks.")]
    public LayerMask chargeWallMask = ~0;

    [Header("Room bounds")]
    [Tooltip("Shrinks the walkable box further inward so the sprite/body does not hang over wall visuals.")]
    public float roomBoundsExtraInset = 0.35f;

    [Header("Attack cadence")]
    [Tooltip("Minimum time gap between any two attacks.")]
    public float minTimeBetweenAttacks = 1.5f;

    [Header("Optional sound cues")]
    public AudioSource audioSource;
    public AudioClip slamTakeoffSfx;
    public AudioClip slamLandSfx;
    public AudioClip chargeWindupSfx;
    public AudioClip chargeStartSfx;
    public AudioClip chargeWallCancelSfx;

    [Header("Optional animation hooks")]
    public Animator animator;
    public string slamJumpTrigger = "SlamJump";
    public string slamLandTrigger = "SlamLand";
    public string chargeWindupTrigger = "ChargeWindup";
    public string chargeStartTrigger = "ChargeStart";
    [Tooltip("Fallback squash/stretch when no animator trigger is wired.")]
    public bool useFallbackScaleAnimation = true;
    [Tooltip("First telegraph pose (bigger) before jump.")]
    public Vector3 slamJumpScale = new Vector3(1.12f, 1.12f, 1f);
    [Tooltip("Second telegraph pose (smaller) to sell airborne/jump feel.")]
    public Vector3 slamAirScale = new Vector3(0.9f, 0.9f, 1f);
    public Vector3 chargeWindupScale = new Vector3(1.15f, 0.9f, 1f);

    private Rigidbody2D _rb;
    private float _nextSlamTime;
    private float _nextChargeTime;
    private float _nextAnyAttackTime;
    private bool _isAttacking;
    private bool _hudBound;
    private float _roomEngageTimer;
    private bool _wasPlayerInBossRoom;
    private Vector3 _baseScale;
    private Collider2D[] _selfColliders;
    private float _wallProbeRadius = 0.6f;
    private Collider2D _primaryCollider;
    private readonly RaycastHit2D[] _chargeCastHits = new RaycastHit2D[16];
    private readonly Collider2D[] _overlapHits = new Collider2D[16];
    private bool _hasRoomBounds;
    private Bounds _roomBoundsWorld;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        // Critical: kinematic bodies must use full contacts to be blocked by static wall colliders.
        _rb.useFullKinematicContacts = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _baseScale = transform.localScale;
        _selfColliders = GetComponents<Collider2D>();
        _primaryCollider = GetComponent<Collider2D>();
        _wallProbeRadius = ComputeWallProbeRadius();
        RefreshRoomBounds();

        if (bossHealth == null)
            bossHealth = GetComponent<BossHealth>();
        if (bossHealth == null)
            bossHealth = GetComponentInChildren<BossHealth>(true);

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        _nextSlamTime = Time.time + 1f;
        _nextChargeTime = Time.time + 1.75f;
        _nextAnyAttackTime = Time.time + 1f;
    }

    private bool IsPlayerInBossRoom()
    {
        return dungeonState != null && dungeonState.currentRoomCoord == roomCoord;
    }

    private void UpdateBossHudBinding()
    {
        bool inRoom = IsPlayerInBossRoom();

        if (inRoom)
        {
            if (!_hudBound && bossHUD != null && bossHealth != null)
            {
                bossHUD.Bind(bossHealth);
                _hudBound = true;
            }
        }
        else
        {
            if (_hudBound && bossHUD != null)
            {
                bossHUD.Unbind();
                _hudBound = false;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!IsPlayerInBossRoom() || playerTransform == null || bossHealth == null || _isAttacking)
            return;

        if (_roomEngageTimer > 0f)
            return;

        Vector2 toPlayer = (Vector2)playerTransform.position - _rb.position;
        float d = toPlayer.magnitude;
        if (d <= stopDistanceFromPlayer || d < 0.001f)
            return;

        Vector2 dir = toPlayer / d;
        float spd = moveSpeed;
        if (TryGetComponent(out StatusEffectHost status))
            spd *= status.MoveSpeedMultiplier;
        Vector2 target = _rb.position + dir * (spd * Time.fixedDeltaTime);
        _rb.MovePosition(ClampToRoomBounds(target));
    }

    private void Update()
    {
        UpdateBossHudBinding();
        if (!_hasRoomBounds)
            RefreshRoomBounds();

        if (!IsPlayerInBossRoom())
        {
            _wasPlayerInBossRoom = false;
            _roomEngageTimer = roomEngageDelaySeconds;
            return;
        }

        if (!_wasPlayerInBossRoom)
        {
            _wasPlayerInBossRoom = true;
            _roomEngageTimer = roomEngageDelaySeconds;
        }

        if (playerTransform == null || bossHealth == null || _isAttacking)
            return;

        if (_roomEngageTimer > 0f)
        {
            _roomEngageTimer -= Time.deltaTime;
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        bool cadenceReady = Time.time >= _nextAnyAttackTime;
        bool playerNearby = dist <= slamTriggerDistance;
        bool canSlam = playerNearby && Time.time >= _nextSlamTime && cadenceReady;
        bool canCharge = Time.time >= _nextChargeTime && cadenceReady;

        // Default to charge; if nearby, slam takes priority.
        if (canSlam)
        {
            StartCoroutine(SlamRoutine());
            return;
        }

        if (canCharge)
        {
            StartCoroutine(ChargeRoutine());
        }
    }

    private IEnumerator SlamRoutine()
    {
        _isAttacking = true;
        _nextSlamTime = Time.time + slamCooldown;
        _nextAnyAttackTime = Time.time + minTimeBetweenAttacks;

        TriggerAnim(slamJumpTrigger);
        SpawnSlamVfx(slamTakeoffVfxPrefab);
        PlayCue(slamTakeoffSfx);
        yield return SlamTelegraphJumpRoutine();

        if (!CanContinueAttack())
            yield break;

        transform.localScale = _baseScale;
        TriggerAnim(slamLandTrigger);
        SpawnSlamVfx(slamLandVfxPrefab);
        PlayCue(slamLandSfx);

        Vector2 center = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, shockwaveRadius, slamHitMask);
        bool hitPlayer = false;
        Vector2 firstHitPos = Vector2.zero;
        Rigidbody2D firstHitRb = null;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i];
            if (c == null || c.isTrigger) continue;
            if (!c.CompareTag("Player")) continue;

            PlayerDodge pd = c.GetComponentInParent<PlayerDodge>();
            if (pd != null && pd.IsInInvulnerableWindow)
                continue;

            PlayerHealth ph = c.GetComponentInParent<PlayerHealth>();
            if (ph == null)
                continue;

            ph.TakeDamage(slamDamage);
            hitPlayer = true;

            if (firstHitRb == null && c.attachedRigidbody != null)
                firstHitRb = c.attachedRigidbody;
            if (firstHitPos == Vector2.zero)
                firstHitPos = c.bounds.center;
            break; // Single hit for this slam.
        }

        if (hitPlayer && firstHitRb != null)
        {
            Vector2 away = (firstHitPos - center).normalized;
            if (away.sqrMagnitude < 0.01f) away = Vector2.right;
            firstHitRb.AddForce(away * slamKnockbackForce, ForceMode2D.Impulse);
            firstHitRb.gameObject.SendMessage("OnKnockbackReceived", SendMessageOptions.DontRequireReceiver);
        }

        _isAttacking = false;
    }

    private IEnumerator ChargeRoutine()
    {
        _isAttacking = true;
        _nextChargeTime = Time.time + chargeCooldown;
        _nextAnyAttackTime = Time.time + minTimeBetweenAttacks;

        Vector2 startPos = _rb.position;
        Vector2 lockedPlayerPos = playerTransform.position;
        Vector2 chargeDir = (lockedPlayerPos - startPos).normalized;
        if (chargeDir.sqrMagnitude < 0.0001f)
            chargeDir = Vector2.right;
        Vector2 lockedTarget = lockedPlayerPos + chargeDir * chargeOvershootDistance;

        TriggerAnimOrFallback(chargeWindupTrigger, chargeWindupScale);
        PlayCue(chargeWindupSfx);
        yield return new WaitForSeconds(chargeWindup);

        if (!CanContinueAttack())
            yield break;

        TriggerAnim(chargeStartTrigger);
        PlayCue(chargeStartSfx);

        bool damaged = false;
        while (CanContinueAttack())
        {
            Vector2 current = _rb.position;
            Vector2 toTarget = lockedTarget - current;
            float distance = toTarget.magnitude;
            if (distance <= chargeStopDistance)
                break;

            Vector2 step = toTarget.normalized * (chargeSpeed * Time.fixedDeltaTime);
            if (step.magnitude > distance)
                step = toTarget;

            Vector2 dir = step.normalized;
            float requestedDistance = step.magnitude;
            bool hitWall = TryGetChargeStepFromSweep(dir, requestedDistance, out float allowedDistance);
            if (allowedDistance > 0.000001f)
            {
                Vector2 unclampedNext = current + dir * allowedDistance;
                Vector2 next = ClampToRoomBounds(unclampedNext);
                if ((next - unclampedNext).sqrMagnitude > 0.000001f)
                    hitWall = true;

                _rb.MovePosition(next);
                if (!damaged)
                    damaged = TryDamagePlayerInChargeLine(next);
            }

            if (hitWall)
            {
                PlayCue(chargeWallCancelSfx);
                break;
            }

            yield return new WaitForFixedUpdate();
        }

        _isAttacking = false;
    }

    private bool TryDamagePlayerInChargeLine(Vector2 samplePos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(samplePos, chargeHitRadius, chargeHitMask);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i];
            if (c == null || c.isTrigger || !c.CompareTag("Player"))
                continue;

            PlayerDodge pd = c.GetComponentInParent<PlayerDodge>();
            if (pd != null && pd.IsInInvulnerableWindow)
                continue;

            PlayerHealth ph = c.GetComponentInParent<PlayerHealth>();
            if (ph == null)
                continue;

            ph.TakeDamage(chargeDamage);
            return true;
        }

        return false;
    }

    private bool CanContinueAttack()
    {
        if (!IsPlayerInBossRoom() || playerTransform == null || bossHealth == null)
        {
            _isAttacking = false;
            return false;
        }

        return true;
    }

    private void TriggerAnim(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            return;
        animator.SetTrigger(triggerName);
    }

    private void TriggerAnimOrFallback(string triggerName, Vector3 fallbackScale)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(triggerName))
        {
            animator.SetTrigger(triggerName);
            return;
        }

        if (useFallbackScaleAnimation)
            StartCoroutine(ScalePulseRoutine(fallbackScale, 0.1f));
    }

    private IEnumerator ScalePulseRoutine(Vector3 pulseScale, float halfDuration)
    {
        float t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / halfDuration);
            transform.localScale = Vector3.Lerp(_baseScale, pulseScale, a);
            yield return null;
        }

        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / halfDuration);
            transform.localScale = Vector3.Lerp(pulseScale, _baseScale, a);
            yield return null;
        }

        transform.localScale = _baseScale;
    }

    private IEnumerator SlamTelegraphJumpRoutine()
    {
        if (!useFallbackScaleAnimation || slamJumpTelegraph <= 0f)
        {
            yield return new WaitForSeconds(slamJumpTelegraph);
            yield break;
        }

        float total = slamJumpTelegraph;
        float growTime = total * 0.4f;
        float shrinkTime = Mathf.Max(0.001f, total - growTime);

        float t = 0f;
        while (t < growTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / growTime);
            transform.localScale = Vector3.Lerp(_baseScale, slamJumpScale, a);
            yield return null;
        }

        t = 0f;
        while (t < shrinkTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / shrinkTime);
            transform.localScale = Vector3.Lerp(slamJumpScale, slamAirScale, a);
            yield return null;
        }
    }

    private void SpawnSlamVfx(GameObject prefab)
    {
        if (prefab == null)
            return;
        Vector3 pos = slamVfxSpawnPoint != null ? slamVfxSpawnPoint.position : transform.position;
        Instantiate(prefab, pos, Quaternion.identity);
    }

    private void PlayCue(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;
        audioSource.PlayOneShot(clip);
    }

    private bool TryGetChargeStepFromSweep(Vector2 direction, float requestedDistance, out float allowedDistance)
    {
        allowedDistance = requestedDistance;
        if (requestedDistance <= 0.000001f)
            return false;

        if (_primaryCollider == null)
            _primaryCollider = GetComponent<Collider2D>();
        if (_primaryCollider == null)
            return true;

        ContactFilter2D castFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = chargeWallMask,
            useTriggers = false
        };

        int hitCount = _primaryCollider.Cast(direction, castFilter, _chargeCastHits, requestedDistance + 0.02f);

        float nearest = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = _chargeCastHits[i].collider;
            if (col == null || col.isTrigger)
                continue;
            if (!IsBlockingChargeWall(col))
                continue;

            float d = _chargeCastHits[i].distance;
            if (d < nearest)
                nearest = d;
        }

        if (nearest != float.MaxValue)
        {
            allowedDistance = Mathf.Max(0f, nearest - 0.06f);
            return true;
        }

        // Fallback guard: if already overlapping a room wall, don't advance.
        ContactFilter2D overlapFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = chargeWallMask,
            useTriggers = false
        };

        Vector2 prevPos = _rb.position;
        _rb.position = prevPos + direction * requestedDistance;
        int overlapCount = _primaryCollider.Overlap(overlapFilter, _overlapHits);
        _rb.position = prevPos;

        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D col = _overlapHits[i];
            if (!IsBlockingChargeWall(col))
                continue;

            allowedDistance = 0f;
            return true;
        }

        return false;
    }

    private Vector2 ClampToRoomBounds(Vector2 position)
    {
        if (!_hasRoomBounds)
            return position;

        float inset = _wallProbeRadius + roomBoundsExtraInset;
        return RoomInteriorBounds.ClampXY(position, _roomBoundsWorld, inset, inset);
    }

    private void RefreshRoomBounds()
    {
        _hasRoomBounds = false;

        Transform roomRoot = transform.parent;
        if (roomRoot == null)
            return;

        if (!RoomInteriorBounds.TryCompute(roomRoot, out Bounds room))
            return;

        _roomBoundsWorld = room;
        _hasRoomBounds = true;
    }

    private bool IsBlockingChargeWall(Collider2D col)
    {
        if (col == null || col.isTrigger)
            return false;
        if (col.attachedRigidbody == _rb || col.transform.root == transform.root)
            return false;
        return col.GetComponentInParent<RoomWall>() != null;
    }

    private float ComputeWallProbeRadius()
    {
        if (_selfColliders == null || _selfColliders.Length == 0)
            return Mathf.Max(0.2f, chargeHitRadius);

        float r = 0.2f;
        for (int i = 0; i < _selfColliders.Length; i++)
        {
            Collider2D c = _selfColliders[i];
            if (c == null || !c.enabled || c.isTrigger)
                continue;
            Bounds b = c.bounds;
            float cr = Mathf.Max(b.extents.x, b.extents.y);
            if (cr > r)
                r = cr;
        }

        return r;
    }
}
