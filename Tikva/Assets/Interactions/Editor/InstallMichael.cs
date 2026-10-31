using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
public static class InstallMichael
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
        var go=GameObject.Find("Michael");if(!go)throw new System.Exception("Michael missing");
        var cc=go.GetComponent<CharacterController>();if(!cc)cc=go.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.3f;cc.center=new Vector3(0,.9f,0);cc.stepOffset=.25f;
        var npc=go.GetComponent<MichaelBehaviour>();if(!npc)npc=go.AddComponent<MichaelBehaviour>();npc.animator=go.GetComponentInChildren<Animator>();npc.animator.applyRootMotion=false;
        npc.waypoints=Enumerable.Range(1,4).Select(n=>GameObject.Find("wpos"+n).transform).ToArray();
        var target=go.GetComponent<InteractionObject>();if(!target)target=go.AddComponent<InteractionObject>();target.kind=InteractionKind.Talk;target.displayName="Майкл";target.suspicious=false;target.availableHoverText="поговорить";target.reachDistance=3;
        var icon=go.GetComponent<TalkIndicator>();if(!icon)icon=go.AddComponent<TalkIndicator>();icon.icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Interactions/Cursors/TalkBubble.png");
        var map=npc.mapReward;
        if(!map)
        {
            var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.name="LocationMap";item.transform.position=go.transform.position+Vector3.up;item.transform.localScale=new Vector3(.35f,.025f,.25f);
            item.AddComponent<Rigidbody>().isKinematic=true;
            map=item.AddComponent<InteractionObject>();map.kind=InteractionKind.Pickup;map.suspicious=false;map.displayName="карта локации";map.availableHoverText="посмотреть карту";
            item.AddComponent<MapItem>();npc.mapReward=map;
        }
        map.GetComponent<MapItem>().mapImage=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Interactions/map.jpg");map.gameObject.SetActive(false);
        EditorUtility.SetDirty(npc);EditorUtility.SetDirty(target);EditorUtility.SetDirty(icon);EditorUtility.SetDirty(cc);EditorUtility.SetDirty(npc.animator);EditorUtility.SetDirty(map);EditorUtility.SetDirty(map.GetComponent<MapItem>());
        EditorSceneManager.MarkSceneDirty(go.scene);EditorSceneManager.SaveScene(go.scene);
    }
}
