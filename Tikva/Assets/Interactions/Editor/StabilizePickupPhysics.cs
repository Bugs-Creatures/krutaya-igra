using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class StabilizePickupPhysics
{
    [MenuItem("Tools/Interactions/Stabilize pickup physics")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
        foreach(var item in Object.FindObjectsByType<InteractionObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(item.kind!=InteractionKind.Pickup)continue;
            Configure(item);
        }
        var crafted=PrefabUtility.LoadPrefabContents("Assets/Interactions/RopeGrapplingHook.prefab");
        try{Configure(crafted.GetComponent<InteractionObject>());PrefabUtility.SaveAsPrefabAsset(crafted,"Assets/Interactions/RopeGrapplingHook.prefab");}
        finally{PrefabUtility.UnloadPrefabContents(crafted);}
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
    }
    static void Configure(InteractionObject item)
    {
        var colliders=PickupPhysics.PrepareColliders(item.gameObject,true);
        foreach(var collider in colliders)EditorUtility.SetDirty(collider);
        var body=item.GetComponent<Rigidbody>();
        if(body){Undo.RecordObject(body,"Stable pickup collisions");PickupPhysics.ConfigureBody(body);EditorUtility.SetDirty(body);}
    }
}
