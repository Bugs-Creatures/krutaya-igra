using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Street NPC who wants vodka. Talk to him: he asks for it. Come with the vodka in hand: a choice
// to give it. Given: he takes the bottle in his right hand, thanks, and drops the reward.
[DefaultExecutionOrder(900)]
public sealed class DrunkDialogue : MonoBehaviour, ITalkTarget
{
    public InteractionObject wantedItem;
    public InteractionObject reward;
    public string speaker="Мужик";
    public string askLine="Эх, водочки бы сейчас...";
    public string offerLine="О, водочка! Угостишь?";
    public string giveChoice="Отдать водку";
    public string keepChoice="Не важно";
    public string thanksLine="Вот спасибо, выручил! На, держи.";
    public string afterLine="Хорошо пошла...";
    public bool given;
    public float lettersPerSecond=35;
    [Min(0)] public float rewardThrow=1.2f;
    PlayerInteraction interaction;
    SurvivalPlayerController player;
    RectTransform panel,speechBackground,giveButton,keepButton;
    TextMeshProUGUI text,giveLabel,keepLabel;
    Transform hand;
    string line;
    float letters,remaining;
    bool choices;
    public bool Choosing=>choices;
    void Start()
    {
        interaction=Object.FindAnyObjectByType<PlayerInteraction>();player=interaction.player;
        hand=RightHand(this);
        if(reward&&!given)reward.gameObject.SetActive(false);
        panel=Rect("Drunk dialogue",interaction.prompt.transform,new Vector2(650,160),new Vector2(0,115));
        speechBackground=Rect("Speech background",panel,new Vector2(610,40),new Vector2(0,45));
        speechBackground.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.025f,.025f,.025f,1);
        text=Label(speechBackground,"",new Vector2(590,32),Vector2.zero);
        text.alignment=TextAlignmentOptions.Center;
        giveButton=Rect("Give",panel,new Vector2(290,36),new Vector2(0,-5));
        keepButton=Rect("Keep",panel,new Vector2(290,36),new Vector2(0,-45));
        giveLabel=Label(giveButton,giveChoice,new Vector2(280,36),Vector2.zero);
        keepLabel=Label(keepButton,keepChoice,new Vector2(280,36),Vector2.zero);
        giveLabel.alignment=keepLabel.alignment=TextAlignmentOptions.Center;
        panel.gameObject.SetActive(false);
    }
    // Humanoid bone if the rig has one, else the Mixamo bone by name (Y Bot is imported as Generic).
    public static Transform RightHand(Component npc)
    {
        var animator=npc.GetComponentInChildren<Animator>();
        if(animator&&animator.isHuman){var bone=animator.GetBoneTransform(HumanBodyBones.RightHand);if(bone)return bone;}
        foreach(var t in npc.GetComponentsInChildren<Transform>())if(t.name.EndsWith("RightHand"))return t;
        return null;
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
    public void Talk()
    {
        if(choices||player.ShopReturning)return;
        bool offer=!given&&interaction.heldItem==wantedItem;
        Say(given?afterLine:offer?offerLine:askLine,offer);
    }
    void Say(string phrase,bool choice)
    {
        var direction=player.transform.position-transform.position;direction.y=0;if(direction.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(direction);
        choices=choice;player.dialogueLocked=choice;
        if(choice){interaction.prompt.Present(null,Vector2.zero,player.EffectiveFirstPerson);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;interaction.prompt.UseDefaultCursor();}
        line=speaker+": "+phrase;letters=8;remaining=4;panel.gameObject.SetActive(true);text.text=line;text.maxVisibleCharacters=8;
        var size=text.GetPreferredValues(line);
        speechBackground.sizeDelta=new Vector2(Mathf.Ceil(size.x)+20,Mathf.Ceil(size.y)+10);
        text.rectTransform.sizeDelta=new Vector2(Mathf.Ceil(size.x)+2,Mathf.Ceil(size.y)+2);
        giveLabel.color=keepLabel.color=Color.white;
        giveButton.gameObject.SetActive(false);keepButton.gameObject.SetActive(false);
    }
    void Choose(bool give)
    {
        interaction.prompt.SetInteractionCursor(false);
        player.dialogueClickFrame=Time.frameCount;
        choices=false;player.dialogueLocked=false;
        if(player.EffectiveFirstPerson){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        if(give&&!given&&interaction.heldItem==wantedItem){Give();Say(thanksLine,false);}else panel.gameObject.SetActive(false);
    }
    void Give()
    {
        var bottle=interaction.TakeHeld();given=true;
        if(hand)bottle.SetGrip(hand,false);else bottle.gameObject.SetActive(false);
        if(!reward)return;
        var point=transform.position+transform.forward*.6f+Vector3.up*1.1f;
        reward.transform.position=point;reward.gameObject.SetActive(true);
        var body=reward.GetComponent<Rigidbody>();
        if(body){body.position=point;body.isKinematic=false;body.useGravity=true;body.linearVelocity=(transform.forward+Vector3.up)*rewardThrow;}
        reward.RecordNpcDrop();
    }
    void Update()
    {
        if(!player||!panel.gameObject.activeSelf)return;
        letters+=lettersPerSecond*Time.unscaledDeltaTime;text.maxVisibleCharacters=(int)letters;
        if(letters<line.Length)return;
        if(choices)
        {
            giveButton.gameObject.SetActive(true);keepButton.gameObject.SetActive(true);
            var mouse=Mouse.current;
            if(mouse!=null)
            {
                var point=mouse.position.ReadValue();
                bool giveHovered=RectTransformUtility.RectangleContainsScreenPoint(giveButton,point);
                bool keepHovered=RectTransformUtility.RectangleContainsScreenPoint(keepButton,point);
                giveLabel.color=giveHovered?Color.yellow:Color.white;keepLabel.color=keepHovered?Color.yellow:Color.white;
                interaction.prompt.SetInteractionCursor(giveHovered||keepHovered);
                if(mouse.leftButton.wasPressedThisFrame){if(giveHovered)Choose(true);else if(keepHovered)Choose(false);}
            }
        }
        else {remaining-=Time.unscaledDeltaTime;if(remaining<=0)panel.gameObject.SetActive(false);}
    }
    void OnDisable(){if(player)player.dialogueLocked=false;if(panel)panel.gameObject.SetActive(false);}
}

