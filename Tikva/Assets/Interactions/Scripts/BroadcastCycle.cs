using UnityEngine;

public enum BroadcastPhase { OnAir, Advertisement, PermanentlyOff }

[DefaultExecutionOrder(-150)]
public sealed class BroadcastCycle : MonoBehaviour
{
    public SurvivalPlayerController player;
    public PlayerInteraction interaction;
    [Header("Schedule (gameplay seconds)")]
    [Min(.1f)] public float firstBreakDelay=10;
    [Min(.1f)] public float onAirDuration=35;
    [Min(.1f)] public float advertisementDuration=60;
    [Min(0)] public float announcementLead=3;
    [Header("Audio clips — empty slots are allowed")]
    public AudioClip onAirMusic;
    public AudioClip advertisementMusic;
    public AudioClip breakAnnouncement;
    [Header("Audio sources")]
    public AudioSource onAirSource;
    public AudioSource advertisementSource;
    public AudioSource announcerSource;
    [Range(0,1)] public float musicVolume=.45f;
    [Range(0,1)] public float announcerVolume=.9f;
    [Range(0,1)] public float announcerMusicDuck=.45f;
    [Min(.01f)] public float musicFadeDuration=.6f;
    public BroadcastPhase Phase {get;private set;}=BroadcastPhase.OnAir;
    public bool HasStarted {get;private set;}
    public bool PermanentlyStopped=>Phase==BroadcastPhase.PermanentlyOff;
    public float SecondsRemaining=>HasStarted&&!PermanentlyStopped?remaining:0;
    public int AdvertisementCount {get;private set;}
    public int AnnouncementCount {get;private set;}
    float remaining;
    bool warned,suspended,airPaused,adPaused;

    void Awake()
    {
        if(!player)player=GetComponent<SurvivalPlayerController>();
        if(!interaction)interaction=GetComponent<PlayerInteraction>();
    }
    void OnEnable()
    {
        if(interaction)interaction.CharacterInteracted+=FirstCharacterInteraction;
        if(player)player.CamerasPermanentlyDisabled+=StopPermanently;
    }
    void Start()
    {
        if(player.cameraRig.CamerasDisabled){StopPermanently();return;}
        player.SetFirstPerson(false);SyncMusic();
    }
    void FirstCharacterInteraction(InteractionObject character)
    {
        if(HasStarted||PermanentlyStopped||suspended||!character||character.kind!=InteractionKind.Talk)return;
        HasStarted=true;remaining=Mathf.Max(.1f,firstBreakDelay);warned=false;
    }
    void Update()
    {
        if(player.cameraRig.CamerasDisabled&&!PermanentlyStopped)StopPermanently();
        if(PauseMenu.IsPaused)return;
        Advance(Time.deltaTime);
        if(announcerSource&&announcerSource.isPlaying&&!CanHearAnnouncement())announcerSource.Stop();
        UpdateMusic(Time.deltaTime);
    }

    // Used by the normal game clock and deterministic schedule verification.
    public void Advance(float seconds)
    {
        if(!HasStarted||PermanentlyStopped||suspended||PauseMenu.IsPaused||seconds<=0)return;
        while(seconds>0)
        {
            if(Phase==BroadcastPhase.OnAir&&!warned)
            {
                float untilAnnouncement=Mathf.Max(0,remaining-Mathf.Max(0,announcementLead));
                if(seconds>=untilAnnouncement)
                {
                    remaining-=untilAnnouncement;seconds-=untilAnnouncement;warned=true;
                    Announce();
                    if(seconds<=0)break;
                }
            }
            float step=Mathf.Min(seconds,remaining);remaining-=step;seconds-=step;
            if(Phase==BroadcastPhase.OnAir&&!warned&&remaining<=Mathf.Max(0,announcementLead)+.0001f)
            {warned=true;Announce();}
            if(remaining>.0001f)break;
            if(Phase==BroadcastPhase.OnAir)
            {
                Phase=BroadcastPhase.Advertisement;remaining=Mathf.Max(.1f,advertisementDuration);AdvertisementCount++;
                StopAnnouncement();player.SetFirstPerson(true);
            }
            else
            {
                Phase=BroadcastPhase.OnAir;remaining=Mathf.Max(.1f,onAirDuration);warned=false;
                player.SetFirstPerson(false);
            }
            SyncMusic();
        }
    }
    bool CanHearAnnouncement()=>!suspended&&!PermanentlyStopped&&Phase==BroadcastPhase.OnAir&&!player.cameraRig.InBlindZone&&!player.cameraRig.AutomaticFirstPerson;
    void Announce()
    {
        // A missed warning in a blind zone is not replayed on leaving it.
        if(!CanHearAnnouncement())return;
        AnnouncementCount++;
        if(!announcerSource||!breakAnnouncement)return;
        announcerSource.clip=breakAnnouncement;announcerSource.volume=announcerVolume;announcerSource.Play();
    }
    void StopAnnouncement(){if(announcerSource)announcerSource.Stop();}
    public void StopPermanently()
    {
        if(PermanentlyStopped)return;
        Phase=BroadcastPhase.PermanentlyOff;remaining=0;warned=true;StopAnnouncement();
        if(!player.cameraRig.CamerasDisabled)player.DisableCamerasPermanently();else player.SetFirstPerson(true);
        SyncMusic();
    }
    public void StopForEnding(){suspended=true;StopAnnouncement();}

    void SyncMusic()
    {
        if(suspended)return;
        if(Phase==BroadcastPhase.OnAir)Resume(onAirSource,onAirMusic,ref airPaused);
        else Resume(advertisementSource,advertisementMusic,ref adPaused);
    }
    static void Resume(AudioSource source,AudioClip clip,ref bool paused)
    {
        if(!source||!clip)return;
        if(source.clip!=clip){source.clip=clip;paused=false;}
        if(paused){source.UnPause();paused=false;}
        else if(!source.isPlaying)source.Play();
    }
    void UpdateMusic(float seconds)
    {
        bool air=!suspended&&Phase==BroadcastPhase.OnAir;
        bool ad=!suspended&&!air;
        float duck=announcerSource&&announcerSource.isPlaying?announcerMusicDuck:1;
        Fade(onAirSource,air?musicVolume*duck:0,seconds,!air,ref airPaused);
        Fade(advertisementSource,ad?musicVolume:0,seconds,!ad,ref adPaused);
    }
    void Fade(AudioSource source,float target,float seconds,bool pause,ref bool paused)
    {
        if(!source)return;
        source.volume=Mathf.MoveTowards(source.volume,target,seconds/Mathf.Max(.01f,musicFadeDuration));
        if(pause&&source.volume<=.0001f&&source.isPlaying){source.Pause();paused=true;}
    }
    void OnDisable()
    {
        if(interaction)interaction.CharacterInteracted-=FirstCharacterInteraction;
        if(player)player.CamerasPermanentlyDisabled-=StopPermanently;
        if(onAirSource)onAirSource.Stop();if(advertisementSource)advertisementSource.Stop();StopAnnouncement();
    }
}
