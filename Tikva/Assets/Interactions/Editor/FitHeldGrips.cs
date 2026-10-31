using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// How each item sits in the hand, from the shape of its model: the long axis of the mesh, its working end
// (the narrow end of a key, a screwdriver or the cutters, the wide end of a shovel, a crowbar or the hook)
// and its flat side. First person (the anchor before the camera, low right: x right, y up, z ahead): tools
// point ahead and a little up and left, the shovel's blade ahead and down, the bottle stands, the coil faces
// the eye. Third person (the right hand bone, its y along the fingers, its -x the way the player faces):
// tools stick out ahead and a little down from the fist, the bottle stands, the coil hangs. Safe to run again.
public static class FitHeldGrips
{
    enum End { Narrow, Wide }
    // item, working end, first person: end towards, flat side towards, shift; third person: end towards, flat side towards, shift
    static readonly (string item, End end, Vector3 fpEnd, Vector3 fpFlat, float fpShift, Vector3 tpEnd, Vector3 tpFlat, float tpShift)[] Grips =
    {
        ("Ключ", End.Narrow, new(-.25f,.35f,1), new(0,.5f,-1), 0, new(-1,.35f,0), new(0,0,1), .03f),
        ("Плоскогубцы", End.Narrow, new(-.2f,.3f,1), new(0,.6f,-1), 0, new(-1,.35f,0), new(0,0,1), .04f),
        ("screwdriver", End.Narrow, new(-.25f,.35f,1), new(0,1,0), .03f, new(-1,.35f,0), new(0,0,1), .06f),
        ("Лопата", End.Wide, new(-.3f,-.25f,1), new(0,1,.2f), .35f, new(-1,.6f,0), new(0,0,1), .45f),
        ("crowbar", End.Wide, new(-.25f,.3f,1), new(1,0,.2f), .2f, new(-1,.35f,0), new(0,0,1), .2f),
        ("vodka", End.Narrow, Vector3.zero, Vector3.zero, 0, new(0,-1,0), new(0,0,1), 0),   // first person: as it was, upright, the label to the eye
        ("rope", End.Wide, new(1,0,0), new(.2f,.3f,-1), 0, new(0,1,0), new(0,0,1), 0),
        ("GrapplingHook", End.Wide, new(-.1f,.7f,1), new(1,0,0), .05f, new(-1,-.4f,0), new(0,0,1), .08f),
    };
    const string Crafted="Assets/Interactions/RopeGrapplingHook.prefab";

    [MenuItem("Tools/Fit held grips")]
    public static void Apply()
    {
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        var hand=Object.FindAnyObjectByType<PlayerInteraction>().rightHand;
        var items=Object.FindObjectsByType<InteractionObject>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        foreach(var g in Grips)
        {
            var item=items.FirstOrDefault(i=>i.name==g.item);
            if(!item){Debug.LogWarning("HELDGRIPS no "+g.item);continue;}
            Fit(item,g,hand.lossyScale.x);EditorUtility.SetDirty(item);
        }
        var root=PrefabUtility.LoadPrefabContents(Crafted);   // the rope-and-hook as the hook
        Fit(root.GetComponent<InteractionObject>(),Grips.First(g=>g.item=="GrapplingHook"),hand.lossyScale.x);
        PrefabUtility.SaveAsPrefabAsset(root,Crafted);PrefabUtility.UnloadPrefabContents(root);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }

    static void Fit(InteractionObject item,(string item,End end,Vector3 fpEnd,Vector3 fpFlat,float fpShift,Vector3 tpEnd,Vector3 tpFlat,float tpShift) g,float handScale)
    {
        // the model's vertices in the item's own space, at world size
        var t=item.transform;var s=t.lossyScale;
        var pts=item.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh).SelectMany(f=>f.sharedMesh.vertices.Select(v=>Vector3.Scale(t.InverseTransformPoint(f.transform.TransformPoint(v)),s))).ToArray();
        Vector3 lo=pts.Aggregate(Vector3.Min),hi=pts.Aggregate(Vector3.Max),size=hi-lo,centre=(lo+hi)/2;
        int longAxis=size.x>=size.y&&size.x>=size.z?0:size.y>=size.z?1:2;
        int flatAxis=Enumerable.Range(0,3).Where(a=>a!=longAxis).OrderBy(a=>size[a]).First();
        // how wide each end is, across the long axis: the last sixth of the length at either end
        float Width(float from,float to)=>pts.Where(p=>p[longAxis]>=from&&p[longAxis]<=to)
            .Select(p=>Enumerable.Range(0,3).Where(a=>a!=longAxis).Max(a=>Mathf.Abs(p[a]-centre[a]))).DefaultIfEmpty(0).Max();
        float l6=size[longAxis]/6;
        bool highIsWide=Width(hi[longAxis]-l6,hi[longAxis])>Width(lo[longAxis],lo[longAxis]+l6);
        var along=Vector3.zero;along[longAxis]=(highIsWide==(g.end==End.Wide))?1:-1;
        var flat=Vector3.zero;flat[flatAxis]=1;
        Quaternion Turn(Vector3 end,Vector3 side)=>Quaternion.LookRotation(end.normalized,side)*Quaternion.Inverse(Quaternion.LookRotation(along,flat));
        var fp=g.fpEnd!=Vector3.zero?Turn(g.fpEnd,g.fpFlat):Quaternion.identity;var tp=Turn(g.tpEnd,g.tpFlat);
        // the middle of the model at the anchor, moved towards the working end by the shift
        if(g.fpEnd!=Vector3.zero){item.firstPersonHeldRotation=fp.eulerAngles;item.firstPersonHeldPosition=g.fpEnd.normalized*g.fpShift-fp*centre;}
        item.thirdPersonHeldRotation=tp.eulerAngles;
        item.thirdPersonHeldPosition=(Vector3.up*.07f+g.tpEnd.normalized*g.tpShift-tp*centre)/handScale;   // into the palm, along the fingers
        Debug.Log($"HELDGRIPS {item.name}: long axis {longAxis} flat {flatAxis} working end {(along[longAxis]>0?"+":"-")} size {size} fp {item.firstPersonHeldRotation} tp {item.thirdPersonHeldRotation}");
    }
}
