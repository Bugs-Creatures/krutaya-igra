using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[DefaultExecutionOrder(1500)]
public sealed class PlayerTutorialHud : MonoBehaviour
{
    [Header("Tutorial pictures (optional)")]
    public Texture2D mapImage,suspiciousImage,droppedItemsImage;
    [Header("Scene tutorial cards")]
    public CanvasGroup tutorialOverlay;
    public TutorialCardView mapCard,suspiciousCard,droppedCard;
    public bool IsShowing=>showing;
    public int PageCount=>pages.Count;
    public int PageNumber=>page+1;
    public string CurrentHint=>showing&&pages.Count>0?pages[page]:"";
    PlayerInteraction interaction;SurvivalPlayerController player;PlayerMapInventory map;
    readonly HashSet<string> known=new();readonly List<string> pending=new(),pages=new();
    RectTransform card,close,action;
    CanvasGroup popupGroup;GameObject popup;
    TextMeshProUGUI title,body,counter,actionText,placeholder;
    UnityEngine.UI.RawImage picture;
    float queuedAt,popupFade;bool showing,closing;int page,openedFrame;
    readonly HashSet<InteractionObject> droppedItems=new();
    readonly Dictionary<InteractionObject,int> suspiciousAttempts=new();
    bool wasManualFirstPerson;
    CursorLockMode oldLock;bool oldVisible;
    void Start()
    {
        interaction=GetComponent<PlayerInteraction>();player=interaction.player;map=GetComponent<PlayerMapInventory>();wasManualFirstPerson=player.firstPerson;Build();
    }
    void Build()
    {
        popupGroup=tutorialOverlay;popup=popupGroup.gameObject;popup.SetActive(false);
        mapCard.gameObject.SetActive(false);suspiciousCard.gameObject.SetActive(false);droppedCard.gameObject.SetActive(false);
    }
    public void QueueHint(string id)
    {
        if(id=="movement")return;
        if(!known.Add(id))return;
        if(showing&&!closing){pages.Add(id);RefreshCounter();}else {pending.Add(id);queuedAt=Time.unscaledTime;}
    }
    public void MapCollected()
    {
        QueueHint("map");
    }
    public void SuspiciousAttempt(InteractionObject item)
    {
        if(!item||!item.suspicious||player.EffectiveFirstPerson)return;
        suspiciousAttempts.TryGetValue(item,out int count);suspiciousAttempts[item]=++count;
        if(count>=3)QueueHint("suspicious");
    }
    public void SuspiciousDroppedByCamera()=>QueueHint("suspicious");
    public void ItemDropped(InteractionObject item){if(item)droppedItems.Add(item);}
    bool HasDroppedItems()
    {
        foreach(var item in droppedItems)if(item&&item.isActiveAndEnabled&&item.IsDroppedByPlayer&&!item.IsHeld)return true;
        return false;
    }
    void Update()
    {
        if(!player)return;
        bool enteredManualFirstPerson=player.firstPerson&&!wasManualFirstPerson;
        wasManualFirstPerson=player.firstPerson;
        if(map.HasMap&&!map.HasOpenedMap&&player.EffectiveFirstPerson)QueueHint("map");
        if(enteredManualFirstPerson&&!known.Contains("dropped")&&HasDroppedItems())QueueHint("dropped");
        if(!showing&&pending.Count>0&&!player.modalInputLocked&&!player.dialogueLocked&&!player.ShopReturning&&player.dialogueClickFrame!=Time.frameCount&&Time.unscaledTime-queuedAt>=.2f)OpenHints();

        if(!showing)return;
        popupFade=Mathf.MoveTowards(popupFade,closing?0:1,Time.unscaledDeltaTime*7);popupGroup.alpha=popupFade;card.localScale=Vector3.one*Mathf.Lerp(.94f,1,Mathf.SmoothStep(0,1,popupFade));
        if(closing&&popupFade<=0){FinishHints();return;}
        var mouse=Mouse.current;if(mouse==null)return;
        var position=mouse.position.ReadValue();bool overClose=GameHudUI.Hit(close,position),overAction=GameHudUI.Hit(action,position);
        interaction.prompt.SetInteractionCursor(overClose||overAction);
        if(!closing&&popupFade>.85f&&Time.frameCount>openedFrame&&mouse.leftButton.wasPressedThisFrame){if(overClose)Dismiss();else if(overAction)NextPage();}
    }
    void OpenHints()
    {

        pending.RemoveAll(id=>id=="map"&&map.HasOpenedMap);if(pending.Count==0)return;
        pages.Clear();pages.AddRange(pending);pending.Clear();page=0;showing=true;closing=false;popupFade=0;openedFrame=Time.frameCount;
        oldLock=Cursor.lockState;oldVisible=Cursor.visible;player.modalInputLocked=true;interaction.prompt.ClearFeedback();interaction.prompt.Present(null,Vector2.zero,false);
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;interaction.prompt.UseDefaultCursor();popup.SetActive(true);popupGroup.alpha=0;RenderPage();
    }
    void RenderPage()
    {
        string id=pages[page];
        var view=id=="map"?mapCard:id=="suspicious"?suspiciousCard:droppedCard;
        foreach(var candidate in new[]{mapCard,suspiciousCard,droppedCard})candidate.gameObject.SetActive(candidate==view);
        card=(RectTransform)view.transform;close=view.closeButton;action=view.actionButton;
        title=view.title;body=view.body;counter=view.counter;actionText=view.actionText;placeholder=view.placeholder;picture=view.picture;
        var texture=id=="map"?mapImage:id=="suspicious"?suspiciousImage:droppedItemsImage;
        if(texture)picture.texture=texture;
        picture.enabled=picture.texture!=null;placeholder.gameObject.SetActive(!picture.texture);
        if(picture.texture)picture.GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectRatio=(float)picture.texture.width/picture.texture.height;
        RefreshCounter();
    }
    void RefreshCounter(){counter.text=pages.Count>1?$"{page+1}/{pages.Count}":"";actionText.text=page+1<pages.Count?"Далее":"Понятно";}
    public void NextPage(){if(!showing||closing)return;if(page+1<pages.Count){page++;RenderPage();popupFade=.65f;openedFrame=Time.frameCount;}else Dismiss();}
    public void Dismiss(){if(showing)closing=true;}
    void FinishHints()
    {
        showing=false;closing=false;popup.SetActive(false);pages.Clear();player.modalInputLocked=false;player.dialogueClickFrame=Time.frameCount;
        Cursor.lockState=player.EffectiveFirstPerson?oldLock:CursorLockMode.None;Cursor.visible=player.EffectiveFirstPerson?oldVisible:true;interaction.prompt.UseDefaultCursor();
    }
    void OnDisable(){if(showing)FinishHints();}
}




