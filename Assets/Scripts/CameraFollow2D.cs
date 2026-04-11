using System.Collections;
using UnityEngine;

/// <summary>
/// Smooth follow + additive crit shake. Shake is applied on top of follow so it never snaps to an old Awake position.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    public static CameraFollow2D Instance { get; private set; }

    [Header("Follow Target")]
    public Transform target;

    [Header("Follow Settings")]
    public float smoothTime = 0.15f;

    [Header("Crit shake (additive)")]
    public float shakeDuration = 0.18f;
    public float shakeMagnitude = 0.12f;

    private Vector3 _followVelocity;
    private Vector3 _followPos;
    private Vector3 _shakeOffset;
    private Coroutine _shakeRoutine;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        _followPos = transform.position;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desired = new Vector3(
            target.position.x,
            target.position.y,
            transform.position.z);

        _followPos = Vector3.SmoothDamp(_followPos, desired, ref _followVelocity, smoothTime);
        transform.position = _followPos + _shakeOffset;
    }

    /// <summary>Adds a short positional shake without fighting SmoothDamp toward the player.</summary>
    public void PlayCritShake()
    {
        if (_shakeRoutine != null)
            StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float a = 1f - t / shakeDuration;
            Vector2 o = Random.insideUnitCircle * (shakeMagnitude * a);
            _shakeOffset = new Vector3(o.x, o.y, 0f);
            yield return null;
        }

        _shakeOffset = Vector3.zero;
        _shakeRoutine = null;
    }
}
