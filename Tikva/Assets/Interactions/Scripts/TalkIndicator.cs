using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[DefaultExecutionOrder(1100)]
public sealed class TalkIndicator : MonoBehaviour
{
    public Texture2D icon;
    public float showDistance=5;
    public float headOffset=.3f;
    public float iconSize=30;
    public float fadeSeconds=.2f;
    static readonly List<TalkIndicator> active=new();
    PlayerInteraction player;
    InteractionObject target;
    Transform head;
    RectTransform rect;
    UnityEngine.UI.RawImage image;
    CanvasGroup group;
    void OnEnable()=>active.Add(this);
    void Start()
    {
        player=Object.FindAnyObjectByType<PlayerInteraction>();target=GetComponent<InteractionObject>();
        var animator=GetComponentInChildren<Animator>();if(animator&&animator.isHuman)head=animator.GetBoneTransform(HumanBodyBones.Head);
        var g=new GameObject("Talk indicator",typeof(RectTransform),typeof(UnityEngine.UI.RawImage),typeof(CanvasGroup));
        rect=g.GetComponent<RectTransform>();rect.SetParent(player.prompt.transform,false);rect.sizeDelta=Vector2.one*iconSize;
        image=g.GetComponent<UnityEngine.UI.RawImage>();image.texture=icon;image.raycastTarget=false;
        group=g.GetComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;
    }
    void LateUpdate()
    {
        if(!player||!rect)return;
        var camera=player.player.cameraRig.output;
        Vector3 position=head?head.position:transform.position+Vector3.up*1.8f;
        var screen=camera.WorldToScreenPoint(position+Vector3.up*headOffset);
        bool visible=screen.z>0&&screen.x>=0&&screen.x<=Screen.width&&screen.y>=0&&screen.y<=Screen.height&&Vector3.Distance(player.transform.position,transform.position)<showDistance&&!player.player.dialogueLocked&&!player.player.ShopReturning;
        if(visible)
        {
            Vector3 ray=position-camera.transform.position;
            var hits=Physics.RaycastAll(camera.transform.position,ray.normalized,ray.magnitude,~(1<<2),QueryTriggerInteraction.Ignore);
            foreach(var hit in hits)if(!hit.transform.IsChildOf(transform)&&!hit.transform.IsChildOf(player.transform)){visible=false;break;}
        }
        visible=visible&&!player.player.modalInputLocked;
        group.alpha=Mathf.MoveTowards(group.alpha,visible?1:0,Time.unscaledDeltaTime/Mathf.Max(.01f,fadeSeconds));
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)player.prompt.transform,screen,null,out var local);rect.anchoredPosition=local;
        image.color=player.Hovered==target&&player.InReach(target)?Color.yellow:new Color(.78f,.78f,.78f,1);
    }
    void OnDisable(){active.Remove(this);if(rect)Destroy(rect.gameObject);}
}
