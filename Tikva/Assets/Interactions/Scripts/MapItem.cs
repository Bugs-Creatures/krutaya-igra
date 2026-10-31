using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using TMPro;

public sealed class MapItem : MonoBehaviour
{
    public Texture2D mapImage;
    public bool IsOpen {get;private set;}
    PlayerInteraction interaction;
    GameObject overlay;
    RectTransform close;
    IDisposable buttonSubscription;
    CursorLockMode oldLock;bool oldVisible;
    int openingFrame;
    public void Open(PlayerInteraction owner)
    {
        if(IsOpen)return;interaction=owner;IsOpen=true;openingFrame=Time.frameCount;
        oldLock=Cursor.lockState;oldVisible=Cursor.visible;
        owner.player.modalInputLocked=true;owner.prompt.ClearFeedback();owner.prompt.Present(null,Vector2.zero,false);
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;owner.prompt.UseDefaultCursor();
        if(!overlay)Build();overlay.SetActive(true);
        buttonSubscription=InputSystem.onAnyButtonPress.Call(control=>{if(IsOpen&&Time.frameCount>openingFrame)Close();});
    }
    void Build()
    {
        overlay=new GameObject("Map viewer",typeof(RectTransform));var rect=overlay.GetComponent<RectTransform>();rect.SetParent(interaction.prompt.transform,false);
        rect.anchorMin=new Vector2(.15f,.15f);rect.anchorMax=new Vector2(.85f,.85f);rect.offsetMin=rect.offsetMax=Vector2.zero;
        var picture=new GameObject("Map image",typeof(RectTransform),typeof(UnityEngine.UI.RawImage),typeof(UnityEngine.UI.AspectRatioFitter));var p=picture.GetComponent<RectTransform>();p.SetParent(rect,false);p.anchorMin=p.anchorMax=new Vector2(.5f,.5f);
        picture.GetComponent<UnityEngine.UI.RawImage>().texture=mapImage;picture.GetComponent<UnityEngine.UI.RawImage>().raycastTarget=false;
        var fit=picture.GetComponent<UnityEngine.UI.AspectRatioFitter>();fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=mapImage?(float)mapImage.width/mapImage.height:1;
        var button=new GameObject("Close map",typeof(RectTransform));close=button.GetComponent<RectTransform>();close.SetParent(p,false);close.anchorMin=close.anchorMax=Vector2.one;close.pivot=Vector2.one;close.anchoredPosition=new Vector2(-8,-8);close.sizeDelta=new Vector2(38,38);
        var label=button.AddComponent<TextMeshProUGUI>();label.font=interaction.prompt.font;label.text="×";label.fontSize=36;label.color=Color.black;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
    }
    void Update()
    {
        if(!IsOpen)return;
        var mouse=Mouse.current;bool hit=mouse!=null&&RectTransformUtility.RectangleContainsScreenPoint(close,mouse.position.ReadValue());
        interaction.prompt.SetInteractionCursor(hit);
        close.GetComponent<TextMeshProUGUI>().color=hit?Color.yellow:Color.black;
    }
    public void Close()
    {
        if(!IsOpen)return;IsOpen=false;buttonSubscription?.Dispose();buttonSubscription=null;
        if(overlay)overlay.SetActive(false);
        interaction.player.modalInputLocked=false;interaction.player.dialogueClickFrame=Time.frameCount;
        Cursor.lockState=oldLock;Cursor.visible=oldVisible;interaction.prompt.UseDefaultCursor();
        if(interaction.heldItem&&interaction.heldItem.gameObject==gameObject)interaction.DropHeld();
    }
    void OnDisable(){if(IsOpen)Close();}
    void OnDestroy(){buttonSubscription?.Dispose();if(overlay)Destroy(overlay);}
}
