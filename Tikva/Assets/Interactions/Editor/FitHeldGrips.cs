using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FitHeldGrips
{
    // Model-space working axis, fraction from the butt to the grip, and independent presentation axes. The
    // pliers (end-cutting nippers, head along -Z), the screwdriver (tip along +Z) and the crowbar (hook along +Z)
    // are the textured QuestTools models.
    static readonly (string name,Vector3 axis,Vector3 flat,float grip,Vector3 tp,Vector3 fp,Vector3 fpFlat,float fpScale)[] Poses={
        ("Ключ",Vector3.left,Vector3.up,.17f,new(-1,.12f,0),new(-.65f,.65f,.2f),new(0,0,-1),1),
        ("Плоскогубцы",Vector3.back,Vector3.up,.24f,new(-1,.12f,0),new(-.45f,.8f,.25f),new(0,0,-1),1),
        ("screwdriver",Vector3.forward,Vector3.up,.21f,new(-1,.12f,0),new(-.35f,.85f,.3f),new(0,0,-1),1),
        ("Лопата",Vector3.back,Vector3.up,.16f,new(-1,.15f,0),new(-.42f,.86f,.25f),Vector3.forward,.68f),
        ("crowbar",Vector3.forward,Vector3.up,.22f,new(-1,.12f,0),new(-.35f,.9f,.25f),new(-1,0,0),.9f),
        ("vodka",Vector3.up,Vector3.back,.35f,new(-1,.12f,0),Vector3.up,Vector3.back,1),
        ("rope",Vector3.right,Vector3.up,.12f,Vector3.up,new(.95f,.2f,0),Vector3.back,1),
        ("Axe",Vector3.forward,Vector3.up,.18f,new(-1,.12f,0),new(-.25f,.96f,.08f),Vector3.left,1),
        ("GrapplingHook",Vector3.forward,Vector3.up,.2f,new(-1,.12f,0),new(-.3f,.8f,.35f),new(1,0,0),1),
        ("RopeGrapplingHook",Vector3.forward,Vector3.up,.18f,new(-1,.12f,0),new(-.3f,.8f,.35f),new(1,0,0),.85f)
    };
    const string Crafted="Assets/Interactions/RopeGrapplingHook.prefab";
    public static void ApplyItems(string[] names)
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
        var interaction=Object.FindAnyObjectByType<PlayerInteraction>();
        var items=Object.FindObjectsByType<InteractionObject>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        foreach(var item in items.Where(i=>names.Contains(i.name)))
        {
            Undo.RecordObject(item,"Adjust held presentation");
            Fit(item,interaction.rightHand,interaction.firstPersonAnchor);EditorUtility.SetDirty(item);
        }
        EditorSceneManager.MarkSceneDirty(interaction.gameObject.scene);EditorSceneManager.SaveScene(interaction.gameObject.scene);
    }
    public static Vector3[] ModelPoints(InteractionObject item)
    {
        var root=item.transform;var scale=root.lossyScale;
        return item.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&f.name!="Interaction outline")
            .SelectMany(f=>f.sharedMesh.vertices.Select(v=>Vector3.Scale(root.InverseTransformPoint(f.transform.TransformPoint(v)),scale))).ToArray();
    }
    static void CentreMisplacedPivot(InteractionObject item)
    {
        var points=ModelPoints(item);if(points.Length==0)return;
        var centre=(points.Aggregate(Vector3.Min)+points.Aggregate(Vector3.Max))/2;
        if(centre.magnitude<2)return;
        var root=item.transform;var children=root.Cast<Transform>().ToArray();
        var positions=children.Select(t=>t.position).ToArray();
        var colliders=root.GetComponents<BoxCollider>();var centres=colliders.Select(c=>root.TransformPoint(c.center)).ToArray();
        Undo.RecordObjects(new Object[]{root}.Concat(children).Concat(colliders).ToArray(),"Centre item pivot");
        root.position+=root.rotation*centre;
        for(int n=0;n<children.Length;n++)children[n].position=positions[n];
        for(int n=0;n<colliders.Length;n++)colliders[n].center=root.InverseTransformPoint(centres[n]);
    }
    [MenuItem("Tools/Fit held grips")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
        var interaction=Object.FindAnyObjectByType<PlayerInteraction>();var hand=interaction.rightHand;
        var handPose=interaction.GetComponent<HeldItemHandPose>();
        if(!handPose)handPose=interaction.gameObject.AddComponent<HeldItemHandPose>();
        handPose.interaction=interaction;EditorUtility.SetDirty(handPose);
        var items=Object.FindObjectsByType<InteractionObject>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        foreach(var item in items.Where(i=>i.kind==InteractionKind.Pickup&&!i.GetComponent<MapItem>()))
        {
            CentreMisplacedPivot(item);Fit(item,hand,interaction.firstPersonAnchor);EditorUtility.SetDirty(item);
        }
        var root=PrefabUtility.LoadPrefabContents(Crafted);
        try{Fit(root.GetComponent<InteractionObject>(),hand,interaction.firstPersonAnchor);PrefabUtility.SaveAsPrefabAsset(root,Crafted);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        EditorSceneManager.MarkSceneDirty(interaction.gameObject.scene);EditorSceneManager.SaveScene(interaction.gameObject.scene);
    }
    static void Fit(InteractionObject item,Transform hand,Transform fpAnchor)
    {
        var g=Poses.FirstOrDefault(g=>g.name==item.name);
        if(g.name==null)throw new System.InvalidOperationException("Missing held pose for "+item.name);
        var pts=ModelPoints(item);var lo=pts.Aggregate(Vector3.Min);var hi=pts.Aggregate(Vector3.Max);var centre=(lo+hi)/2;
        float min=pts.Min(p=>Vector3.Dot(p,g.axis)),max=pts.Max(p=>Vector3.Dot(p,g.axis));
        var grip=centre+g.axis*(Mathf.Lerp(min,max,g.grip)-Vector3.Dot(centre,g.axis));
        Quaternion Rotation(Vector3 end,Vector3 face)=>Quaternion.LookRotation(end,face)*Quaternion.Inverse(Quaternion.LookRotation(g.axis,g.flat));
        var tp=Rotation(g.tp,Vector3.forward);var fp=Rotation(g.fp,g.fpFlat);
        var handScale=hand.lossyScale;
        item.thirdPersonHeldRotation=tp.eulerAngles;
        item.thirdPersonHeldPosition=new Vector3(-.008f,.015f,.025f)-new Vector3((tp*grip).x/handScale.x,(tp*grip).y/handScale.y,(tp*grip).z/handScale.z);
        item.firstPersonHeldRotation=fp.eulerAngles;item.firstPersonHeldScale=g.fpScale;
        var rotated=pts.Select(p=>fp*(p-centre)*g.fpScale).ToArray();
        float tan=Mathf.Tan(34*Mathf.Deg2Rad),aspect=16f/9;Vector3 displayCentre=Vector3.zero;
        for(float distance=.28f;distance<2;distance+=.01f)
        {
            displayCentre=new Vector3(.44f*tan*aspect*distance,-.4f*tan*distance,distance);
            bool Fits(Vector3 point)
            {
                var v=point+displayCentre;if(v.z<.12f)return false;
                float x=.5f+v.x/(2*tan*aspect*v.z),y=.5f+v.y/(2*tan*v.z);
                return x>=.50f&&x<=.92f&&y>=.07f&&y<=.75f;
            }
            if(rotated.All(Fits))break;
        }
        // Long tools intentionally extend below the view: show their working heads close to the eye.
        if(item.name=="Axe")displayCentre=new Vector3(.25f,-.23f,.52f);
        if(item.name=="Лопата")displayCentre=new Vector3(.37f,-.34f,.58f);
        item.firstPersonHeldPosition=displayCentre-fp*centre*g.fpScale-fpAnchor.localPosition;
    }
}
