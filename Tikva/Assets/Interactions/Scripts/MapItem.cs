using UnityEngine;

public sealed class MapItem : MonoBehaviour
{
    public Texture2D mapImage;
    public void Collect(PlayerInteraction owner)
    {
        var inventory=owner.GetComponent<PlayerMapInventory>();if(!inventory)inventory=owner.gameObject.AddComponent<PlayerMapInventory>();
        inventory.Collect(mapImage);gameObject.SetActive(false);
    }
}
