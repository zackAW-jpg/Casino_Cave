using UnityEngine;

public static class RoomInteriorBounds
{
    public static bool TryCompute(Transform roomRoot, out Bounds worldBounds)
    {
        worldBounds = default;
        if (roomRoot == null)
            return false;

        RoomWall[] walls = roomRoot.GetComponentsInChildren<RoomWall>(true);
        if (walls == null || walls.Length == 0)
            return false;

        bool hasAny = false;
        Bounds envelope = default;
        for (int i = 0; i < walls.Length; i++)
        {
            Collider2D wallCol = walls[i] != null ? walls[i].GetComponent<Collider2D>() : null;
            if (wallCol == null || !wallCol.enabled || wallCol.isTrigger)
                continue;

            if (!hasAny)
            {
                envelope = wallCol.bounds;
                hasAny = true;
            }
            else
            {
                envelope.Encapsulate(wallCol.bounds);
            }
        }

        if (!hasAny)
            return false;

        
        
        float roomCx = envelope.center.x;
        float roomCy = envelope.center.y;
        float leftInner = float.NegativeInfinity;
        float rightInner = float.PositiveInfinity;
        float bottomInner = float.NegativeInfinity;
        float topInner = float.PositiveInfinity;
        bool hasLeft = false, hasRight = false, hasBottom = false, hasTop = false;

        for (int i = 0; i < walls.Length; i++)
        {
            Collider2D wallCol = walls[i] != null ? walls[i].GetComponent<Collider2D>() : null;
            if (wallCol == null || !wallCol.enabled || wallCol.isTrigger)
                continue;

            Bounds b = wallCol.bounds;
            bool vertical = b.size.x <= b.size.y;
            if (vertical)
            {
                if (b.center.x <= roomCx)
                {
                    leftInner = Mathf.Max(leftInner, b.max.x);
                    hasLeft = true;
                }
                else
                {
                    rightInner = Mathf.Min(rightInner, b.min.x);
                    hasRight = true;
                }
            }
            else
            {
                if (b.center.y <= roomCy)
                {
                    bottomInner = Mathf.Max(bottomInner, b.max.y);
                    hasBottom = true;
                }
                else
                {
                    topInner = Mathf.Min(topInner, b.min.y);
                    hasTop = true;
                }
            }
        }

        if (hasLeft && hasRight && hasBottom && hasTop && leftInner < rightInner && bottomInner < topInner)
        {
            Vector3 min = new Vector3(leftInner, bottomInner, envelope.min.z);
            Vector3 max = new Vector3(rightInner, topInner, envelope.max.z);
            worldBounds.SetMinMax(min, max);
            return true;
        }

        
        worldBounds = envelope;
        return true;
    }

    public static Vector2 ClampXY(Vector2 position, Bounds room, float insetX, float insetY)
    {
        float minX = room.min.x + insetX;
        float maxX = room.max.x - insetX;
        float minY = room.min.y + insetY;
        float maxY = room.max.y - insetY;

        if (minX > maxX)
        {
            float cx = room.center.x;
            minX = cx;
            maxX = cx;
        }

        if (minY > maxY)
        {
            float cy = room.center.y;
            minY = cy;
            maxY = cy;
        }

        return new Vector2(
            Mathf.Clamp(position.x, minX, maxX),
            Mathf.Clamp(position.y, minY, maxY));
    }
}
