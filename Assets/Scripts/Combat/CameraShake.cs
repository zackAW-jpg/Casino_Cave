using UnityEngine;

/// <summary>
/// Forwards crit shake to <see cref="CameraFollow2D"/> on the same GameObject. Keeps existing Instance.Shake() calls working.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private CameraFollow2D _follow;

    private void Awake()
    {
        Instance = this;
        _follow = GetComponent<CameraFollow2D>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Shake()
    {
        if (_follow != null)
            _follow.PlayCritShake();
    }
}
