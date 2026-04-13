using UnityEngine;

/// <summary>
/// Small helpers so optional <see cref="AudioClip"/> fields stay null-safe everywhere.
/// </summary>
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

    /// <summary>Picks a random non-null entry (tries a few times if array is sparse).</summary>
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
