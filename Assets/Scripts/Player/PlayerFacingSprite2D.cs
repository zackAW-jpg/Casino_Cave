using UnityEngine;

/// <summary>
/// Picks one of four cardinal sprites from movement: single-axis uses that axis; on diagonals,
/// keeps facing the axis that was already held when the second axis was added. Idle→diagonal
/// in one frame picks horizontal vs vertical randomly.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerFacingSprite2D : MonoBehaviour
{
    public PlayerMovement movement;

    [Header("Sprites (cardinal)")]
    public Sprite spriteUp;
    public Sprite spriteDown;
    public Sprite spriteLeft;
    public Sprite spriteRight;

    [Tooltip("Matches movement dead zone.")]
    public float inputDeadZone = 0.01f;

    private SpriteRenderer _sr;

    private enum Facing
    {
        Up,
        Down,
        Left,
        Right
    }

    private Facing _lockedFacing = Facing.Down;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (movement == null)
            movement = GetComponent<PlayerMovement>();
    }

    private void LateUpdate()
    {
        if (movement == null || _sr == null)
            return;

        Vector2 prev = movement.PreviousFrameMoveInput;
        Vector2 cur = movement.CurrentMoveInput;

        _lockedFacing = ComputeFacing(prev, cur, _lockedFacing, inputDeadZone);
        ApplySprite(_lockedFacing);
    }

    private static Facing ComputeFacing(Vector2 prev, Vector2 cur, Facing locked, float dz)
    {
        bool chx = Mathf.Abs(cur.x) > dz;
        bool chy = Mathf.Abs(cur.y) > dz;
        bool phx = Mathf.Abs(prev.x) > dz;
        bool phy = Mathf.Abs(prev.y) > dz;

        bool curD = chx && chy;
        bool prevD = phx && phy;
        bool curH = chx && !chy;
        bool curV = !chx && chy;
        bool prevH = phx && !phy;
        bool prevV = !phx && phy;

        if (!chx && !chy)
            return locked;

        if (curH)
            return cur.x > 0f ? Facing.Right : Facing.Left;

        if (curV)
            return cur.y > 0f ? Facing.Up : Facing.Down;

        // Diagonal
        if (prevD && curD)
            return locked;

        if (prevH && curD)
            return cur.x > 0f ? Facing.Right : Facing.Left;

        if (prevV && curD)
            return cur.y > 0f ? Facing.Up : Facing.Down;

        if (!prevD && curD && !phx && !phy)
        {
            if (Random.value < 0.5f)
                return cur.x > 0f ? Facing.Right : Facing.Left;
            return cur.y > 0f ? Facing.Up : Facing.Down;
        }

        return cur.x > 0f ? Facing.Right : Facing.Left;
    }

    private void ApplySprite(Facing f)
    {
        Sprite s = null;
        switch (f)
        {
            case Facing.Up:
                s = spriteUp;
                break;
            case Facing.Down:
                s = spriteDown;
                break;
            case Facing.Left:
                s = spriteLeft;
                break;
            default:
                s = spriteRight;
                break;
        }

        if (s != null)
            _sr.sprite = s;
    }
}
