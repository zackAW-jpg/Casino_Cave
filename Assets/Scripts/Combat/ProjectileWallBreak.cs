using UnityEngine;

public static class ProjectileWallBreak
{
    public static bool IsRoomWall(Collider2D other)
    {
        if (other == null || other.isTrigger)
            return false;
        return other.GetComponentInParent<RoomWall>() != null;
    }

    public static void SpawnBreakVfx(Vector3 worldPosition, GameObject vfxPrefab)
    {
        if (vfxPrefab == null)
            return;
        Object.Instantiate(vfxPrefab, worldPosition, Quaternion.identity);
    }

    public static Vector3 ContactPoint(Collider2D wall, Vector3 projectilePosition)
    {
        return wall.ClosestPoint(projectilePosition);
    }
}
