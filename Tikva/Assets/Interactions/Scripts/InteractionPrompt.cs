using UnityEngine;
using TMPro;

[DefaultExecutionOrder(2000)]
public sealed class InteractionPrompt : MonoBehaviour
{
    public TMP_FontAsset font;
    public Texture2D linkCursor;
    public Texture2D defaultCursor;
    public Vector2 defaultHotspot=new Vector2(2,2);
    public Vector2 itemHotspot=new Vector2(11,2);
    [Min(0)] public float textHoldSeconds=.55f;
    [Min(.01f)] public float textFadeSeconds=.45f;
    RectTransform canvasRect,tooltip;
    TextMeshProUGUI label;
    UnityEngine.UI.Image reticle;
    CanvasGroup tooltipGroup;
    float feedbackSeconds;
    bool handShown;
    Texture2D activeCursor;
    bool ownsFont;
    public bool HandShown=>handShown;
    public string VisibleText=>label&&tooltip.gameObject.activeSelf?label.text:"";
    public float VisibleFontSize=>label&&tooltip.gameObject.activeSelf?label.fontSize:0;
    void Awake()
    {
        var canvas=gameObject.GetComponent<Canvas>();if(!canvas)canvas=gameObject.AddComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
        var scaler=gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        canvasRect=(RectTransform)transform;
        if(!font){font=TMP_FontAsset.CreateFontAsset("Arial","Regular",48);ownsFont=true;}
        var panel=new GameObject("Interaction tooltip",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(CanvasGroup));panel.SetActive(false);panel.transform.SetParent(transform,false);
        tooltip=(RectTransform)panel.transform;tooltip.pivot=new Vector2(0,1);tooltip.sizeDelta=new Vector2(300,40);
        tooltipGroup=panel.GetComponent<CanvasGroup>();tooltipGroup.blocksRaycasts=false;tooltipGroup.interactable=false;
        panel.GetComponent<UnityEngine.UI.Image>().color=new Color(.03f,.03f,.03f,1);panel.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
        var text=new GameObject("Action",typeof(RectTransform));text.SetActive(false);text.transform.SetParent(panel.transform,false);
        label=text.AddComponent<TextMeshProUGUI>();label.font=font;label.fontSize=22;label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.NoWrap;
        var rect=(RectTransform)text.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(10,5);rect.offsetMax=new Vector2(-10,-4);text.SetActive(true);
        var dot=new GameObject("First person aim",typeof(RectTransform),typeof(UnityEngine.UI.Image));dot.transform.SetParent(transform,false);
        reticle=dot.GetComponent<UnityEngine.UI.Image>();reticle.raycastTarget=false;reticle.color=new Color(1,1,1,.75f);
        var dr=(RectTransform)dot.transform;dr.sizeDelta=new Vector2(3,3);dr.anchoredPosition=Vector2.zero;
        UseDefaultCursor();
    }
    public void UseDefaultCursor()
    {
        activeCursor=defaultCursor;handShown=false;
        ApplyCursor();
    }
    public void SetInteractionCursor(bool hand)
    {
        var cursor=hand?linkCursor:defaultCursor;
        if(hand==handShown&&cursor==activeCursor)return;
        activeCursor=cursor;handShown=hand;
        ApplyCursor();
    }
    void ApplyCursor()
    {
        Cursor.SetCursor(activeCursor,activeCursor?(handShown?itemHotspot:defaultHotspot):Vector2.zero,CursorMode.Auto);
    }
    void OnApplicationFocus(bool focused){if(focused)ApplyCursor();}
    public void Present(InteractionObject item,Vector2 screen,bool firstPerson)
    {
        if(!tooltip)return;
        reticle.enabled=firstPerson;
        bool hand=item && !firstPerson;
        SetInteractionCursor(hand);
        if(tooltip.gameObject.activeSelf)PlaceTooltip(screen);
    }
    public void ShowClickFeedback(InteractionObject item,Vector2 screen,bool firstPerson,bool inReach,string overrideText=null)
    {
        if(!item||!tooltip)return;
        tooltip.gameObject.SetActive(true);
        feedbackSeconds=Mathf.Max(0,textHoldSeconds)+Mathf.Max(.01f,textFadeSeconds);
        tooltipGroup.alpha=1;
        label.text=overrideText??item.Prompt(firstPerson,inReach);label.fontSize=item.hoverTextSize;
        label.color=item.kind==InteractionKind.Talk&&!inReach?Color.white:item.Allowed(firstPerson)?new Color(1,.9f,.25f):new Color(1,.32f,.27f);
        Vector2 preferred=label.GetPreferredValues(label.text);
        tooltip.sizeDelta=new Vector2(Mathf.Ceil(preferred.x)+20,Mathf.Ceil(preferred.y)+9);
        PlaceTooltip(screen);
    }
    public void ClearFeedback()
    {
        feedbackSeconds=0;
        if(tooltip)tooltip.gameObject.SetActive(false);
    }
    void PlaceTooltip(Vector2 screen)
    {
        float scale=GetComponent<Canvas>().scaleFactor;
        float width=tooltip.sizeDelta.x,height=tooltip.sizeDelta.y;
        screen+=new Vector2(18,-18);screen.x=Mathf.Clamp(screen.x,6,Screen.width-width*scale-6);
        screen.y=Mathf.Clamp(screen.y,(height+2)*scale,Screen.height-6);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,null,out var local);tooltip.anchoredPosition=local;
    }
    void Update()
    {
        if(!tooltip||!tooltip.gameObject.activeSelf)return;
        feedbackSeconds=Mathf.Max(0,feedbackSeconds-Time.unscaledDeltaTime);
        tooltipGroup.alpha=Mathf.Clamp01(feedbackSeconds/Mathf.Max(.01f,textFadeSeconds));
        if(feedbackSeconds<=0)tooltip.gameObject.SetActive(false);
    }
    void OnDisable()
    {
        ClearFeedback();
        Cursor.SetCursor(null,Vector2.zero,CursorMode.Auto);Cursor.visible=true;handShown=false;
    }
    void OnDestroy()
    {
        if(ownsFont&&font){foreach(var atlas in font.atlasTextures)if(atlas)Destroy(atlas);if(font.material)Destroy(font.material);Destroy(font);}
    }
}

