using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(50)]
public class SecurityGuardDiceRollVisual : MonoBehaviour
{
    public enum DiceRollFacingBasis
    {
        VelocityAlongSpriteRight = 0,
        VelocityAlongSpriteUp = 1,
        VelocityAlongSpriteDown = 2,
        VelocityAlongSpriteLeft = 3,
    }

    const int FrameCount = 16;
    [SerializeField] Sprite[] rollFrames = new Sprite[FrameCount];

    [SerializeField] Rigidbody2D rb;
    [SerializeField] float rollCyclesPerSecond = 2f;
    [SerializeField] float moveThreshold = 0.08f;
    [SerializeField] DiceRollFacingBasis facingBasis = DiceRollFacingBasis.VelocityAlongSpriteUp;
    [SerializeField] float facingFineTuneDegrees = 0f;
    [SerializeField] float spriteArtRotationDegrees = 0f;
    [SerializeField] bool invertRollFrameOrder = true;

    SpriteRenderer _sr;
    SecurityGuard _guard;
    float _rollPhase;
    int _lastFrameIndex = -1;

    static Vector2 BasisAxis(DiceRollFacingBasis basis)
    {
        return basis switch
        {
            DiceRollFacingBasis.VelocityAlongSpriteRight => Vector2.right,
            DiceRollFacingBasis.VelocityAlongSpriteUp => Vector2.up,
            DiceRollFacingBasis.VelocityAlongSpriteDown => Vector2.down,
            DiceRollFacingBasis.VelocityAlongSpriteLeft => Vector2.left,
            _ => Vector2.up,
        };
    }

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        _guard = GetComponent<SecurityGuard>();
    }

    void OnEnable()
    {
        _rollPhase = 0f;
        _lastFrameIndex = -1;
        ApplyFrame(invertRollFrameOrder ? FrameCount - 1 : 0);
    }

    void LateUpdate()
    {
        if (_sr == null || rb == null || rollFrames == null || rollFrames.Length < FrameCount)
            return;

        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude < moveThreshold * moveThreshold)
            return;

        Vector2 face = ResolveFaceDirection(v);
        if (face.sqrMagnitude < 0.0001f)
            return;

        face.Normalize();

        Vector2 basis = BasisAxis(facingBasis);
        float velAng = Mathf.Atan2(face.y, face.x) * Mathf.Rad2Deg;
        float axisAng = Mathf.Atan2(basis.y, basis.x) * Mathf.Rad2Deg;
        float angleDeg = velAng - axisAng + facingFineTuneDegrees + spriteArtRotationDegrees;

        rb.rotation = angleDeg;
        rb.angularVelocity = 0f;

        _rollPhase = Mathf.Repeat(_rollPhase + rollCyclesPerSecond * Time.deltaTime, 1f);
        int baseIdx = Mathf.Min(FrameCount - 1, Mathf.FloorToInt(_rollPhase * FrameCount));
        int idx = invertRollFrameOrder ? (FrameCount - 1 - baseIdx) : baseIdx;
        ApplyFrame(idx);
    }

    Vector2 ResolveFaceDirection(Vector2 velocity)
    {
        if (_guard != null)
        {
            if (_guard.IntendedMoveDirection.sqrMagnitude > 0.0001f)
                return _guard.IntendedMoveDirection;

            if (_guard.target != null)
            {
                Vector2 to = (Vector2)_guard.target.position - (Vector2)transform.position;
                if (to.sqrMagnitude > 0.0001f)
                    return to;
            }
        }

        return velocity;
    }

    void ApplyFrame(int index)
    {
        index = Mathf.Clamp(index, 0, FrameCount - 1);
        if (index == _lastFrameIndex)
            return;

        Sprite s = rollFrames[index];
        if (s != null)
            _sr.sprite = s;

        _lastFrameIndex = index;
    }
}
