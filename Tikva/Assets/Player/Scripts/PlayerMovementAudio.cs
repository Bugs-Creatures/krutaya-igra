using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerMovementAudio : MonoBehaviour
{
    [Header("Clips (optional)")]
    public AudioClip footstepSound;
    public AudioClip landingSound;
    [Header("Timing")]
    [Min(.05f)] public float walkStepInterval=.48f;
    [Min(.05f)] public float runStepInterval=.3f;
    [Min(0)] public float minimumAirTime=.12f;
    [Range(0,1)] public float volume=.75f;
    AudioSource source;
    float stepProgress,airTime;
    bool airborne;
    public void ResetMotion(){stepProgress=airTime=0;airborne=false;}
    public void ProcessMovement(Vector3 displacement,bool grounded,bool running,float dt)
    {
        if(dt<=0)return;
        if(!grounded){airTime+=dt;airborne=true;stepProgress=0;return;}
        if(airborne)
        {
            if(airTime>=minimumAirTime){Play(landingSound);stepProgress=0;airTime=0;airborne=false;return;}
            airTime=0;airborne=false;
        }
        displacement.y=0;
        if(displacement.sqrMagnitude<.000001f){stepProgress=0;return;}
        stepProgress+=dt/Mathf.Max(.05f,running?runStepInterval:walkStepInterval);
        if(stepProgress>=1){stepProgress%=1;Play(footstepSound);}
    }
    void Play(AudioClip clip)
    {
        if(!clip)return;
        if(!source){source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;}
        source.PlayOneShot(clip,volume);
    }
    void OnDisable(){ResetMotion();if(source)source.Stop();}
}
