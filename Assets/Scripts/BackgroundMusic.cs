using UnityEngine;

/// <summary>
/// Main looping bed (e.g. soundtrack). Optional second layer for ambient loop on the same GameObject.
/// Put one instance in Main Menu (music) and one in Dungeon (ambient + music, or music only).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    [Tooltip("Primary loop — e.g. dungeon soundtrack or title theme.")]
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 0.5f;

    [Header("Optional second layer (ambient bed)")]
    [Tooltip("Separate loop under the main track (cave ambience, wind, etc.).")]
    public AudioClip ambientBedClip;
    [Range(0f, 1f)] public float ambientVolume = 0.35f;

    private AudioSource _source;
    private AudioSource _ambient;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.loop = true;
        _source.playOnAwake = false;
        _source.clip = clip;
        _source.volume = volume;

        if (ambientBedClip != null)
        {
            _ambient = gameObject.AddComponent<AudioSource>();
            _ambient.loop = true;
            _ambient.playOnAwake = false;
            _ambient.clip = ambientBedClip;
            _ambient.volume = ambientVolume;
        }
    }

    void Start()
    {
        if (clip != null && !_source.isPlaying)
            _source.Play();

        if (_ambient != null && ambientBedClip != null && !_ambient.isPlaying)
            _ambient.Play();
    }
}
