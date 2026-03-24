using UnityEngine;

/// <summary>
/// Trigger transition to the adjacent room. Optionally spawns two sprites at the door jambs
/// (both sides of the opening) when <see cref="iconSprite"/> is set.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    public DoorDirection direction;

    [Header("Doorway icons (optional)")]
    public Sprite iconSprite;
    [Tooltip("Distance from door center along the wall, to each icon (left/right or up/down).")]
    public float iconLocalHalfWidth = 0.35f;
    [Tooltip("For North/South doors: local Y tweak. For East/West: local X tweak.")]
    public float iconLocalY = 0f;
    public int iconSortingOrder = 5;
    public Color iconColor = Color.white;
    [Tooltip("Uniform scale for each icon sprite.")]
    public float iconScale = 0.35f;

    private DungeonRoomController roomController;

    private void Awake()
    {
        roomController = GetComponentInParent<DungeonRoomController>();
        if (iconSprite != null && transform.Find("DoorIcon_L") == null)
            CreateDoorwayIcons();
    }

    private void CreateDoorwayIcons()
    {
        Vector3 leftLocal;
        Vector3 rightLocal;

        // North/South: opening runs east-west → icons sit on −X / +X from center.
        // East/West: opening runs north-south → icons sit on −Y / +Y from center.
        switch (direction)
        {
            case DoorDirection.North:
            case DoorDirection.South:
                leftLocal = new Vector3(-iconLocalHalfWidth, iconLocalY, 0f);
                rightLocal = new Vector3(iconLocalHalfWidth, iconLocalY, 0f);
                break;
            case DoorDirection.East:
            case DoorDirection.West:
                leftLocal = new Vector3(iconLocalY, -iconLocalHalfWidth, 0f);
                rightLocal = new Vector3(iconLocalY, iconLocalHalfWidth, 0f);
                break;
            default:
                leftLocal = new Vector3(-iconLocalHalfWidth, 0f, 0f);
                rightLocal = new Vector3(iconLocalHalfWidth, 0f, 0f);
                break;
        }

        CreateIconChild("DoorIcon_L", leftLocal);
        CreateIconChild("DoorIcon_R", rightLocal);
    }

    private void CreateIconChild(string childName, Vector3 localPosition)
    {
        GameObject go = new GameObject(childName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one * iconScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = iconSprite;
        sr.color = iconColor;
        sr.sortingOrder = iconSortingOrder;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (roomController == null)
        {
            Debug.LogError("Door has no DungeonRoomController in its parents.", this);
            return;
        }

        roomController.TryMoveToAdjacentRoom(direction);
    }
}
