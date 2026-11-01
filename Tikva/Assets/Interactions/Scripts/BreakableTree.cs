using UnityEngine;

[RequireComponent(typeof(InteractionObject))]
public sealed class BreakableTree : MonoBehaviour
{
    public TreeBreakEffect effectPrefab;
    public bool IsBroken {get;private set;}
    public TreeBreakEffect LastEffect {get;private set;}
    public bool TryChop(PlayerInteraction interaction,out string failure)
    {
        failure=null;
        if(IsBroken||!interaction)return false;
        var target=GetComponent<InteractionObject>();
        if(!interaction.InReach(target)){failure=target.tooFarHoverText;return false;}
        if(!interaction.heldItem||!interaction.heldItem.GetComponent<AxeWeapon>())
        {failure="не перелезть";return false;}
        IsBroken=true;
        if(target.Outline)target.Outline.Show(false,PlayerInteraction.AvailableOutlineColor);
        if(effectPrefab)
        {
            LastEffect=Instantiate(effectPrefab,transform.position,transform.rotation);
            LastEffect.transform.localScale=transform.lossyScale;
            var direction=transform.position-interaction.transform.position;direction.y=0;
            LastEffect.Burst(direction.normalized);
        }
        gameObject.SetActive(false);
        return true;
    }
}
