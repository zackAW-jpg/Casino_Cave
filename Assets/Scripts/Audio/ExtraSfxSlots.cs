using UnityEngine;

public class ExtraSfxSlots : MonoBehaviour
{
    public AudioSource audioSource;
    public int lastPlayedIndex { get; private set; } = -1;
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
