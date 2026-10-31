using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[System.Serializable]
public sealed class CrimsonDialogueLine
{
    [TextArea(2,6)] public string phrase;
    public string answer;
    public CrimsonDialogueLine(string text,string response){phrase=text;answer=response;}
}

public sealed class CrimsonMassDialogue : MonoBehaviour, ITalkTarget
{
    public InteractionObject axeReward;
    public Color textColor=new Color(.9f,.14f,.13f);
    [Min(1)] public float lettersPerSecond=35;
    [Min(0)] public float waveAmplitude=2;
    [Min(0)] public float waveSpeed=4;
    public float waveCharacterPhase=.45f;
    [Min(0)] public float finalLineHold=1.2f;
    [Min(0)] public float farewellHold=2;
    public CrimsonDialogueLine[] lines={
        new CrimsonDialogueLine("Ты пришёл. Я Ждал.","Что ты такое?"),
        new CrimsonDialogueLine("Мы с другой планеты, мы не хотим тебе зла.","Кто \"мы\"?"),
        new CrimsonDialogueLine("Значит у нас получилось.","Что получилось?"),
        new CrimsonDialogueLine("Притвориться людьми. Ты до последнего не замечал разницы. Мы очень хотели стать людьми, ради этого всё здесь и построено. Нам нужно было узнать, как ведут себя люди.","Так кому \"нам\"?"),
        new CrimsonDialogueLine("Я не один такой. Тебя всё время окружали такие как я. Они хорошо справились.","И что теперь?"),
        new CrimsonDialogueLine("Единственное, что я не видел - человеческую смерть.","Ты хочешь меня убить?"),
        new CrimsonDialogueLine("Нет, к нашему с тобой счастью здесь есть ещё один человек.","Кто же?"),
        new CrimsonDialogueLine("Твой сосед. У подъезда. Держи, ты знаешь, что с этим делать.","")
    };
    public bool IsOpen {get;private set;}
    public bool RewardGiven {get;private set;}
    public bool HasCompleted {get;private set;}
    public int CurrentLine {get;private set;}
    public string VisiblePhrase=>body?body.text:"";
    PlayerInteraction interaction;RectTransform panel,background,answerButton;
    TextMeshProUGUI speaker,body,answer;
    float revealedCharacters,closeAt;bool lineComplete,farewellVisible;
    float prefixWidth,prefixHeight;
    CursorLockMode oldLock;bool oldVisible;int changedFrame;
    void Ensure()
    {
        if(panel)return;interaction=Object.FindAnyObjectByType<PlayerInteraction>();
        panel=GameHudUI.Rect("CrimsonMass dialogue",interaction.prompt.transform,new Vector2(1040,0),new Vector2(0,90));panel.anchorMin=panel.anchorMax=new Vector2(.5f,0);
        background=GameHudUI.Rect("Speech background",panel,Vector2.zero,new Vector2(0,35));background.pivot=new Vector2(.5f,0);GameHudUI.Image(background,new Color(.02f,.02f,.02f,1));
        speaker=GameHudUI.Text("Speaker",background,interaction.prompt.font,"???:",24,textColor,Vector2.zero,Vector2.zero);speaker.alignment=TextAlignmentOptions.TopLeft;
        body=GameHudUI.Text("Monster text",background,interaction.prompt.font,"",24,textColor,Vector2.zero,Vector2.zero);body.alignment=TextAlignmentOptions.TopLeft;body.lineSpacing=3;
        answerButton=GameHudUI.Rect("Answer",panel,new Vector2(960,44),Vector2.zero);answer=GameHudUI.Text("Answer text",answerButton,interaction.prompt.font,"",24,Color.white,new Vector2(950,42),Vector2.zero);
        panel.gameObject.SetActive(false);
    }
    public void Talk()
    {
        Ensure();var player=interaction.player;
        if(IsOpen||player.dialogueLocked||player.modalInputLocked||player.ShopReturning||lines==null||lines.Length==0)return;
        if(HasCompleted){ShowFarewell();return;}
        IsOpen=true;oldLock=Cursor.lockState;oldVisible=Cursor.visible;player.dialogueLocked=true;
        interaction.prompt.ClearFeedback();interaction.prompt.Present(null,Vector2.zero,false);
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;interaction.prompt.UseDefaultCursor();panel.gameObject.SetActive(true);CurrentLine=0;ShowLine();
    }
    void ShowLine()
    {
        var line=lines[CurrentLine];body.text=line.phrase;body.maxVisibleCharacters=0;body.color=textColor;speaker.color=textColor;
        revealedCharacters=0;lineComplete=false;FitSpeech(line.phrase);
        answer.text=line.answer;answer.color=Color.white;changedFrame=Time.frameCount;answerButton.gameObject.SetActive(false);
    }
    void FitSpeech(string phrase)
    {
        const float maxWidth=960,paddingX=16,gap=8;
        float paddingY=8+waveAmplitude;
        Vector2 prefix=speaker.GetPreferredValues(speaker.text);
        prefixWidth=prefix.x;prefixHeight=prefix.y;
        float width=Mathf.Clamp(body.GetPreferredValues(phrase,Mathf.Infinity,Mathf.Infinity).x,1,maxWidth-prefix.x-gap-paddingX*2);
        Vector2 size=body.GetPreferredValues(phrase,width,Mathf.Infinity);
        float height=Mathf.Max(prefix.y,size.y);
        // Keep wrapping and the text origin stable; only the backdrop follows revealed glyphs.
        background.pivot=new Vector2(0,1);
        background.anchoredPosition=new Vector2(-(prefix.x+gap+width+paddingX*2)/2,130);
        void Place(RectTransform rect,Vector2 dimensions,Vector2 offset)
        {rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.sizeDelta=dimensions;rect.anchoredPosition=offset;}
        Place(speaker.rectTransform,new Vector2(prefix.x+1,height),new Vector2(paddingX,-paddingY));
        Place(body.rectTransform,new Vector2(width+1,height+1),new Vector2(paddingX+prefix.x+gap,-paddingY));
        FitVisibleSpeech();
    }
    void FitVisibleSpeech()
    {
        body.ForceMeshUpdate();var info=body.textInfo;
        float width=0,height=prefixHeight;int lastLine=0;
        for(int i=0;i<info.characterCount&&i<body.maxVisibleCharacters;i++)
        {
            var character=info.characterInfo[i];
            width=Mathf.Max(width,character.xAdvance);lastLine=character.lineNumber;
        }
        if(info.lineCount>0)
            height=Mathf.Max(height,info.lineInfo[0].ascender-info.lineInfo[lastLine].descender);
        background.sizeDelta=new Vector2(prefixWidth+8+width+32,height+2*(8+waveAmplitude));
        answerButton.anchoredPosition=new Vector2(0,130-background.sizeDelta.y-36);
    }
    void UpdateTyping(float dt)
    {
        if(lineComplete)return;
        revealedCharacters+=lettersPerSecond*dt;body.maxVisibleCharacters=Mathf.Min(body.text.Length,Mathf.FloorToInt(revealedCharacters));
        FitVisibleSpeech();
        if(body.maxVisibleCharacters<body.text.Length)return;
        lineComplete=true;changedFrame=Time.frameCount;
        if(farewellVisible){closeAt=Time.unscaledTime+farewellHold;return;}
        if(CurrentLine==lines.Length-1)
        {HasCompleted=true;GiveAxe();closeAt=Time.unscaledTime+finalLineHold;}
        else answerButton.gameObject.SetActive(true);
    }
    void Update()
    {
        if(!IsOpen&&!farewellVisible)return;
        UpdateTyping(Time.unscaledDeltaTime);
        if(lineComplete&&(farewellVisible||CurrentLine==lines.Length-1)&&Time.unscaledTime>=closeAt)
        {if(IsOpen)End();else HideFarewell();return;}
        if(farewellVisible)return;
        var mouse=Mouse.current;bool hover=answerButton.gameObject.activeSelf&&mouse!=null&&GameHudUI.Hit(answerButton,mouse.position.ReadValue());
        interaction.prompt.SetInteractionCursor(hover);answer.color=hover?Color.yellow:Color.white;
        if(hover&&Time.frameCount>changedFrame&&mouse.leftButton.wasPressedThisFrame)Advance();
    }
    public void Advance()
    {
        if(!IsOpen||!lineComplete||CurrentLine==lines.Length-1)return;
        interaction.player.dialogueClickFrame=Time.frameCount;
        if(CurrentLine+1<lines.Length){CurrentLine++;ShowLine();}else End();
    }
    void LateUpdate()
    {
        if(!IsOpen&&!farewellVisible)return;
        // Rebuild the original mesh before applying offsets, so the wave never accumulates.
        body.ForceMeshUpdate();var info=body.textInfo;
        for(int i=0;i<info.characterCount&&i<body.maxVisibleCharacters;i++)
        {
            var character=info.characterInfo[i];if(!character.isVisible)continue;
            var vertices=info.meshInfo[character.materialReferenceIndex].vertices;
            var offset=Vector3.up*Mathf.Sin(Time.unscaledTime*waveSpeed-i*waveCharacterPhase)*waveAmplitude;
            for(int n=0;n<4;n++)vertices[character.vertexIndex+n]+=offset;
        }
        body.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
    void GiveAxe()
    {
        if(RewardGiven||!axeReward)return;RewardGiven=true;
        var player=interaction.player;var direction=Vector3.ProjectOnPlane(player.transform.position-transform.position,Vector3.up).normalized;
        if(direction.sqrMagnitude<.01f)direction=transform.forward;
        var point=transform.position+direction*1.2f+Vector3.up*1.2f;point.y=Mathf.Max(point.y,player.transform.position.y+.5f);
        axeReward.transform.position=point;axeReward.gameObject.SetActive(true);axeReward.Attach(transform);axeReward.Drop(point,GetComponentsInChildren<Collider>());axeReward.RecordNpcDrop();
    }
    public void End()
    {
        if(!IsOpen)return;IsOpen=false;if(panel)panel.gameObject.SetActive(false);
        if(interaction&&interaction.player)
        {interaction.player.dialogueLocked=false;interaction.player.dialogueClickFrame=Time.frameCount;}
        Cursor.lockState=oldLock;Cursor.visible=oldVisible;
        if(interaction&&interaction.prompt)interaction.prompt.UseDefaultCursor();
    }
    void ShowFarewell()
    {
        if(farewellVisible)return;
        farewellVisible=true;panel.gameObject.SetActive(true);answerButton.gameObject.SetActive(false);
        body.text="Удачи.";body.maxVisibleCharacters=0;body.color=textColor;speaker.color=textColor;
        revealedCharacters=0;lineComplete=false;FitSpeech(body.text);
    }
    void HideFarewell(){farewellVisible=false;if(panel)panel.gameObject.SetActive(false);}
    void OnDisable(){if(IsOpen)End();HideFarewell();}
    void OnDestroy(){if(panel)Destroy(panel.gameObject);}
}
