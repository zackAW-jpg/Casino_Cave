using UnityEngine;

/// <summary>
/// Visual-only dialogue poses: either swap sprites on one <see cref="SpriteRenderer"/> (<see cref="spritesPerStep"/>)
/// or toggle child <see cref="poseLayers"/>. Called from <see cref="NpcMultiLineDialogueTrigger"/>.
/// </summary>
public class NpcDialogueSpriteLayers : MonoBehaviour
{
    [Header("Option A: one SpriteRenderer, swap sprite per line")]
    [Tooltip("If empty, GetComponent<SpriteRenderer>() on this GameObject is used.")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("Index matches dialogue line index. Leave entries null to keep previous sprite.")]
    public Sprite[] spritesPerStep;

    [Header("Option B: stacked child objects (one pose per line)")]
    [Tooltip("Child GameObjects in order: index 0 = first dialogue line. Same world position; only one active.")]
    public GameObject[] poseLayers;

    [Tooltip("When dialogue ends or is cancelled, show only layer 0. If false, all layers are hidden.")]
    public bool showFirstLayerWhenDialogueEnds = true;

    private bool UsesSpriteSwap => spritesPerStep != null && spritesPerStep.Length > 0;

    private void Awake()
    {
        if (UsesSpriteSwap && spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>Activates one pose; index is clamped if there are fewer sprites/layers than dialogue lines.</summary>
    public void SetStep(int stepIndex)
    {
        if (UsesSpriteSwap)
        {
            if (spriteRenderer == null)
                return;

            int i = Mathf.Clamp(stepIndex, 0, spritesPerStep.Length - 1);
            Sprite s = spritesPerStep[i];
            if (s != null)
                spriteRenderer.sprite = s;
            return;
        }

        if (poseLayers == null || poseLayers.Length == 0)
            return;

        int visible = Mathf.Clamp(stepIndex, 0, poseLayers.Length - 1);

        for (int j = 0; j < poseLayers.Length; j++)
        {
            if (poseLayers[j] != null)
                poseLayers[j].SetActive(j == visible);
        }
    }

    public void OnDialogueEnded()
    {
        if (UsesSpriteSwap)
        {
            if (spriteRenderer == null || spritesPerStep == null || spritesPerStep.Length == 0)
                return;

            Sprite s = spritesPerStep[0];
            if (s != null)
                spriteRenderer.sprite = s;
            return;
        }

        if (poseLayers == null || poseLayers.Length == 0)
            return;

        if (showFirstLayerWhenDialogueEnds && poseLayers[0] != null)
            SetStep(0);
        else
        {
            for (int i = 0; i < poseLayers.Length; i++)
            {
                if (poseLayers[i] != null)
                    poseLayers[i].SetActive(false);
            }
        }
    }
}
