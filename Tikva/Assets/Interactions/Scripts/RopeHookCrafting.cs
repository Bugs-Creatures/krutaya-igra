using UnityEngine;

public sealed class RopeHookCrafting : MonoBehaviour
{
    public InteractionObject rope;
    public InteractionObject hook;
    public InteractionObject resultPrefab;
    public InteractionObject LastCrafted {get;private set;}
    public bool TryCombine(PlayerInteraction interaction,InteractionObject target)
    {
        if(!resultPrefab||!rope||!hook||!interaction.heldItem)return false;
        var held=interaction.heldItem;
        bool pair=held==rope&&target==hook||held==hook&&target==rope;
        if(!pair)return false;
        Vector3 point=target.transform.position;Quaternion rotation=target.transform.rotation;
        interaction.TakeHeld();held.gameObject.SetActive(false);target.gameObject.SetActive(false);
        LastCrafted=Instantiate(resultPrefab,point,rotation);LastCrafted.name="RopeGrapplingHook";
        LastCrafted.RecordCraftedDrop();
        Destroy(held.gameObject);Destroy(target.gameObject);
        return true;
    }
}
