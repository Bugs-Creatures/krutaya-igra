using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[DefaultExecutionOrder(1600)]
public sealed class DroppedItemHighlight : MonoBehaviour
{
    public Color highlightColor=new Color(.25f,.8f,1,1);
    [Min(8)] public float distanceTextSize=14;
    readonly List<InteractionObject> targets=new();
    sealed class DistanceLabel
    {
        public RectTransform panel;
        public TextMeshProUGUI text;
        public Collider collider;
    }
    readonly Dictionary<InteractionObject,DistanceLabel> labels=new();
    PlayerInteraction interaction;bool revealing;float refreshTime;
    void Start(){interaction=GetComponent<PlayerInteraction>();}
    void RefreshTargets()
    {
        Clear();revealing=true;refreshTime=Time.unscaledTime+.25f;
        foreach(var item in Object.FindObjectsByType<InteractionObject>())if(item.IsSearchableDrop&&!item.IsHeld&&item.kind==InteractionKind.Pickup)targets.Add(item);
    }
    void LateUpdate()
    {
        if(!interaction)return;
        bool held=Mouse.current!=null&&Mouse.current.rightButton.isPressed;
        bool allowed=held&&interaction.player.EffectiveFirstPerson&&!interaction.player.modalInputLocked&&!interaction.player.dialogueLocked&&interaction.player.dialogueClickFrame!=Time.frameCount;
        if(!allowed){if(revealing)Clear();return;}
        if(!revealing||Time.unscaledTime>=refreshTime)RefreshTargets();
        foreach(var item in targets)
        {
            if(!item||!item.isActiveAndEnabled||item.IsHeld||!item.IsSearchableDrop){if(item&&item.Outline)item.Outline.Show(false,highlightColor);HideLabel(item);continue;}
            item.Outline.Show(true,highlightColor,true);
            UpdateLabel(item);
        }
    }
    void HideLabel(InteractionObject item)
    {
        if(ReferenceEquals(item,null))return;
        if(labels.TryGetValue(item,out var label)&&label.panel)label.panel.gameObject.SetActive(false);
    }
    void UpdateLabel(InteractionObject item)
    {
        if(!interaction.prompt||!interaction.player.cameraRig.output)return;
        if(!labels.TryGetValue(item,out var label))
        {
            label=new DistanceLabel();label.collider=item.GetComponentInChildren<Collider>();
            label.panel=GameHudUI.Rect("Distance - "+item.name,interaction.prompt.transform,Vector2.zero,Vector2.zero);
            GameHudUI.Image(label.panel,new Color(.03f,.03f,.03f,1));
            label.text=GameHudUI.Text("Distance",label.panel,interaction.prompt.font,"",distanceTextSize,highlightColor,Vector2.zero,Vector2.zero);
            label.text.textWrappingMode=TextWrappingModes.NoWrap;
            label.text.richText=false;
            label.text.rectTransform.anchorMin=Vector2.zero;label.text.rectTransform.anchorMax=Vector2.one;
            label.text.rectTransform.offsetMin=new Vector2(10,5);label.text.rectTransform.offsetMax=new Vector2(-10,-4);
            labels.Add(item,label);
        }
        Vector3 world=label.collider?label.collider.bounds.center:item.transform.position;
        world.y=label.collider?label.collider.bounds.max.y+.15f:world.y+.2f;
        Vector3 screen=interaction.player.cameraRig.output.WorldToScreenPoint(world);
        bool visible=screen.z>0&&screen.x>=0&&screen.x<=Screen.width&&screen.y>=0&&screen.y<=Screen.height;
        label.text.fontSize=distanceTextSize;label.text.color=highlightColor;
        label.text.text=item.ItemName+" "+Vector3.Distance(interaction.transform.position,item.transform.position).ToString("F1",CultureInfo.InvariantCulture)+"м";
        label.panel.gameObject.SetActive(visible);if(!visible)return;
        Vector2 preferred=label.text.GetPreferredValues(label.text.text);
        label.panel.sizeDelta=new Vector2(Mathf.Ceil(preferred.x)+20,Mathf.Ceil(preferred.y)+9);
        screen.y+=8;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)interaction.prompt.transform,screen,null,out var local);
        label.panel.anchoredPosition=local;label.panel.pivot=new Vector2(.5f,0);
    }
    void Clear()
    {
        foreach(var item in targets)if(item&&item.Outline)
        {
            bool hover=interaction&&interaction.Hovered==item;
            item.Outline.Show(hover,hover?(item.Allowed(interaction.player.EffectiveFirstPerson)?PlayerInteraction.AvailableOutlineColor:Color.red):highlightColor);
        }
        foreach(var label in labels.Values)if(label.panel)label.panel.gameObject.SetActive(false);
        targets.Clear();revealing=false;
    }
    void OnDisable(){Clear();}
    void OnDestroy(){foreach(var label in labels.Values)if(label.panel)Destroy(label.panel.gameObject);labels.Clear();}
}

