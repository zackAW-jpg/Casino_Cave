using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    public DoorDirection direction;

    [Header("Door graphic placement (prefab)")]
    [SerializeField] Transform doorGraphicRoot;
    [SerializeField] bool syncGraphicSortingOrder = true;

    [Header("Runtime panel (only if Door Graphic Root is not assigned)")]
    public int doorPanelSortingOrder = 4;
    public float doorPanelUniformScale = 1f;
    public Vector2 doorPanelLocalOffset;
    public float doorPanelLocalEulerZ = 0f;

    [Header("Doorway icons (fallback when no panel sprite)")]
    public Sprite iconSprite;
    public float iconLocalHalfWidth = 0.35f;
    public float iconLocalY = 0f;
    public int iconSortingOrder = 5;
    public Color iconColor = Color.white;
    public float iconScale = 0.35f;

    private DungeonRoomController _roomController;

    private void Awake()
    {
        _roomController = GetComponentInParent<DungeonRoomController>();
    }

    
    
    
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
    public float ignoreAfterDamageSeconds = 0.45f;
    public float doorCooldownAfterTransitionSeconds = 1f;

    [Header("Audio (optional)")]
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
