using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Colliders that follow the models of everything the player can pick up: each mesh of the item gets a
// convex mesh collider of its own (what a body resting on a floor touches is its convex hull anyway), the
// old boxes round the whole item go. An item resting where it was put (kinematic) is let down onto
// whatever is under it, as its old box no longer holds it up. The bench in the yard, where the key and
// the pliers lie, had convex colliders: one hull over the seat and the back that stood up to 30 cm over
// the seat and hid anything lying on it from the cursor; its meshes are used as they are now. The crafted
// rope-and-hook prefab is fitted too. Safe to run again.
public static class FitPickupColliders
{
    [MenuItem("Tools/Fit pickup colliders")]
    public static void Apply()
    {
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        var bench=GameObject.Find("bench");
        if(bench)foreach(var c in bench.GetComponentsInChildren<MeshCollider>(true))c.convex=false;
        foreach(var item in Object.FindObjectsByType<InteractionObject>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(i=>i.kind==InteractionKind.Pickup))
        {
            Fit(item.gameObject);
            var body=item.GetComponent<Rigidbody>();
            float drop=item.gameObject.activeInHierarchy&&(!body||body.isKinematic)?Settle(item.gameObject):0;
            Debug.Log($"PICKUPCOLLIDERS {item.name}: {item.GetComponentsInChildren<Collider>(true).Length} collider(s), let down {drop*100:0.0} cm");
        }
        const string crafted="Assets/Interactions/RopeGrapplingHook.prefab";
        var root=PrefabUtility.LoadPrefabContents(crafted);Fit(root);PrefabUtility.SaveAsPrefabAsset(root,crafted);PrefabUtility.UnloadPrefabContents(root);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }

    // a convex mesh collider on every mesh of the item, nothing else; the bottle is round and its hull would
    // have more than the 255 faces Unity allows one, so it gets a capsule along its length
    static readonly string[] Round={"vodka"};
    public static void Fit(GameObject item)
    {
        foreach(var c in item.GetComponentsInChildren<Collider>(true).Where(c=>!c.isTrigger))Object.DestroyImmediate(c);
        foreach(var f in item.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh))
        {
            if(Round.Contains(item.name))
            {
                var b=f.sharedMesh.bounds;var capsule=f.gameObject.AddComponent<CapsuleCollider>();
                capsule.direction=b.size.x>=b.size.y&&b.size.x>=b.size.z?0:b.size.y>=b.size.z?1:2;
                capsule.center=b.center;capsule.height=b.size[capsule.direction];
                capsule.radius=Enumerable.Range(0,3).Where(a=>a!=capsule.direction).Max(a=>b.size[a])/2;
                continue;
            }
            var c=f.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;c.convex=true;
        }
    }

    // down onto the nearest surface under any of its vertices (not more than 15 cm); how far it went
    static float Settle(GameObject item)
    {
        Physics.SyncTransforms();
        float gap=float.MaxValue;
        foreach(var f in item.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh))
            foreach(var v in f.sharedMesh.vertices)
            {
                var p=f.transform.TransformPoint(v);
                var under=Physics.RaycastAll(p+Vector3.up*.001f,Vector3.down,.5f,~0,QueryTriggerInteraction.Ignore)
                    .Where(h=>!h.collider.transform.IsChildOf(item.transform)).Select(h=>h.distance-.001f).DefaultIfEmpty(float.MaxValue).Min();
                gap=Mathf.Min(gap,under);
            }
        if(gap<=0||gap>.15f)return 0;
        item.transform.position+=Vector3.down*gap;
        return gap;
    }
}
