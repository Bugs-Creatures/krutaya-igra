using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Puts the vodka-wanting NPC ("drunk") by the bench on the street, with his hidden reward cube.
public static class InstallDrunk
{
    [MenuItem("Tools/Install drunk NPC")]
    public static void Apply()
    {
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        var seller=GameObject.Find("salesman");var vodka=GameObject.Find("vodka");var bench=GameObject.Find("bench");
        if(!seller||!vodka)throw new System.Exception("salesman or vodka missing");
        var drunk=GameObject.Find("drunk");
        if(!drunk){drunk=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Player/Y Bot.fbx"),scene);drunk.name="drunk";}
        Physics.SyncTransforms();
        var spot=bench?bench.transform.position+new Vector3(1.3f,0,-.4f):new Vector3(-.5f,0,8.6f);
        // ground under the spot, skipping the NPC and his reward when the installer runs again
        var ownReward=GameObject.Find("drunk_reward");RaycastHit ground=default;
        var downs=Physics.RaycastAll(spot+Vector3.up*3,Vector3.down,10,~0,QueryTriggerInteraction.Ignore);
        System.Array.Sort(downs,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(var hit in downs)
            if(!hit.transform.IsChildOf(drunk.transform)&&(!ownReward||!hit.transform.IsChildOf(ownReward.transform))){ground=hit;spot.y=hit.point.y;break;}
        drunk.transform.SetPositionAndRotation(spot,Quaternion.Euler(0,180,0));
        // same idle as the cashier, clothes in a different colour
        var sellerAnimator=seller.GetComponent<Animator>();var animator=drunk.GetComponent<Animator>();
        if(sellerAnimator&&animator){animator.runtimeAnimatorController=sellerAnimator.runtimeAnimatorController;animator.applyRootMotion=sellerAnimator.applyRootMotion;}
        var tinted=new Dictionary<Material,Material>();
        var sellerRenderers=seller.GetComponentsInChildren<Renderer>();
        foreach(var r in drunk.GetComponentsInChildren<Renderer>())
        {
            var source=System.Array.Find(sellerRenderers,s=>s.name==r.name);var mats=(source?source:r).sharedMaterials;
            for(int i=0;i<mats.Length;i++)mats[i]=Tinted(mats[i],tinted);
            r.sharedMaterials=mats;
        }
        var body=drunk.GetComponent<CapsuleCollider>();if(!body){body=drunk.AddComponent<CapsuleCollider>();body.center=new Vector3(0,.9f,0);body.height=1.8f;body.radius=.35f;}
        var talk=drunk.GetComponent<InteractionObject>();if(!talk)talk=drunk.AddComponent<InteractionObject>();
        talk.kind=InteractionKind.Talk;talk.suspicious=false;talk.reachDistance=4;talk.availableHoverText="поговорить";talk.displayName="Мужик";
        var sellerIndicator=seller.GetComponent<TalkIndicator>();var indicator=drunk.GetComponent<TalkIndicator>();if(!indicator)indicator=drunk.AddComponent<TalkIndicator>();
        if(sellerIndicator)indicator.icon=sellerIndicator.icon;
        // reward: a pickup cube by his feet, hidden until he gets the vodka
        var rewardObject=GameObject.Find("drunk_reward");
        if(!rewardObject){rewardObject=GameObject.CreatePrimitive(PrimitiveType.Cube);rewardObject.name="drunk_reward";}
        rewardObject.transform.position=spot+drunk.transform.forward*.6f+Vector3.up*.125f;rewardObject.transform.localScale=Vector3.one*.25f;
        var reward=rewardObject.GetComponent<InteractionObject>();if(!reward)reward=rewardObject.AddComponent<InteractionObject>();
        reward.kind=InteractionKind.Pickup;reward.suspicious=false;reward.displayName="Награда";
        if(!rewardObject.GetComponent<Rigidbody>())rewardObject.AddComponent<Rigidbody>();
        var dialogue=drunk.GetComponent<DrunkDialogue>();if(!dialogue)dialogue=drunk.AddComponent<DrunkDialogue>();
        dialogue.wantedItem=vodka.GetComponent<InteractionObject>();dialogue.reward=reward;
        // report what the NPC stands in, so a bad spot shows up in the log
        Physics.SyncTransforms();
        var blockers=new List<string>();
        foreach(var c in Physics.OverlapCapsule(spot+Vector3.up*.4f,spot+Vector3.up*1.5f,.3f,~0,QueryTriggerInteraction.Ignore))
            if(!c.transform.IsChildOf(drunk.transform)&&c.gameObject!=rewardObject)blockers.Add(c.name);
        Debug.Log($"DRUNK at {spot}, ground {(ground.collider?ground.collider.name:"none")}, overlaps [{string.Join(", ",blockers)}], hand {(DrunkDialogue.RightHand(drunk.transform) is Transform h?h.name:"none")}, wanted {dialogue.wantedItem}, materials {tinted.Count}");
        foreach(var o in new Object[]{drunk,talk,indicator,reward,dialogue,body,animator})if(o)EditorUtility.SetDirty(o);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    static Material Tinted(Material source,Dictionary<Material,Material> cache)
    {
        if(!source)return source;
        if(cache.TryGetValue(source,out var copy))return copy;
        var path="Assets/Interactions/Drunk_"+source.name.Replace(" ","_")+".mat";
        copy=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!copy){copy=new Material(source);AssetDatabase.CreateAsset(copy,path);}
        var colour=new Color(.34f,.28f,.22f);
        if(copy.HasProperty("_BaseColor"))copy.SetColor("_BaseColor",colour);else if(copy.HasProperty("_Color"))copy.SetColor("_Color",colour);
        EditorUtility.SetDirty(copy);cache[source]=copy;return copy;
    }
}
