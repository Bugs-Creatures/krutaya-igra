using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class InstallRopeHookCrafting
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
        var player=Object.FindAnyObjectByType<PlayerInteraction>();var hook=Object.FindAnyObjectByType<ZaikaDialogue>().hookReward;var rope=Object.FindAnyObjectByType<DrunkDialogue>().reward;
        Undo.RecordObject(hook,"Make hook ordinary");hook.suspicious=false;EditorUtility.SetDirty(hook);
        string path="Assets/Interactions/RopeGrapplingHook.prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!prefab)
        {
            var root=new GameObject("RopeGrapplingHook");var visual=GameObject.CreatePrimitive(PrimitiveType.Cube);visual.name="Visual";visual.transform.SetParent(root.transform,false);visual.transform.localScale=Vector3.one*.25f;Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Interactions/HookPlaceholder.mat");
            root.AddComponent<BoxCollider>().size=Vector3.one*.25f;var body=root.AddComponent<Rigidbody>();body.mass=.8f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            var item=root.AddComponent<InteractionObject>();item.kind=InteractionKind.Pickup;item.suspicious=true;item.displayName="Крюк-кошка на верёвке";item.hoverTextSize=14;
            root.AddComponent<InteractionOutline>().outlineShader=Shader.Find("Interaction/Outline");
            prefab=PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);
        }
        var craft=player.GetComponent<RopeHookCrafting>();if(!craft)craft=Undo.AddComponent<RopeHookCrafting>(player.gameObject);
        craft.rope=rope;craft.hook=hook;craft.resultPrefab=prefab.GetComponent<InteractionObject>();
        EditorUtility.SetDirty(craft);EditorSceneManager.MarkSceneDirty(player.gameObject.scene);EditorSceneManager.SaveScene(player.gameObject.scene);AssetDatabase.SaveAssets();
    }
}
