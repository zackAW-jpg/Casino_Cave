using UnityEngine;

/// <summary>
/// Trigger transition to the adjacent room. Visuals: optional door sprite from
/// <see cref="DungeonRoomController"/> on a child <see cref="doorGraphicRoot"/>, or a runtime-spawned panel,
/// or legacy paired jamb icons from <see cref="iconSprite"/>.
/// </summary>
/// <remarks>
/// <b>Tighter door hitbox:</b> each door object uses a <see cref="UnityEngine.BoxCollider2D"/> on the same
/// GameObject as this script. Open <c>Assets/Prefabs/Rooms/Room.prefab</c>, expand <c>Doors</c>, select
/// <c>Door_N</c> / <c>Door_E</c> / etc., and reduce <b>Size</b> (and use <b>Offset</b> to keep the box
/// centered on the opening). This script does not resize the collider.
/// </remarks>
[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    public DoorDirection direction;

    [Header("Door graphic placement (prefab)")]
    [Tooltip("Use the child named DoorGraphic (or any transform). Select it in the Hierarchy and move, rotate, and scale in the Scene view — that pose is what you see in play mode.")]
    [SerializeField] Transform doorGraphicRoot;

    [Tooltip("When on, each room load sets the child SpriteRenderer Sorting Order to match Door Panel Sorting Order below. Turn off to control sorting only on the SpriteRenderer.")]
    [SerializeField] bool syncGraphicSortingOrder = true;

    [Header("Runtime panel (only if Door Graphic Root is not assigned)")]
    [Tooltip("Sorting order for a spawned DoorPanel when no doorGraphicRoot is set.")]
    public int doorPanelSortingOrder = 4;

    [Tooltip("Uniform scale for spawned DoorPanel.")]
    public float doorPanelUniformScale = 1f;

    [Tooltip("Local offset for spawned DoorPanel.")]
    public Vector2 doorPanelLocalOffset;

    [Tooltip("Local Z rotation in degrees for spawned DoorPanel.")]
    public float doorPanelLocalEulerZ = 0f;

    [Header("Doorway icons (fallback when no panel sprite)")]
    [Tooltip("Small jamb icons only used when there is no sprite from the Room (Door Graphic N/E/S/W) and none on the DoorGraphic SpriteRenderer in the prefab.")]
    public Sprite iconSprite;
    [Tooltip("Distance from door center along the wall, to each icon (left/right or up/down).")]
    public float iconLocalHalfWidth = 0.35f;
    [Tooltip("For North/South doors: local Y tweak. For East/West: local X tweak.")]
    public float iconLocalY = 0f;
    public int iconSortingOrder = 5;
    public Color iconColor = Color.white;
    [Tooltip("Uniform scale for each icon sprite.")]
    public float iconScale = 0.35f;

    private DungeonRoomController _roomController;

    private void Awake()
    {
        _roomController = GetComponentInParent<DungeonRoomController>();
    }

    /// <summary>
    /// Called by <see cref="DungeonRoomController.SetupFromRoomState"/> after door active state is set.
    /// </summary>
    public void ConfigureVisuals(Sprite roomAssignedPanelSprite)
    {
        ClearGeneratedVisuals();

        if (!gameObject.activeInHierarchy)
            return;

        Sprite panelSprite = ResolvePanelSprite(roomAssignedPanelSprite);

        if (doorGraphicRoot != null)
        {
            ApplyToGraphicRoot(panelSprite);
            if (panelSprite == null && iconSprite != null)
                CreateDoorwayIcons();
            return;
        }

        if (panelSprite != null)
        {
            CreatePanelChild(panelSprite);
            return;
        }

        if (iconSprite != null)
            CreateDoorwayIcons();
    }

    /// <summary>
    /// Room controller sprites override; if those are empty, use whatever is already on the prefab DoorGraphic (so you can assign art only on the child).
    /// </summary>
    Sprite ResolvePanelSprite(Sprite fromRoom)
    {
        if (fromRoom != null)
            return fromRoom;

        if (doorGraphicRoot != null)
        {
            var sr = doorGraphicRoot.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                return sr.sprite;
        }

        return null;
    }

    void ApplyToGraphicRoot(Sprite sprite)
    {
        doorGraphicRoot.gameObject.SetActive(sprite != null);
        if (sprite == null)
            return;

        SpriteRenderer sr = doorGraphicRoot.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = doorGraphicRoot.gameObject.AddComponent<SpriteRenderer>();

        sr.sprite = sprite;
        sr.color = Color.white;
        if (syncGraphicSortingOrder)
            sr.sortingOrder = doorPanelSortingOrder;
    }

    private void ClearGeneratedVisuals()
    {
        DestroyChildIfExists("DoorPanel");
        DestroyChildIfExists("DoorIcon_L");
        DestroyChildIfExists("DoorIcon_R");
    }

    private void DestroyChildIfExists(string childName)
    {
        Transform t = transform.Find(childName);
        if (t != null)
            Destroy(t.gameObject);
    }

    private void CreatePanelChild(Sprite sprite)
    {
        GameObject go = new GameObject("DoorPanel");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(doorPanelLocalOffset.x, doorPanelLocalOffset.y, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, doorPanelLocalEulerZ);
        go.transform.localScale = Vector3.one * doorPanelUniformScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingOrder = doorPanelSortingOrder;
    }

    private void CreateDoorwayIcons()
    {
        Vector3 leftLocal;
        Vector3 rightLocal;

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

    [Tooltip("Ignore door transitions briefly after the player takes damage (avoids knockback pushing into triggers).")]
    public float ignoreAfterDamageSeconds = 0.45f;

    [Tooltip("After a successful room transition, block further door triggers for this long so the player can leave the doorway.")]
    public float doorCooldownAfterTransitionSeconds = 1f;

    [Header("Audio (optional)")]
    [Tooltip("Played at this door when a room transition succeeds.")]
    public AudioClip doorUseSound;
    public AudioSource doorAudioSource;

    private static float s_lastSuccessfulDoorTransitionTime = -1000f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null && health.IsRecentlyDamaged(ignoreAfterDamageSeconds))
            return;

        if (Time.time - s_lastSuccessfulDoorTransitionTime < doorCooldownAfterTransitionSeconds)
            return;

        if (_roomController == null)
        {
            Debug.LogError("Door has no DungeonRoomController in its parents.", this);
            return;
        }

        if (_roomController.TryMoveToAdjacentRoom(direction))
        {
            s_lastSuccessfulDoorTransitionTime = Time.time;
            PlayDoorSound();
        }
    }

    void PlayDoorSound()
    {
        if (doorUseSound == null)
            return;
        if (doorAudioSource != null)
            doorAudioSource.PlayOneShot(doorUseSound);
        else
            AudioSource.PlayClipAtPoint(doorUseSound, transform.position, 1f);
    }
}
