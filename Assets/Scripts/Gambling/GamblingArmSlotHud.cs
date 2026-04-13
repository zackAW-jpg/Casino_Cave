using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GamblingArmSlotHud : MonoBehaviour
{
    [Header("Reels (left → right)")]
    public Image reelLeft;
    public Image reelMiddle;
    public Image reelRight;

    [Header("Symbol strips (enum order)")]
    public Sprite[] outerReelSprites;
    public Sprite[] middleReelSprites;

    [Header("Draw order (UI)")]
    public RectTransform slotMachineBackground;

    [Header("Display")]
    [Range(16f, 256f)]
    public float maxReelSymbolSize = 72f;

    [Header("Timing")]
    public float spinTickSeconds = 0.04f;

    [Header("Slam")]
    public float slamScale = 1.12f;
    public float slamDurationSeconds = 0.08f;

    [Header("Audio (optional)")]
    public AudioClip reelSpinTickSound;
    [Range(0.1f, 2f)] public float reelSpinTickVolumeScale = 1.35f;
    public AudioClip[] reelStopSoundsPerSlot = new AudioClip[3];
    public AudioClip rollCompleteSound;
    public AudioSource audioSource;

    private void Awake()
    {
        FixSiblingDrawOrder();
        DisableRaycastTargetsOnReels();
    }

    
    
    
    private void DisableRaycastTargetsOnReels()
    {
        if (reelLeft != null) reelLeft.raycastTarget = false;
        if (reelMiddle != null) reelMiddle.raycastTarget = false;
        if (reelRight != null) reelRight.raycastTarget = false;

        if (slotMachineBackground != null)
        {
            var bg = slotMachineBackground.GetComponent<Image>();
            if (bg != null)
                bg.raycastTarget = false;
        }
    }

    
    
    
    private void FixSiblingDrawOrder()
    {
        Transform parent = null;
        if (reelLeft != null) parent = reelLeft.transform.parent;
        else if (reelMiddle != null) parent = reelMiddle.transform.parent;
        else if (reelRight != null) parent = reelRight.transform.parent;
        if (parent == null)
            return;

        if (slotMachineBackground != null && slotMachineBackground.parent == parent)
            slotMachineBackground.SetAsFirstSibling();

        if (reelLeft != null) reelLeft.transform.SetAsLastSibling();
        if (reelMiddle != null) reelMiddle.transform.SetAsLastSibling();
        if (reelRight != null) reelRight.transform.SetAsLastSibling();
    }

    private void Start()
    {
        if (outerReelSprites == null || outerReelSprites.Length < 4)
            Debug.LogWarning("GamblingArmSlotHud: assign Outer Reel Sprites (4) on this component — left/right reels stay empty otherwise.", this);
        if (middleReelSprites == null || middleReelSprites.Length < 5)
            Debug.LogWarning("GamblingArmSlotHud: assign Middle Reel Sprites (5) on this component.", this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (outerReelSprites != null && outerReelSprites.Length > 0 && outerReelSprites.Length != 4)
            Debug.LogWarning("GamblingArmSlotHud: Outer Reel Sprites should have exactly 4 entries (Punch…Hammer).", this);
        if (middleReelSprites != null && middleReelSprites.Length > 0 && middleReelSprites.Length != 5)
            Debug.LogWarning("GamblingArmSlotHud: Middle Reel Sprites should have exactly 5 entries (Mushroom…Gold).", this);

        if (slotMachineBackground != null && reelLeft != null &&
            slotMachineBackground.parent != reelLeft.transform.parent)
        {
            Debug.LogWarning(
                "GamblingArmSlotHud: Slot Machine Background must be a sibling of the reel Images (same parent) so draw order can be fixed.",
                this);
        }
    }
#endif

    public IEnumerator PlayRollAnimation(GamblingArmRollResult result, float totalDuration)
    {
        totalDuration = Mathf.Max(0.08f, totalDuration);
        float t0 = Time.time;
        float tEnd = t0 + totalDuration;
        float lockLeft = t0 + totalDuration / 3f;
        float lockMid = t0 + 2f * totalDuration / 3f;

        bool lockedLeft = false;
        bool lockedMid = false;
        float nextSpin = t0;

        while (Time.time < tEnd)
        {
            float now = Time.time;

            if (now >= nextSpin)
            {
                if (!lockedLeft)
                    SpinOuter(reelLeft);
                if (!lockedMid)
                    SpinMiddle(reelMiddle);
                SpinOuter(reelRight);
                nextSpin += spinTickSeconds;
                PlayReelSpinTick();
            }

            if (!lockedLeft && now >= lockLeft)
            {
                ApplyOuter(reelLeft, result.Attack);
                lockedLeft = true;
                PlayReelStopSound(0);
                yield return Slam(reelLeft);
            }

            if (!lockedMid && now >= lockMid)
            {
                ApplyMiddle(reelMiddle, result.Modifier);
                lockedMid = true;
                PlayReelStopSound(1);
                yield return Slam(reelMiddle);
            }

            yield return null;
        }

        if (!lockedLeft)
        {
            ApplyOuter(reelLeft, result.Attack);
            PlayReelStopSound(0);
            yield return Slam(reelLeft);
        }

        if (!lockedMid)
        {
            ApplyMiddle(reelMiddle, result.Modifier);
            PlayReelStopSound(1);
            yield return Slam(reelMiddle);
        }

        ApplyOuter(reelRight, result.CritSymbol);
        PlayReelStopSound(2);
        yield return Slam(reelRight);

        PlayCompleteSound();
    }

    private void SpinOuter(Image img)
    {
        if (img == null || outerReelSprites == null || outerReelSprites.Length == 0)
            return;
        int i = Random.Range(0, outerReelSprites.Length);
        Sprite s = outerReelSprites[i];
        img.sprite = s;
        img.enabled = s != null;
        ApplyFittedSpriteSize(img, s);
    }

    private void SpinMiddle(Image img)
    {
        if (img == null || middleReelSprites == null || middleReelSprites.Length == 0)
            return;
        int i = Random.Range(0, middleReelSprites.Length);
        Sprite s = middleReelSprites[i];
        img.sprite = s;
        img.enabled = s != null;
        ApplyFittedSpriteSize(img, s);
    }

    private void ApplyOuter(Image img, GamblingAttackType a)
    {
        if (img == null || outerReelSprites == null) return;
        int i = Mathf.Clamp((int)a, 0, outerReelSprites.Length - 1);
        Sprite s = outerReelSprites[i];
        if (s != null)
        {
            img.sprite = s;
            img.enabled = true;
            ApplyFittedSpriteSize(img, s);
        }
    }

    private void ApplyMiddle(Image img, GamblingModifierType m)
    {
        if (img == null || middleReelSprites == null) return;
        int i = Mathf.Clamp((int)m, 0, middleReelSprites.Length - 1);
        Sprite s = middleReelSprites[i];
        if (s != null)
        {
            img.sprite = s;
            img.enabled = true;
            ApplyFittedSpriteSize(img, s);
        }
    }

    
    
    
    private void ApplyFittedSpriteSize(Image img, Sprite s)
    {
        if (img == null || s == null)
            return;

        float targetMax = maxReelSymbolSize > 0f ? maxReelSymbolSize : 72f;

        RectTransform rt = img.rectTransform;
        rt.localScale = Vector3.one;
        img.SetNativeSize();

        float rw = rt.rect.width;
        float rh = rt.rect.height;
        float maxDim = Mathf.Max(rw, rh);
        if (maxDim < 0.01f)
            return;

        float uniform = targetMax / maxDim;
        uniform = Mathf.Clamp(uniform, 0.001f, 4f);
        rt.localScale = new Vector3(uniform, uniform, 1f);
    }

    private IEnumerator Slam(Image img)
    {
        if (img == null || slamDurationSeconds <= 0.001f)
            yield break;

        Transform tr = img.transform;
        Vector3 baseScale = tr.localScale;
        float half = slamDurationSeconds * 0.5f;
        float e = 0f;
        while (e < half)
        {
            e += Time.deltaTime;
            float u = half > 1e-6f ? Mathf.Clamp01(e / half) : 1f;
            tr.localScale = baseScale * Mathf.Lerp(1f, slamScale, u);
            yield return null;
        }

        e = 0f;
        while (e < half)
        {
            e += Time.deltaTime;
            float u = half > 1e-6f ? Mathf.Clamp01(e / half) : 1f;
            tr.localScale = baseScale * Mathf.Lerp(slamScale, 1f, u);
            yield return null;
        }

        tr.localScale = baseScale;
    }

    void PlayReelSpinTick()
    {
        if (reelSpinTickSound == null)
            return;
        float v = Mathf.Clamp(reelSpinTickVolumeScale, 0.1f, 2f);
        if (audioSource != null)
            audioSource.PlayOneShot(reelSpinTickSound, v);
        else
            AudioSource.PlayClipAtPoint(reelSpinTickSound, transform.position, v);
    }

    void PlayReelStopSound(int slotIndex)
    {
        if (reelStopSoundsPerSlot == null || slotIndex < 0 || slotIndex >= reelStopSoundsPerSlot.Length)
            return;
        AudioClip c = reelStopSoundsPerSlot[slotIndex];
        if (c == null)
            return;
        if (audioSource != null)
            audioSource.PlayOneShot(c);
        else
            AudioSource.PlayClipAtPoint(c, transform.position, 1f);
    }

    private void PlayCompleteSound()
    {
        if (rollCompleteSound == null) return;
        if (audioSource != null)
            audioSource.PlayOneShot(rollCompleteSound);
        else
            AudioSource.PlayClipAtPoint(rollCompleteSound, transform.position, 1f);
    }
}
