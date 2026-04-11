using System.Collections;
using UnityEngine;

/// <summary>Optional component on enemies/bosses for ice slow and fire cinders from gambling arm hits.</summary>
public class StatusEffectHost : MonoBehaviour
{
    [Tooltip("Multiplies movement speed while ice slow is active.")]
    [Range(0.05f, 1f)] public float iceSlowMultiplier = 0.45f;

    [Header("Ice slow visual")]
    [Tooltip("Multiplies sprite colors while slowed (bluer).")]
    public Color iceTintColor = new Color(0.72f, 0.88f, 1.08f, 1f);

    public float MoveSpeedMultiplier { get; private set; } = 1f;

    private float _iceEndTime;
    private SpriteRenderer[] _spriteRenderers;
    private Color[] _spriteOriginalColors;
    private Coroutine _iceVisualCo;

    private void Awake()
    {
        CacheRenderers();
    }

    private void CacheRenderers()
    {
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (_spriteRenderers.Length == 0)
            return;

        _spriteOriginalColors = new Color[_spriteRenderers.Length];
        for (int i = 0; i < _spriteRenderers.Length; i++)
            _spriteOriginalColors[i] = _spriteRenderers[i].color;
    }

    private void Update()
    {
        if (Time.time >= _iceEndTime && MoveSpeedMultiplier < 1f - 0.001f)
            MoveSpeedMultiplier = 1f;
    }

    public void ApplyIceSlow(float durationSeconds)
    {
        _iceEndTime = Time.time + durationSeconds;
        MoveSpeedMultiplier = iceSlowMultiplier;
        ApplyIceVisual(durationSeconds);
    }

    private void ApplyIceVisual(float durationSeconds)
    {
        if (_spriteRenderers == null || _spriteRenderers.Length == 0)
            CacheRenderers();
        if (_spriteRenderers == null || _spriteRenderers.Length == 0)
            return;

        if (_iceVisualCo != null)
            StopCoroutine(_iceVisualCo);

        _iceVisualCo = StartCoroutine(IceVisualRoutine(durationSeconds));
    }

    private IEnumerator IceVisualRoutine(float durationSeconds)
    {
        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            if (_spriteRenderers[i] == null) continue;
            _spriteRenderers[i].color = _spriteOriginalColors[i] * iceTintColor;
        }

        yield return new WaitForSeconds(durationSeconds);

        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            if (_spriteRenderers[i] != null)
                _spriteRenderers[i].color = _spriteOriginalColors[i];
        }

        _iceVisualCo = null;
    }

    public void ScheduleCinderDamage(int damage, float delaySeconds, Health health, BossHealth bossHealth)
    {
        if (damage <= 0) return;
        StartCoroutine(CinderAfterDelay(damage, delaySeconds, health, bossHealth));
    }

    private static IEnumerator CinderAfterDelay(int damage, float delay, Health health, BossHealth bossHealth)
    {
        yield return new WaitForSeconds(delay);
        if (health != null && health.gameObject != null)
            health.TakeDamage(damage);
        else if (bossHealth != null && bossHealth.gameObject != null)
            bossHealth.TakeDamage(damage);
    }
}
