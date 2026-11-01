using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EndingTransition : MonoBehaviour
{
    public SurvivalPlayerController player;
    public CanvasGroup fade;
    public string endingScene="Ending01";
    [Min(0)] public float effectHold=.15f;
    [Min(.01f)] public float fadeDuration=1.6f;
    [Min(0)] public float blackHold=.2f;
    public bool IsRunning {get;private set;}
    public bool LoadStarted {get;private set;}
    public float FadeAlpha=>fade?fade.alpha:0;

    void Awake(){if(fade){fade.alpha=0;fade.blocksRaycasts=false;fade.interactable=false;}}
    public void Begin()=>Begin(endingScene,effectHold);
    public void Begin(string destination,float delay)
    {
        if(IsRunning)return;
        endingScene=destination;
        IsRunning=true;
        var pause=player.GetComponent<PauseMenu>();if(pause)pause.enabled=false;
        player.modalInputLocked=true;
        var interaction=player.GetComponent<PlayerInteraction>();
        if(interaction.prompt){interaction.prompt.ClearFeedback();interaction.prompt.Present(null,Vector2.zero,player.EffectiveFirstPerson);}
        Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
        if(fade){fade.gameObject.SetActive(true);fade.blocksRaycasts=true;}
        StartCoroutine(Finish(Mathf.Max(0,delay)));
    }
    IEnumerator Finish(float delay)
    {
        for(float elapsed=0;elapsed<delay;elapsed+=Time.unscaledDeltaTime)yield return null;
        for(float elapsed=0;elapsed<fadeDuration;elapsed+=Time.unscaledDeltaTime)
        {
            if(fade)fade.alpha=Mathf.SmoothStep(0,1,elapsed/Mathf.Max(.01f,fadeDuration));
            yield return null;
        }
        if(fade)fade.alpha=1;
        yield return new WaitForSecondsRealtime(blackHold);
        LoadStarted=true;
        yield return SceneManager.LoadSceneAsync(endingScene,LoadSceneMode.Single);
    }
}
