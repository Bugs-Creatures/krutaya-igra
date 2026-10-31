using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using TMPro;

[DefaultExecutionOrder(-200)]
public sealed class PlayerMapInventory : MonoBehaviour
{
    public bool HasMap {get;private set;}
    public bool HasOpenedMap {get;private set;}
    public bool IsOpen {get;private set;}
    public Texture2D MapImage {get;private set;}
    PlayerInteraction interaction;GameObject overlay;Canvas canvas;CanvasGroup group;RectTransform panel,close;
    IDisposable buttons;CursorLockMode oldLock;bool oldVisible;int openedFrame;float fade;bool closing;
    public void Collect(Texture2D texture)
    {
        if(HasMap)return;MapImage=texture;HasMap=true;
        GetComponent<PlayerTutorialHud>()?.MapCollected();
    }
    void Start(){interaction=GetComponent<PlayerInteraction>();}
    void Update()
    {
        if(!interaction)return;
        if(!IsOpen&&HasMap&&Keyboard.current!=null&&Keyboard.current.mKey.wasPressedThisFrame&&!interaction.player.modalInputLocked&&!interaction.player.dialogueLocked&&!interaction.player.ShopReturning&&interaction.player.dialogueClickFrame!=Time.frameCount)Open();
        if(!IsOpen)return;
        float target=closing?0:1;fade=Mathf.MoveTowards(fade,target,Time.unscaledDeltaTime*7);group.alpha=fade;panel.localScale=Vector3.one*Mathf.Lerp(.94f,1,Mathf.SmoothStep(0,1,fade));
        if(closing&&fade<=0){FinishClose();return;}
        bool hover=Mouse.current!=null&&GameHudUI.Hit(close,Mouse.current.position.ReadValue());interaction.prompt.SetInteractionCursor(hover);
        close.GetComponent<TextMeshProUGUI>().color=hover?Color.yellow:Color.white;
    }
    public void Open()
    {
        if(!HasMap||IsOpen||interaction.player.modalInputLocked||interaction.player.dialogueLocked)return;
        if(!overlay)Build();IsOpen=true;HasOpenedMap=true;closing=false;fade=0;openedFrame=Time.frameCount;
        oldLock=Cursor.lockState;oldVisible=Cursor.visible;interaction.player.modalInputLocked=true;
        interaction.prompt.ClearFeedback();interaction.prompt.Present(null,Vector2.zero,false);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        overlay.SetActive(true);group.alpha=0;
        buttons=InputSystem.onAnyButtonPress.Call(control=>{if(Time.frameCount>openedFrame)Close();});
    }
    void Build()
    {
        canvas=GameHudUI.Canvas("Map overlay",220);overlay=canvas.gameObject;
        panel=GameHudUI.Rect("Map bounds",overlay.transform,new Vector2(820,540),Vector2.zero);
        var imageRect=GameHudUI.Rect("Map image",panel,new Vector2(820,540),Vector2.zero);var image=imageRect.gameObject.AddComponent<UnityEngine.UI.RawImage>();image.texture=MapImage;image.raycastTarget=false;
        var fit=imageRect.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=MapImage?(float)MapImage.width/MapImage.height:1;
        var closeLabel=GameHudUI.Text("Close",imageRect,interaction.prompt.font,"×",30,Color.white,new Vector2(40,40),new Vector2(-6,-6));
        close=closeLabel.rectTransform;close.anchorMin=close.anchorMax=Vector2.one;close.pivot=Vector2.one;
        closeLabel.outlineColor=Color.black;closeLabel.outlineWidth=.15f;
        group=overlay.AddComponent<CanvasGroup>();group.blocksRaycasts=false;
    }
    public void Close(){if(!IsOpen||closing)return;closing=true;buttons?.Dispose();buttons=null;}
    void FinishClose()
    {
        IsOpen=false;closing=false;overlay.SetActive(false);interaction.player.modalInputLocked=false;interaction.player.dialogueClickFrame=Time.frameCount;
        Cursor.lockState=oldLock;Cursor.visible=oldVisible;interaction.prompt.UseDefaultCursor();
    }
    void OnDisable(){buttons?.Dispose();buttons=null;if(IsOpen)FinishClose();}
    void OnDestroy(){if(overlay)Destroy(overlay);}
}
