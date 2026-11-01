using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class SceneMusic : MonoBehaviour
{
    public AudioClip music;
    [Range(0,1)] public float volume=.45f;

    void Awake()
    {
        var source=GetComponent<AudioSource>();
        source.playOnAwake=false;source.loop=true;source.spatialBlend=0;
        source.volume=volume;source.clip=music;
        if(music)source.Play();
    }
}
