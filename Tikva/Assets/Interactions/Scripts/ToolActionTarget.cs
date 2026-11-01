using UnityEngine;

public enum ToolActionKind { BreakChains, EnterBasement, RemovePanelCover, CutPanelWire, ExitBasement, DigSoil, ChopTree }

public sealed class ToolActionTarget : MonoBehaviour
{
    public ToolActionKind action;
    public InteractionObject requiredTool;
    public string missingToolText;
    [Header("Electrical panel")]
    public ElectricalPanel electricalPanel;
    public BreakableTree breakableTree;
    [Header("Chains")]
    public GameObject intactVisual;
    public GameObject brokenVisual;
    [Header("Basement entrance")]
    public ToolActionTarget chains;
    public Transform destination;
    public bool IsBroken {get;private set;}
    public bool IsUnlocked {get;private set;}
    public bool IsDug {get;private set;}
    public string LastFailure {get;private set;}
    public bool TryUse(PlayerInteraction interaction)
    {
        LastFailure=null;
        if(action==ToolActionKind.ChopTree)
        {
            if(!breakableTree)return false;
            bool succeeded=breakableTree.TryChop(interaction,out string failure);
            LastFailure=failure;return succeeded;
        }
        if(action==ToolActionKind.DigSoil)
        {
            var target=GetComponent<InteractionObject>();
            if(target&&!interaction.InReach(target)){LastFailure=target.tooFarHoverText;return false;}
            if(IsDug)return false;
            if(!requiredTool||interaction.heldItem!=requiredTool)
            {LastFailure=string.IsNullOrWhiteSpace(missingToolText)?"здесь что-то есть":missingToolText;return false;}
            IsDug=true;
            if(target&&target.Outline)target.Outline.Show(false,PlayerInteraction.AvailableOutlineColor);
            gameObject.SetActive(false);
            return true;
        }
        if(action==ToolActionKind.ExitBasement)
        {
            if(!destination){LastFailure="Точка выхода не назначена.";return false;}
            interaction.player.TeleportFirstPerson(destination);
            interaction.player.DisableCamerasPermanently();
            return true;
        }
        if(action==ToolActionKind.RemovePanelCover||action==ToolActionKind.CutPanelWire)
        {
            if(!electricalPanel)return false;
            bool succeeded=electricalPanel.TryUse(this,interaction,out string failure);
            LastFailure=failure;return succeeded;
        }
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
        bool firstUnlock=!IsUnlocked;IsUnlocked=true;
        if(firstUnlock&&interaction.heldItem==requiredTool)
        {
            var key=interaction.TakeHeld();key.gameObject.SetActive(false);Destroy(key.gameObject);
        }
        interaction.player.TeleportFirstPerson(destination);
        if(action==ToolActionKind.EnterBasement)interaction.player.DisableCamerasPermanently();
        return true;
    }
}
