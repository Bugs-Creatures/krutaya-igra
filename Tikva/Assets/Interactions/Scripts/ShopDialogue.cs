using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[DefaultExecutionOrder(900)]
public sealed class ShopDialogue : MonoBehaviour, ITalkTarget
{
    public InteractionObject merchandise;
    public Transform salesman;
    public BoxCollider shopArea;
    public Texture2D talkCursor;
    public bool paid;
    public float lettersPerSecond=35;
    [Min(.1f)] public float returnDuration=.65f;
    PlayerInteraction interaction;
    SurvivalPlayerController player;
    RectTransform panel,speechBackground,payButton,cancelButton;
    TextMeshProUGUI text,payLabel,cancelLabel;
    string line;
    float letters,remaining,cooldown;
    bool choices;
    Vector3 safePosition,itemSafePosition;
    bool wasInside;
    public bool Choosing=>choices;
    void Start()
    {
        interaction=Object.FindAnyObjectByType<PlayerInteraction>();player=interaction.player;
        safePosition=player.transform.position;itemSafePosition=merchandise.transform.position;
        wasInside=Inside(safePosition);
        panel=Rect("Cashier dialogue",interaction.prompt.transform,new Vector2(650,160),new Vector2(0,115));
        speechBackground=Rect("Speech background",panel,new Vector2(610,40),new Vector2(0,45));
        speechBackground.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.025f,.025f,.025f,1);
        text=Label(speechBackground,"",new Vector2(590,32),Vector2.zero);
        text.alignment=TextAlignmentOptions.Center;
        payButton=Rect("Pay",panel,new Vector2(290,36),new Vector2(0,-5));
        cancelButton=Rect("Cancel",panel,new Vector2(290,36),new Vector2(0,-45));
        payLabel=Label(payButton,"Оплатить товар",new Vector2(280,36),Vector2.zero);
        cancelLabel=Label(cancelButton,"Не важно",new Vector2(280,36),Vector2.zero);
        payLabel.alignment=cancelLabel.alignment=TextAlignmentOptions.Center;
        panel.gameObject.SetActive(false);
    }
    RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=parent==interaction.prompt.transform?new Vector2(.5f,0):new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=position;return r;
    }
    TextMeshProUGUI Label(Transform parent,string value,Vector2 size,Vector2 position)
    {
        var r=Rect("Text",parent,size,position);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=interaction.prompt.font;t.fontSize=24;t.text=value;t.raycastTarget=false;return t;
    }
    bool Inside(Vector3 p){var q=shopArea.transform.InverseTransformPoint(p)-shopArea.center;var s=shopArea.size*.5f;return Mathf.Abs(q.x)<s.x&&Mathf.Abs(q.z)<s.z;}
    public void Talk()
    {
        if(choices||player.ShopReturning)return;
        Say(interaction.heldItem==merchandise&&!paid?"Чего желаете?":"Добро пожаловать!",interaction.heldItem==merchandise&&!paid);
    }
    void Say(string phrase,bool choice)
    {
        var direction=player.transform.position-salesman.position;direction.y=0;if(direction.sqrMagnitude>.001f)salesman.rotation=Quaternion.LookRotation(direction);
        choices=choice;player.dialogueLocked=choice;
        if(choice){interaction.prompt.Present(null,Vector2.zero,player.EffectiveFirstPerson);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;interaction.prompt.UseDefaultCursor();}
        line="Кассир: "+phrase;letters=8;remaining=4;panel.gameObject.SetActive(true);text.text=line;text.maxVisibleCharacters=8;
        var size=text.GetPreferredValues(line);
        speechBackground.sizeDelta=new Vector2(Mathf.Ceil(size.x)+20,Mathf.Ceil(size.y)+10);
        text.rectTransform.sizeDelta=new Vector2(Mathf.Ceil(size.x)+2,Mathf.Ceil(size.y)+2);
        payLabel.color=cancelLabel.color=Color.white;
        payButton.gameObject.SetActive(false);cancelButton.gameObject.SetActive(false);
    }
    void Choose(bool pay)
    {
        interaction.prompt.SetInteractionCursor(false);
        player.dialogueClickFrame=Time.frameCount;
        if(pay&&interaction.heldItem==merchandise&&!paid)paid=true;
        choices=false;player.dialogueLocked=false;
        if(player.EffectiveFirstPerson){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        if(pay)Say("Всего хорошего",false);else panel.gameObject.SetActive(false);
    }
    void Update()
    {
        if(!player)return;
        cooldown=Mathf.Max(0,cooldown-Time.deltaTime);
        if(!panel.gameObject.activeSelf)return;
        letters+=lettersPerSecond*Time.unscaledDeltaTime;text.maxVisibleCharacters=(int)letters;
        if(letters<line.Length)return;
        if(choices)
        {
            payButton.gameObject.SetActive(true);cancelButton.gameObject.SetActive(true);
            var mouse=Mouse.current;
            if(mouse!=null)
            {
                var point=mouse.position.ReadValue();
                bool payHovered=RectTransformUtility.RectangleContainsScreenPoint(payButton,point);
                bool cancelHovered=RectTransformUtility.RectangleContainsScreenPoint(cancelButton,point);
                payLabel.color=payHovered?Color.yellow:Color.white;cancelLabel.color=cancelHovered?Color.yellow:Color.white;
                interaction.prompt.SetInteractionCursor(payHovered||cancelHovered);
                if(mouse.leftButton.wasPressedThisFrame){if(payHovered)Choose(true);else if(cancelHovered)Choose(false);}
            }
        }
        else {remaining-=Time.unscaledDeltaTime;if(remaining<=0)panel.gameObject.SetActive(false);}
    }
    void LateUpdate()
    {
        if(!player)return;
        if(player.ShopReturning)return;
        bool inside=Inside(player.transform.position);
        if(!paid&&interaction.heldItem==merchandise&&wasInside&&!inside)
        {
            player.BeginShopReturn(safePosition,salesman.position+Vector3.up*1.5f,returnDuration);
            inside=true;
            if(cooldown<=0){Say("Вы не оплатили товар",false);cooldown=3;}
        }
        if(inside){var p=player.transform.position;var b=shopArea.bounds;if(p.x>b.min.x+.7f&&p.x<b.max.x-.7f&&p.z>b.min.z+.7f&&p.z<b.max.z-.7f)safePosition=p;}
        wasInside=inside;
        if(!paid&&!merchandise.IsHeld)
        {
            if(Inside(merchandise.transform.position))itemSafePosition=merchandise.transform.position;
            else {merchandise.transform.position=itemSafePosition;var body=merchandise.GetComponent<Rigidbody>();if(body){body.position=itemSafePosition;if(!body.isKinematic)body.linearVelocity=Vector3.zero;}}
        }
    }
    void OnDisable(){if(player)player.dialogueLocked=false;if(panel)panel.gameObject.SetActive(false);}
}

