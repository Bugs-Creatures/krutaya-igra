using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The quest items of GameScene get their real models in place of the blockout cubes: the cube's mesh
// and renderer (or its cube child) go, the model goes in as a child "Model" standing where the cube
// stood (same footprint centre, same bottom; the shovel on the ground by the bench, where it is not on
// top of the others), the item's origin moves to the middle of the model (that is what the hand holds),
// its scale goes back to 1 and its box collider is fitted round the model. The item keeps its name and
// its own components (InteractionObject, Rigidbody, quest scripts). Safe to run again.
public static class InstallQuestItems
{
    const string Root = "Assets/ShopProps/Models/";
    static readonly (string item, string model, Vector3 turn)[] Items =
    {
        ("vodka", "Item_Vodka", Vector3.zero), ("rope", "Item_Rope", Vector3.zero),
        ("GrapplingHook", "Item_GrapplingHook", new Vector3(90, 0, 0)),   // on its side
        ("Лопата", "Item_Shovel", new Vector3(90, 0, 0)),                // lying, the blade face up
        ("Ключ", "Item_Key", Vector3.zero), ("Плоскогубцы", "Item_Cutters", Vector3.zero),
    };
    static readonly System.Collections.Generic.Dictionary<string,Vector3> Ground = new()   // footprint centre and bottom, where moved
    {
        ["Лопата"] = new Vector3(-1.0f, 0.0f, 8.95f),
    };

    [MenuItem("Tools/Install quest item models")]
    public static void Apply()
    {
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        InstallShopProps.ImportModels();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        var all=Object.FindObjectsByType<InteractionObject>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        foreach(var (itemName,model,turn) in Items)
        {
            var item=all.FirstOrDefault(i=>i.name==itemName);
            if(!item){Debug.LogWarning("QUESTITEM no "+itemName+" in the scene");continue;}
            var t=item.transform;
            var old=t.Find("Model");
            Bounds was;
            if(old){was=Around(old.gameObject);Object.DestroyImmediate(old.gameObject);}
            else
            {
                var stand=item.GetComponentsInChildren<MeshFilter>(true);
                was=Around(item.gameObject);
                foreach(var f in stand)                                       // the stand-in cube
                {
                    if(f.gameObject==item.gameObject){Object.DestroyImmediate(item.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}
                    else Object.DestroyImmediate(f.gameObject);
                }
                t.localScale=Vector3.one;
            }
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+model+".fbx"),scene);
            var upright=go.transform.rotation;                                // the FBX root's Z-up turn
            go.name="Model";
            go.transform.SetParent(t,false);
            go.transform.localPosition=Vector3.zero;
            go.transform.localRotation=Quaternion.Euler(turn)*upright;
            var b=Around(go);
            var foot=Ground.TryGetValue(itemName,out var g)?g:new Vector3(was.center.x,was.min.y,was.center.z);
            t.position=foot+Vector3.up*(b.size.y/2);                     // the item's origin in the middle of its model
            b=Around(go);
            go.transform.position+=t.position-b.center;
            b=Around(go);
            var box=item.GetComponent<BoxCollider>();
            if(box)
            {
                box.center=t.InverseTransformPoint(b.center);
                var s=t.InverseTransformVector(b.size);box.size=new Vector3(Mathf.Abs(s.x),Mathf.Abs(s.y),Mathf.Abs(s.z));
            }
            Debug.Log($"QUESTITEM {itemName} <- {model}: stood {was.size} at {was.center}, now {b.size} bottom {b.min.y:0.000}");
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }

    // world box round every mesh under go, from the meshes themselves (renderer bounds are empty while inactive)
    static Bounds Around(GameObject go)
    {
        Bounds? all=null;
        foreach(var f in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!f.sharedMesh)continue;
            var mb=f.sharedMesh.bounds;
            for(int i=0;i<8;i++)
            {
                var c=f.transform.TransformPoint(mb.center+Vector3.Scale(mb.extents,new Vector3(i&1,(i>>1)&1,(i>>2)&1)*2-Vector3.one));
                if(all==null)all=new Bounds(c,Vector3.zero);else{var a=all.Value;a.Encapsulate(c);all=a;}
            }
        }
        return all??new Bounds(go.transform.position,Vector3.one*.1f);
    }
}
