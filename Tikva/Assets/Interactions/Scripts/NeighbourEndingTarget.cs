using UnityEngine;

[RequireComponent(typeof(DrunkDialogue))]
public sealed class NeighbourEndingTarget : MonoBehaviour
{
    public NeighbourDeathEffect deathEffectPrefab;
    public EndingTransition ending;
    public bool IsKilled {get;private set;}
    public NeighbourDeathEffect LastEffect {get;private set;}
    public bool HasAxe(PlayerInteraction interaction)=>interaction&&interaction.heldItem&&interaction.heldItem.GetComponent<AxeWeapon>();
    public bool Kill(PlayerInteraction interaction)
    {
        if(IsKilled||!ending||ending.IsRunning||!HasAxe(interaction)||!interaction.InReach(GetComponent<InteractionObject>()))return false;
        IsKilled=true;
        GetComponent<DrunkDialogue>().DropHeldBottle();
        if(deathEffectPrefab)
        {
            var direction=transform.position-interaction.transform.position;direction.y=0;
            LastEffect=Instantiate(deathEffectPrefab,transform.position,transform.rotation);
            LastEffect.Burst(direction.normalized);
        }
        // The transition lives outside the NPC, so it continues after the model and colliders disappear.
        gameObject.SetActive(false);
        ending.Begin();
        return true;
    }
}
