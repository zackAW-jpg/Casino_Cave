using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss AI: slow aimed shots (2 HP) and proximity ground slam (5 HP + knockback).
/// Use a Kinematic Rigidbody2D on the boss so player bullets do not apply knockback.
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
    [Tooltip("HUD binds when the player enters this boss room; unbinds when they leave or the boss dies.")]
    public BossHUD bossHUD;

    [Header("Movement")]
    public float moveSpeed = 2.2f;
    [Tooltip("Stop approaching when within this distance of the player.")]
    public float stopDistanceFromPlayer = 1.2f;

    [Header("Attack 1 — bullets")]
    public GameObject bossBulletPrefab;
    [Tooltip("Child transform for bullet spawn direction; if null, uses boss position + up as fallback.")]
    public Transform firePoint;
    public float shootInterval = 2.8f;
    public float bulletSpeed = 3.5f;

    [Header("Attack 2 — slam shockwave")]
    public float slamProximity = 4f;
    public float slamWindup = 1.1f;
    public float slamCooldown = 4.5f;
    public float shockwaveRadius = 2.75f;
    public int slamDamage = 5;
    public float slamKnockbackForce = 10f;
    [Tooltip("Layer mask for slam hit test (default: everything).")]
    public LayerMask slamHitMask = ~0;

    private Rigidbody2D _rb;
    private float _nextShootTime;
    private float _nextSlamTime;
    private bool _slamming;
    private bool _hudBound;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;

        if (bossHealth == null)
            bossHealth = GetComponent<BossHealth>();
        if (bossHealth == null)
            bossHealth = GetComponentInChildren<BossHealth>(true);

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        _nextShootTime = Time.time + 1f;
        _nextSlamTime = Time.time + slamCooldown;
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
        if (!IsPlayerInBossRoom() || playerTransform == null || bossHealth == null || _slamming)
            return;

        Vector2 toPlayer = (Vector2)playerTransform.position - _rb.position;
        float d = toPlayer.magnitude;
        if (d <= stopDistanceFromPlayer || d < 0.001f)
            return;

        Vector2 dir = toPlayer / d;
        _rb.MovePosition(_rb.position + dir * (moveSpeed * Time.fixedDeltaTime));
    }

    private void Update()
    {
        UpdateBossHudBinding();

        if (!IsPlayerInBossRoom() || playerTransform == null || bossHealth == null || _slamming)
            return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        if (dist <= slamProximity && Time.time >= _nextSlamTime)
        {
            StartCoroutine(SlamRoutine());
            return;
        }

        if (Time.time >= _nextShootTime)
        {
            ShootAtPlayer();
            _nextShootTime = Time.time + shootInterval;
        }
    }

    private void ShootAtPlayer()
    {
        if (bossBulletPrefab == null) return;

        Vector2 basePos = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 dir = ((Vector2)playerTransform.position - basePos).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;

        // Spawn slightly ahead so the bullet doesn't immediately overlap the boss collider
        Vector3 origin = basePos + dir * 0.85f;

        GameObject bullet = Instantiate(bossBulletPrefab, origin, Quaternion.identity);
        Rigidbody2D brb = bullet.GetComponent<Rigidbody2D>();
        if (brb != null)
            brb.linearVelocity = dir * bulletSpeed;
        else
            Debug.LogWarning("BossBullet prefab should have Rigidbody2D.", bullet);
    }

    private IEnumerator SlamRoutine()
    {
        _slamming = true;
        _nextSlamTime = Time.time + slamCooldown;

        yield return new WaitForSeconds(slamWindup);

        if (!IsPlayerInBossRoom())
        {
            _slamming = false;
            yield break;
        }

        Vector2 center = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, shockwaveRadius, slamHitMask);

        var damagedPlayers = new HashSet<PlayerHealth>();
        Rigidbody2D knockbackRb = null;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i];
            if (c == null || c.isTrigger) continue;
            if (!c.CompareTag("Player")) continue;

            PlayerDodge pd = c.GetComponentInParent<PlayerDodge>();
            if (pd != null && pd.IsInInvulnerableWindow)
                continue;

            PlayerHealth ph = c.GetComponentInParent<PlayerHealth>();
            if (ph == null || !damagedPlayers.Add(ph))
                continue;

            ph.TakeDamage(slamDamage);

            if (knockbackRb == null && c.attachedRigidbody != null)
                knockbackRb = c.attachedRigidbody;
        }

        if (knockbackRb != null)
        {
            Vector2 away = (knockbackRb.position - center).normalized;
            if (away.sqrMagnitude < 0.01f) away = Vector2.right;
            knockbackRb.AddForce(away * slamKnockbackForce, ForceMode2D.Impulse);
            knockbackRb.gameObject.SendMessage("OnKnockbackReceived", SendMessageOptions.DontRequireReceiver);
        }

        _slamming = false;
    }
}
