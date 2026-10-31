using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1600)]
public sealed class DroppedItemHighlight : MonoBehaviour
{
    public Color highlightColor=new Color(.25f,.8f,1,1);
    [Range(.2f,2)] public float highlightWidth=1;
    readonly List<InteractionObject> targets=new();
    PlayerInteraction interaction;bool revealing;float refreshTime;
    void Start(){interaction=GetComponent<PlayerInteraction>();}
    void RefreshTargets()
    {
        Clear();revealing=true;refreshTime=Time.unscaledTime+.25f;
        foreach(var item in Object.FindObjectsByType<InteractionObject>())if(item.IsDroppedByPlayer&&!item.IsHeld&&item.kind==InteractionKind.Pickup)targets.Add(item);
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
            if(!item||!item.isActiveAndEnabled||item.IsHeld||!item.IsDroppedByPlayer){if(item&&item.Outline)item.Outline.Show(false,highlightColor);continue;}
            item.Outline.Show(true,highlightColor,true,Mathf.Min(highlightWidth,item.Outline.width*.6f));
        }
    }
    void Clear()
    {
        foreach(var item in targets)if(item&&item.Outline)
        {
            bool hover=interaction&&interaction.Hovered==item;
            item.Outline.Show(hover,hover?(item.Allowed(interaction.player.EffectiveFirstPerson)?PlayerInteraction.AvailableOutlineColor:Color.red):highlightColor);
        }
        targets.Clear();revealing=false;
    }
    void OnDisable(){Clear();}
}
