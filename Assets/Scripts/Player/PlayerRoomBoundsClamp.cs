using UnityEngine;

[DefaultExecutionOrder(100)]
public class PlayerRoomBoundsClamp : MonoBehaviour
{
    public float extraInset = 0.1f;

    Rigidbody2D _rb;
    DungeonRoomSpawner _spawner;
    Collider2D[] _colliders;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _colliders = GetComponents<Collider2D>();
    }

    void FixedUpdate()
    {
        if (_spawner == null)
            _spawner = FindObjectOfType<DungeonRoomSpawner>();

        if (_rb == null || _spawner == null || _spawner.dungeonState == null)
            return;

        DungeonRoomController room = _spawner.GetRoomAt(_spawner.dungeonState.currentRoomCoord);
        if (room == null)
            return;

        if (!RoomInteriorBounds.TryCompute(room.transform, out Bounds b))
            return;

        float insetX = MaxColliderExtentX() + extraInset;
        float insetY = MaxColliderExtentY() + extraInset;

        Vector2 before = _rb.position;
        Vector2 after = RoomInteriorBounds.ClampXY(before, b, insetX, insetY);
        if ((after - before).sqrMagnitude < 1e-10f)
            return;

        _rb.position = after;
        Vector2 v = _rb.linearVelocity;
        if (Mathf.Abs(before.x - after.x) > 1e-4f)
            v.x = 0f;
        if (Mathf.Abs(before.y - after.y) > 1e-4f)
            v.y = 0f;
        _rb.linearVelocity = v;
    }

    float MaxColliderExtentX()
    {
        float r = 0.2f;
        if (_colliders == null)
            return r;
        for (int i = 0; i < _colliders.Length; i++)
        {
            Collider2D c = _colliders[i];
            if (c == null || !c.enabled || c.isTrigger)
                continue;
            float ex = c.bounds.extents.x;
            if (ex > r)
                r = ex;
        }

        return r;
    }

    float MaxColliderExtentY()
    {
        float r = 0.2f;
        if (_colliders == null)
            return r;
        for (int i = 0; i < _colliders.Length; i++)
        {
            Collider2D c = _colliders[i];
            if (c == null || !c.enabled || c.isTrigger)
                continue;
            float ey = c.bounds.extents.y;
            if (ey > r)
                r = ey;
        }

        return r;
    }
}
