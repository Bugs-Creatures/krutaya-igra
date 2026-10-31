using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class InstallZaika
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
        var go=GameObject.Find("Zaika");if(!go)throw new System.Exception("Zaika missing");
        var talk=go.GetComponent<InteractionObject>();if(!talk)talk=go.AddComponent<InteractionObject>();talk.kind=InteractionKind.Talk;talk.displayName="Чудик";talk.suspicious=false;talk.availableHoverText="поговорить";
        if(!go.GetComponentInChildren<Collider>()){var collider=go.AddComponent<CapsuleCollider>();collider.height=1.8f;collider.radius=.3f;collider.center=new Vector3(0,.9f,0);}
        var indicator=go.GetComponent<TalkIndicator>();if(!indicator)indicator=go.AddComponent<TalkIndicator>();indicator.icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Interactions/Cursors/TalkBubble.png");
        var dialogue=go.GetComponent<ZaikaDialogue>();if(!dialogue)dialogue=go.AddComponent<ZaikaDialogue>();
        if(!dialogue.hookReward)
        {
            var root=new GameObject("GrapplingHook");root.transform.position=go.transform.position+Vector3.up;
            var visual=GameObject.CreatePrimitive(PrimitiveType.Cube);visual.name="Visual";visual.transform.SetParent(root.transform,false);visual.transform.localScale=Vector3.one*.25f;Object.DestroyImmediate(visual.GetComponent<Collider>());
            var collider=root.AddComponent<BoxCollider>();collider.size=Vector3.one*.25f;
            var body=root.AddComponent<Rigidbody>();body.mass=.7f;body.isKinematic=true;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            var item=root.AddComponent<InteractionObject>();item.kind=InteractionKind.Pickup;item.displayName="крюк-кошка";item.suspicious=true;
            var outline=root.AddComponent<InteractionOutline>();outline.outlineShader=Shader.Find("Interaction/Outline");
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Interactions/HookPlaceholder.mat");if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=Color.white;AssetDatabase.CreateAsset(material,"Assets/Interactions/HookPlaceholder.mat");}visual.GetComponent<Renderer>().sharedMaterial=material;
            root.SetActive(false);dialogue.hookReward=item;
        }
        EditorUtility.SetDirty(dialogue);EditorUtility.SetDirty(talk);EditorUtility.SetDirty(indicator);EditorSceneManager.MarkSceneDirty(go.scene);EditorSceneManager.SaveScene(go.scene);AssetDatabase.SaveAssets();
    }
}
