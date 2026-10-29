using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MissingAssetRecovery
{
    [MenuItem("Tools/North Blockout/Restore missing wall meshes")]
    public static void Restore()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
        string folder="Assets/NorthBlockout/WallMeshes";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/NorthBlockout","WallMeshes");
        var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);
        var cube=temp.GetComponent<MeshFilter>().sharedMesh;
        int count=0;
        foreach(var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include))
        {
            if(filter.sharedMesh||!filter.name.StartsWith("Partition_"))continue;
            var boxes=filter.GetComponents<BoxCollider>();
            if(boxes.Length==0)throw new System.InvalidOperationException("No surviving colliders: "+filter.name);
            var combine=new List<CombineInstance>();
            foreach(var box in boxes)combine.Add(new CombineInstance {mesh=cube,transform=Matrix4x4.TRS(box.center,Quaternion.identity,box.size)});
            var mesh=new Mesh{name=filter.name};mesh.CombineMeshes(combine.ToArray(),true,true);
            AssetDatabase.CreateAsset(mesh,folder+"/"+filter.name+".asset");
            Undo.RecordObject(filter,"Restore wall mesh");filter.sharedMesh=mesh;EditorUtility.SetDirty(filter);count++;
        }
        Object.DestroyImmediate(temp);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Restored wall meshes: "+count);
    }
}

