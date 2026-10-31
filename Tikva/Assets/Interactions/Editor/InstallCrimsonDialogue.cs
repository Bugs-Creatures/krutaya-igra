using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class InstallCrimsonDialogue
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
        var go=GameObject.Find("CrimsonMass_Idle");if(!go)throw new System.Exception("CrimsonMass_Idle missing");
        var target=go.GetComponent<InteractionObject>();if(!target)target=go.AddComponent<InteractionObject>();target.kind=InteractionKind.Talk;target.displayName="???";target.suspicious=false;target.reachDistance=4;target.hoverTextSize=14;target.availableHoverText="поговорить";
        if(!go.GetComponentInChildren<Collider>())
        {
            var renderers=go.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            var collider=go.AddComponent<BoxCollider>();collider.center=go.transform.InverseTransformPoint(bounds.center);var scale=go.transform.lossyScale;collider.size=new Vector3(bounds.size.x/Mathf.Abs(scale.x),bounds.size.y/Mathf.Abs(scale.y),bounds.size.z/Mathf.Abs(scale.z));
        }
        var indicator=go.GetComponent<TalkIndicator>();if(!indicator)indicator=go.AddComponent<TalkIndicator>();indicator.icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Interactions/Cursors/TalkBubble.png");
        var dialogue=go.GetComponent<CrimsonMassDialogue>();if(!dialogue)dialogue=go.AddComponent<CrimsonMassDialogue>();
        if(!dialogue.axeReward)
        {
            var root=new GameObject("Axe");root.transform.position=go.transform.position+Vector3.up;
            var visual=GameObject.CreatePrimitive(PrimitiveType.Cube);visual.name="Visual";visual.transform.SetParent(root.transform,false);visual.transform.localScale=new Vector3(.22f,.22f,.22f);Object.DestroyImmediate(visual.GetComponent<Collider>());visual.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Interactions/HookPlaceholder.mat");
            root.AddComponent<BoxCollider>().size=Vector3.one*.22f;var body=root.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
            var item=root.AddComponent<InteractionObject>();item.kind=InteractionKind.Pickup;item.displayName="Топор";item.suspicious=false;item.hoverTextSize=14;root.AddComponent<InteractionOutline>().outlineShader=Shader.Find("Interaction/Outline");root.SetActive(false);dialogue.axeReward=item;
        }
        EditorUtility.SetDirty(dialogue);EditorUtility.SetDirty(target);EditorUtility.SetDirty(indicator);EditorSceneManager.MarkSceneDirty(go.scene);EditorSceneManager.SaveScene(go.scene);
    }
}
