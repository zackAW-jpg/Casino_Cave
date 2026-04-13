using UnityEngine;

/// <summary>
/// Cycles through 16 dice sprites while moving; rotates so the roll reads along travel direction.
/// Runs in LateUpdate (after <see cref="SecurityGuard"/>) so facing uses the current frame's chase direction.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(50)]
public class SecurityGuardDiceRollVisual : MonoBehaviour
{
    public enum DiceRollFacingBasis
    {
        [Tooltip("World movement along texture +X. Tall edge-on frames stay vertical on screen when moving horizontally (often looks “sideways”).")]
        VelocityAlongSpriteRight = 0,
        [Tooltip("World movement along texture +Y. Edge-on / stacked frames lie along the path (roll toward target).")]
        VelocityAlongSpriteUp = 1,
        [Tooltip("World movement along texture -Y.")]
        VelocityAlongSpriteDown = 2,
        [Tooltip("World movement along texture -X.")]
        VelocityAlongSpriteLeft = 3,
    }

    const int FrameCount = 16;

    [Tooltip("Sprites in strict roll order (one full loop = one tumble cycle).")]
    [SerializeField] Sprite[] rollFrames = new Sprite[FrameCount];

    [SerializeField] Rigidbody2D rb;

    [Tooltip("How many full 16-frame cycles per second while moving.")]
    [SerializeField] float rollCyclesPerSecond = 2f;

    [Tooltip("Speeds below this hold the last frame and facing.")]
    [SerializeField] float moveThreshold = 0.08f;

    [Tooltip("Use VelocityAlongSpriteUp for tall edge-on dice art so the stack lies along the path.")]
    [SerializeField] DiceRollFacingBasis facingBasis = DiceRollFacingBasis.VelocityAlongSpriteUp;

    [Tooltip("Extra degrees after basis (small nudge only).")]
    [SerializeField] float facingFineTuneDegrees = 0f;

    [Tooltip("Constant Z rotation applied on top of facing. Same effect as batch-rotating every dice PNG by this amount in your art tool (try ±90 if the roll axis was exported sideways).")]
    [SerializeField] float spriteArtRotationDegrees = 0f;

    [Tooltip("Play frames 15→0 while moving if the export order is opposite to forward travel.")]
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
