using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

public static class ConfigurePitPickup
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
        var crowbar=GameObject.Find("crowbar").GetComponent<InteractionObject>();
        var mound=GameObject.Find("Земля над ямой");
        var craft=Object.FindFirstObjectByType<RopeHookCrafting>();
        // The visible model was moved on its own. Move the pickup root to it without
        // changing its world-space appearance, so holding and dropping work normally.
        var model=crowbar.transform.Find("Model");
        if(model&&model.localPosition.sqrMagnitude>4)
        {
            var children=crowbar.transform.Cast<Transform>().ToArray();
            var positions=children.Select(t=>t.position).ToArray();
            Undo.RecordObjects(children.Cast<Object>().Append(crowbar.transform).ToArray(),"Correct crowbar pivot");
            crowbar.transform.position=model.position;
            for(int j=0;j<children.Length;j++)children[j].position=positions[j];
        }
        var pit=crowbar.GetComponent<PitPickup>();if(!pit)pit=Undo.AddComponent<PitPickup>(crowbar.gameObject);
        pit.soilCover=mound;pit.rope=craft.rope;pit.hook=craft.hook;
        var opening=GameObject.Find("Pit interaction opening");
        if(!opening){opening=new GameObject("Pit interaction opening");Undo.RegisterCreatedObjectUndo(opening,"Add pit interaction opening");}
        opening.transform.position=new Vector3(19.73f,5.69f,-29.5f);
        var box=opening.GetComponent<BoxCollider>();if(!box)box=opening.AddComponent<BoxCollider>();
        box.isTrigger=true;box.size=new Vector3(1.5f,.3f,1.5f);pit.opening=box;
        pit.raycastPassThrough=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.name=="Plane"&&c.bounds.Contains(opening.transform.position)).ToArray();
        var body=crowbar.GetComponent<Rigidbody>();if(body){Undo.RecordObject(body,"Keep crowbar lodged in pit");body.isKinematic=true;EditorUtility.SetDirty(body);}
        var soil=mound.GetComponent<InteractionObject>();Undo.RecordObject(soil,"Set soil interaction range");soil.reachDistance=3;soil.tooFarHoverText="вы далеко";EditorUtility.SetDirty(soil);
        var path=AssetDatabase.GetAssetPath(craft.resultPrefab);
        var prefab=PrefabUtility.LoadPrefabContents(path);
        try{if(!prefab.GetComponent<RopeHookTool>())prefab.AddComponent<RopeHookTool>();PrefabUtility.SaveAsPrefabAsset(prefab,path);}
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        EditorUtility.SetDirty(pit);EditorUtility.SetDirty(box);
        EditorSceneManager.MarkSceneDirty(mound.scene);EditorSceneManager.SaveScene(mound.scene);AssetDatabase.SaveAssets();
        Selection.activeGameObject=crowbar.gameObject;
    }
}
