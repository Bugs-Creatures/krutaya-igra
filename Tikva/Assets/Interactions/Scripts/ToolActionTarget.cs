using UnityEngine;

public enum ToolActionKind { BreakChains, EnterBasement }

public sealed class ToolActionTarget : MonoBehaviour
{
    public ToolActionKind action;
    public InteractionObject requiredTool;
    public string missingToolText;
    [Header("Chains")]
    public GameObject intactVisual;
    public GameObject brokenVisual;
    [Header("Basement entrance")]
    public ToolActionTarget chains;
    public Transform destination;
    public bool IsBroken {get;private set;}
    public bool IsUnlocked {get;private set;}
    public string LastFailure {get;private set;}
    public bool TryUse(PlayerInteraction interaction)
    {
        LastFailure=null;
        if(action==ToolActionKind.EnterBasement&&chains&&!chains.IsBroken){LastFailure="Сначала сломайте цепи.";return false;}
        if(action==ToolActionKind.BreakChains&&IsBroken){LastFailure="Цепи уже сломаны.";return false;}
        if(!IsUnlocked&&(!requiredTool||interaction.heldItem!=requiredTool))
        {LastFailure=string.IsNullOrWhiteSpace(missingToolText)?"Нужен подходящий предмет.":missingToolText;return false;}
        if(action==ToolActionKind.BreakChains)
        {
            IsBroken=true;
            var target=GetComponent<InteractionObject>();if(target&&target.Outline)target.Outline.Show(false,Color.red);
            if(intactVisual)intactVisual.SetActive(false);if(brokenVisual)brokenVisual.SetActive(true);
            foreach(var collider in GetComponentsInChildren<Collider>(true))collider.enabled=false;
            if(target)target.enabled=false;
            return true;
        }
        if(!destination){LastFailure="Точка входа не назначена.";return false;}
        IsUnlocked=true;interaction.player.TeleportFirstPerson(destination);return true;
    }
}
