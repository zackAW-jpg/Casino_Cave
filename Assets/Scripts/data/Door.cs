using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    public DoorDirection direction;

    private DungeonRoomController roomController;

    private void Awake()
    {
        roomController = GetComponentInParent<DungeonRoomController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (roomController == null)
        {
            Debug.LogError("Door has no DungeonRoomController in its parents.", this);
            return;
        }

        roomController.TryMoveToAdjacentRoom(direction);
    }
}