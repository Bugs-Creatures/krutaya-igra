using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The quest items of GameScene get their real models in place of the blockout cubes: the cube's mesh
// and renderer (or its cube child) go, the model goes in as a child "Model" standing where the cube
// stood (same footprint centre, same bottom; the shovel on the ground by the bench, where it is not on
// top of the others), the item's origin moves to the middle of the model (that is what the hand holds),
// its scale goes back to 1 and its box collider is fitted round the model. The item keeps its name and
// its own components (InteractionObject, Rigidbody, quest scripts). Small things get a collider a little
// larger than themselves so the cursor finds them, and where the collider of what they lie on stands
// higher than its mesh (the bench's box rides 14 cm over the seat) theirs is stretched up past it, or a
// key 8 mm thick would hide inside the bench's and the aim would hit the bench. The crafted
// rope-and-hook prefab gets its model too. Safe to run again.
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
        ["Ключ"] = new Vector3(-1.57f, 0.505f, 8.56f),                    // on the bench's seat, where the cubes were
        ["Плоскогубцы"] = new Vector3(-1.57f, 0.505f, 9.14f),
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
            var rest=item.gameObject.activeInHierarchy?Support(foot,t):foot.y;   // top of the collider under it
            if(rest<foot.y+.02f)foot.y=rest;                                     // a surface right under it: lie on it
            t.position=foot+Vector3.up*(b.size.y/2);                     // the item's origin in the middle of its model
            b=Around(go);
            go.transform.position+=t.position-b.center;
            b=Around(go);
            var box=item.GetComponent<BoxCollider>();
            if(box)Fit(box,t,b,rest+.03f);
            Physics.SyncTransforms();
            Debug.Log($"QUESTITEM {itemName} <- {model}: stood {was.size} at {was.center}, now {b.size} bottom {b.min.y:0.000}, aim {(item.gameObject.activeInHierarchy?Aim(item):"(off)")}");
        }
        CraftedPrefab();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }

    // the collider round the model, at least 5 cm each way and up to minTop at least (grown upwards, so it
    // still sits on its bottom)
    static void Fit(BoxCollider box,Transform t,Bounds b,float minTop=float.MinValue)
    {
        var s=t.InverseTransformVector(b.size);s=new Vector3(Mathf.Abs(s.x),Mathf.Abs(s.y),Mathf.Abs(s.z));
        var c=t.InverseTransformPoint(b.center);
        var size=Vector3.Max(s,Vector3.one*.05f);
        size.y=Mathf.Max(size.y,minTop-b.min.y);
        box.size=size;box.center=c+Vector3.up*((size.y-s.y)/2);
    }

    // the height of whatever is under foot, not counting the item itself
    static float Support(Vector3 foot,Transform self)
    {
        Physics.SyncTransforms();
        var hits=Physics.RaycastAll(foot+Vector3.up*.5f,Vector3.down,1.5f,~0,QueryTriggerInteraction.Ignore)
            .Where(h=>!h.collider.transform.IsChildOf(self)).OrderBy(h=>h.distance).ToArray();
        return hits.Length>0?hits[0].point.y+.001f:foot.y;
    }

    // what the player's cursor would pick from a step away and a little above, as PlayerInteraction.TargetAt does
    static string Aim(InteractionObject item)
    {
        var c=item.GetComponent<Collider>().bounds.center;
        var ray=new Ray(c+new Vector3(1.2f,1.1f,0.4f),(c-(c+new Vector3(1.2f,1.1f,0.4f))).normalized);
        var hits=Physics.RaycastAll(ray,5f,~(1<<2),QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).ToArray();
        if(hits.Length==0)return "nothing";
        var first=hits[0].collider.GetComponentInParent<InteractionObject>();
        return first==item?"ok":"blocked by "+hits[0].collider.name;
    }

    // what crafting the rope onto the hook drops: the model in place of its cube, lying on its side
    static void CraftedPrefab()
    {
        const string path="Assets/Interactions/RopeGrapplingHook.prefab";
        if(!AssetDatabase.LoadAssetAtPath<GameObject>(path)){Debug.LogWarning("QUESTITEM no "+path);return;}
        var root=PrefabUtility.LoadPrefabContents(path);
        foreach(var n in new[]{"Visual","Model"}){var v=root.transform.Find(n);if(v)Object.DestroyImmediate(v.gameObject);}
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Item_GrapplingHookRope.fbx"));
        var upright=go.transform.rotation;
        go.name="Model";go.transform.SetParent(root.transform,false);
        go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.Euler(90,0,0)*upright;
        var b=Around(go);go.transform.position+=root.transform.position-b.center;b=Around(go);
        var box=root.GetComponent<BoxCollider>();if(box)Fit(box,root.transform,b);
        PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        Debug.Log($"QUESTITEM {path} <- Item_GrapplingHookRope: {b.size}");
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
