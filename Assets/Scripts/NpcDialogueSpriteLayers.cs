using UnityEngine;

public class NpcDialogueSpriteLayers : MonoBehaviour
{
    [Header("Option A: one SpriteRenderer, swap sprite per line")]
    public SpriteRenderer spriteRenderer;
    public Sprite[] spritesPerStep;

    [Header("Option B: stacked child objects (one pose per line)")]
    public GameObject[] poseLayers;
    public bool showFirstLayerWhenDialogueEnds = true;

    private bool UsesSpriteSwap => spritesPerStep != null && spritesPerStep.Length > 0;

    private void Awake()
    {
        if (UsesSpriteSwap && spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    
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
