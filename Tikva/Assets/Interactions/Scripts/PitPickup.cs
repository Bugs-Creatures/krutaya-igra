using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InteractionObject))]
public sealed class PitPickup : MonoBehaviour
{
    public GameObject soilCover;
    public InteractionObject rope;
    public InteractionObject hook;
    public BoxCollider opening;
    [Tooltip("Invisible street colliders that cover the terrain depression. Only interaction rays inside the opening can pass through them.")]
    public Collider[] raycastPassThrough;
    public string incompleteToolText="этого недостаточно";
    public string missingToolText="не дотянуться";
    public bool Retrieved {get;private set;}
    public string LastFailure {get;private set;}
    public bool IgnorePickupDistance=>enabled&&!Retrieved;
    static readonly HashSet<PitPickup> active=new();

    void OnEnable()=>active.Add(this);
    void OnDisable()=>active.Remove(this);

    public bool CanRetrieve(PlayerInteraction interaction)
    {
        LastFailure=null;
        if(Retrieved)return true;
        var held=interaction.heldItem;
        if(soilCover&&soilCover.activeInHierarchy){LastFailure=missingToolText;return false;}
        if(held&&held.GetComponent<RopeHookTool>())return true;
        LastFailure=held&&(held==rope||held==hook)?incompleteToolText:missingToolText;
        return false;
    }

    public void MarkRetrieved(){Retrieved=true;LastFailure=null;}

    public static bool AllowsRayThrough(RaycastHit hit)
    {
        foreach(var pit in active)
        {
            if(!pit||pit.Retrieved||!pit.opening||(pit.soilCover&&pit.soilCover.activeInHierarchy))continue;
            if(!pit.opening.bounds.Contains(hit.point)||pit.raycastPassThrough==null)continue;
            foreach(var collider in pit.raycastPassThrough)if(collider==hit.collider)return true;
        }
        return false;
    }
}
