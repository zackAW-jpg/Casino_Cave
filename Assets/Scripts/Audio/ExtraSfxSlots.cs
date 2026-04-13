using UnityEngine;

/// <summary>
/// Optional holder for clips you have not wired to a specific system yet. Add to any scene object,
/// assign an <see cref="AudioSource"/> (or leave null for PlayClipAtPoint at this transform).
/// Call <see cref="Play"/> from your own scripts, timelines, or UnityEvents.
/// </summary>
public class ExtraSfxSlots : MonoBehaviour
{
    public AudioSource audioSource;

    [Tooltip("Index into the array below.")]
    public int lastPlayedIndex { get; private set; } = -1;

    [Tooltip("Generic one-shot slots — rename uses in your own notes.")]
    public AudioClip[] customOneShots;

    public void Play(int index, float volumeScale = 1f)
    {
        if (customOneShots == null || index < 0 || index >= customOneShots.Length)
            return;
        lastPlayedIndex = index;
        SfxUtil.PlayOneShot(customOneShots[index], audioSource, transform.position, volumeScale);
    }

    public void PlayRandom(float volumeScale = 1f)
    {
        if (customOneShots == null || customOneShots.Length == 0)
            return;
        AudioClip c = SfxUtil.PickRandomNonNull(customOneShots);
        SfxUtil.PlayOneShot(c, audioSource, transform.position, volumeScale);
    }
}
