using UnityEngine;

[RequireComponent(typeof(Canvas),typeof(CanvasGroup))]
public sealed class CameraOnlyCanvas : MonoBehaviour
{
    public SurvivalPlayerController player;
    [Min(.01f)] public float fadeDuration=.2f;
    Canvas canvas;
    CanvasGroup group;
    void Awake()
    {
        canvas=GetComponent<Canvas>();group=GetComponent<CanvasGroup>();
        if(!player)player=Object.FindAnyObjectByType<SurvivalPlayerController>();
        group.interactable=false;group.blocksRaycasts=false;group.alpha=0;
    }
    void LateUpdate()
    {
        bool visible=player&&!player.EffectiveFirstPerson&&!player.modalInputLocked;
        if(visible)canvas.enabled=true;
        group.alpha=Mathf.MoveTowards(group.alpha,visible?1:0,Time.unscaledDeltaTime/Mathf.Max(.01f,fadeDuration));
        if(group.alpha<=0)canvas.enabled=false;
    }
}
