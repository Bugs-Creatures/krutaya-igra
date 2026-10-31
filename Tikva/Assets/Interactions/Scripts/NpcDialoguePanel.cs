using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public sealed class NpcDialoguePanel : MonoBehaviour
{
    PlayerInteraction interaction;
    RectTransform root,background;
    TextMeshProUGUI label;
    readonly TextMeshProUGUI[] answers=new TextMeshProUGUI[2];
    string phrase;float letters,delay;Action<int> choice;Action finished;
    CursorLockMode oldLock;bool oldVisible;
    bool blocksInput;
    public bool IsOpen=>root&&root.gameObject.activeSelf;
    RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=position;return r;
    }
    TextMeshProUGUI Text(Transform parent,Vector2 size,Vector2 position)
    {
        var t=Rect("Text",parent,size,position).gameObject.AddComponent<TextMeshProUGUI>();t.font=interaction.prompt.font;t.fontSize=24;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;
    }
    void Ensure()
    {
        if(root)return;interaction=UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();
        root=Rect("Michael dialogue",interaction.prompt.transform,new Vector2(950,180),new Vector2(0,120));root.anchorMin=root.anchorMax=new Vector2(.5f,0);
        background=Rect("Speech background",root,new Vector2(900,44),new Vector2(0,45));background.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.025f,.025f,.025f,.92f);
        label=Text(background,new Vector2(880,40),Vector2.zero);
        for(int n=0;n<2;n++)answers[n]=Text(root,new Vector2(900,38),new Vector2(0,-5-42*n));
        root.gameObject.SetActive(false);
    }
    public void Begin(bool blockInput=true)
    {
        Ensure();blocksInput=blockInput;
        if(blocksInput)
        {
            oldLock=Cursor.lockState;oldVisible=Cursor.visible;
            interaction.player.dialogueLocked=true;interaction.prompt.ClearFeedback();interaction.prompt.Present(null,Vector2.zero,false);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;interaction.prompt.UseDefaultCursor();
        }
        root.gameObject.SetActive(true);
    }
    public void Say(string text,string first=null,string second=null,Action<int> selected=null,float hold=1,Action complete=null)
    {
        phrase=text;label.text=text;letters=text.IndexOf(':')+2;label.maxVisibleCharacters=(int)letters;
        var size=label.GetPreferredValues(text);background.sizeDelta=new Vector2(Mathf.Min(940,size.x+24),size.y+10);label.rectTransform.sizeDelta=background.sizeDelta-new Vector2(20,8);
        choice=selected;finished=complete;delay=hold;
        answers[0].text=first??"";answers[1].text=second??"";
        foreach(var answer in answers){answer.gameObject.SetActive(false);answer.color=Color.white;}
        if(blocksInput)interaction.prompt.SetInteractionCursor(false);
    }
    void Update()
    {
        if(!IsOpen)return;
        letters+=35*Time.unscaledDeltaTime;label.maxVisibleCharacters=(int)letters;if(letters<phrase.Length)return;
        if(choice!=null)
        {
            bool hovered=false;var mouse=Mouse.current;
            for(int n=0;n<2;n++)
            {
                answers[n].gameObject.SetActive(true);
                bool hit=mouse!=null&&RectTransformUtility.RectangleContainsScreenPoint(answers[n].rectTransform,mouse.position.ReadValue());
                answers[n].color=hit?Color.yellow:Color.white;hovered|=hit;
                if(hit&&mouse.leftButton.wasPressedThisFrame){Select(n);return;}
            }
            interaction.prompt.SetInteractionCursor(hovered);
        }
        else {delay-=Time.unscaledDeltaTime;if(delay<=0){var callback=finished;finished=null;if(callback!=null)callback();else End();}}
    }
    public void Select(int index)
    {
        if(choice==null||letters<phrase.Length)return;
        var callback=choice;choice=null;interaction.player.dialogueClickFrame=Time.frameCount;callback(index);
    }
    public void End()
    {
        if(!IsOpen)return;root.gameObject.SetActive(false);choice=null;finished=null;
        if(blocksInput)
        {
            interaction.player.dialogueLocked=false;interaction.player.dialogueClickFrame=Time.frameCount;
            Cursor.lockState=oldLock;Cursor.visible=oldVisible;interaction.prompt.UseDefaultCursor();
        }
    }
    void OnDisable(){if(IsOpen)End();}
}
