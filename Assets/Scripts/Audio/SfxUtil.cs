using UnityEngine;

public static class SfxUtil
{
    public static void PlayOneShot(AudioClip clip, AudioSource source, Vector3 worldFallbackPosition, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        float v = Mathf.Clamp01(volumeScale);
        if (source != null)
        {
            source.PlayOneShot(clip, v);
            return;
        }

        AudioSource.PlayClipAtPoint(clip, worldFallbackPosition, v);
    }

    
    public static AudioClip PickRandomNonNull(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return null;

        for (int t = 0; t < clips.Length * 2; t++)
        {
            AudioClip c = clips[Random.Range(0, clips.Length)];
            if (c != null)
                return c;
        }

        return null;
    }
}
