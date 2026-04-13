using System;
using UnityEngine;

[Serializable]
public class RoomState
{
    public Vector2Int coord;

    [Header("Doors (cardinal)")]
    public bool doorNorth;
    public bool doorEast;
    public bool doorSouth;
    public bool doorWest;

    [Header("Room content")]
    public RoomEventType eventType = RoomEventType.None;

    [Header("Progress flags")]
    public bool visited;
    public bool cleared;

    [Tooltip("When true, boss is not respawned and the cave exit beacon may appear.")]
    public bool bossDefeated;

    public int distanceFromStart;

}
