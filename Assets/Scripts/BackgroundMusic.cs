using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 0.5f;

    private AudioSource _source;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.loop = true;
        _source.playOnAwake = false;
        _source.clip = clip;
        _source.volume = volume;
    }

    void Start()
    {
        if (clip != null && !_source.isPlaying)
            _source.Play();
    }
}
