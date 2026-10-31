using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class InstallShopDialogue
{
    public static void Apply()
    {
        var vodka=GameObject.Find("vodka");var seller=GameObject.Find("salesman");
        if(!vodka||!seller)throw new System.Exception("vodka or salesman missing");
        var item=vodka.GetComponent<InteractionObject>();if(!item)item=vodka.AddComponent<InteractionObject>();item.kind=InteractionKind.Pickup;item.suspicious=false;item.displayName="Vodka";
        if(!vodka.GetComponentInChildren<Collider>())vodka.AddComponent<BoxCollider>();
        var talk=seller.GetComponent<InteractionObject>();if(!talk)talk=seller.AddComponent<InteractionObject>();talk.kind=InteractionKind.Talk;talk.suspicious=false;talk.reachDistance=4;talk.availableHoverText="поговорить";
        if(!seller.GetComponentInChildren<Collider>()){var c=seller.AddComponent<CapsuleCollider>();c.center=new Vector3(0,.9f,0);c.height=1.8f;c.radius=.35f;}
        var dialogue=seller.GetComponent<ShopDialogue>();if(!dialogue)dialogue=seller.AddComponent<ShopDialogue>();dialogue.salesman=seller.transform;dialogue.merchandise=item;
        var zone=GameObject.Find("Shop payment boundary");if(!zone)zone=new GameObject("Shop payment boundary");zone.transform.position=new Vector3(-14,1.5f,-17);var box=zone.GetComponent<BoxCollider>();if(!box)box=zone.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(14,4,10);dialogue.shopArea=box;
        var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/talk.png");importer.textureType=TextureImporterType.Cursor;importer.isReadable=true;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=32;importer.SaveAndReimport();dialogue.talkCursor=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/talk.png");
        EditorUtility.SetDirty(item);EditorUtility.SetDirty(talk);EditorUtility.SetDirty(dialogue);EditorSceneManager.MarkSceneDirty(seller.scene);EditorSceneManager.SaveScene(seller.scene);
    }
}
