using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Play-mode check of the vodka NPC, runs on its own in batch mode:
// Unity.exe -batchmode -projectPath Tikva -executeMethod DrunkPlayCheck.Run   (exit code 0 = all passed)
public static class DrunkPlayCheck
{
    const string Key="DrunkPlayCheck.running";
    static int stage;
    static float waitUntil;
    static Vector3 rewardStart;
    static readonly Dictionary<string,bool> r=new();
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        SessionState.SetBool(Key,true);
        Hook();   // the project enters play mode without a domain reload: subscribe now
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    static void Hook()
    {
        if(!SessionState.GetBool(Key,false))return;
        EditorApplication.playModeStateChanged-=OnState;EditorApplication.playModeStateChanged+=OnState;
    }
    static void OnState(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode)return;
        stage=0;waitUntil=Time.realtimeSinceStartup+1;EditorApplication.update-=Step;EditorApplication.update+=Step;
    }
    static void Step()
    {
        if(Time.realtimeSinceStartup<waitUntil)return;
        try
        {
            var i=Object.FindAnyObjectByType<PlayerInteraction>();var p=i.player;var d=Object.FindAnyObjectByType<DrunkDialogue>();
            var vodka=d.wantedItem;var reward=d.reward;var front=d.transform.position+d.transform.forward*1.2f;
            if(stage==0)
            {
                r["reward hidden at start"]=!reward.gameObject.activeSelf;
                p.Teleport(front);d.Talk();
                r["without vodka: he asks, no choice"]=!d.Choosing&&!p.dialogueLocked;
                Object.FindAnyObjectByType<ShopDialogue>().paid=true;
                p.Teleport(vodka.transform.position+Vector3.down*1.2f);
                r["vodka picked up in the shop"]=i.TryInteract(vodka)&&i.heldItem==vodka;
                p.Teleport(front);d.Talk();
                r["with vodka: choice offered, player held"]=d.Choosing&&p.dialogueLocked;
                typeof(DrunkDialogue).GetMethod("Choose",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(d,new object[]{true});
                r["vodka left the player's hands"]=i.heldItem==null&&p.currentHeldItem=="";
                r["bottle in his right hand"]=vodka.IsHeld&&vodka.transform.parent==DrunkDialogue.RightHand(d);
                r["given, player free again"]=d.given&&!d.Choosing&&!p.dialogueLocked;
                r["reward appeared"]=reward.gameObject.activeSelf;
                rewardStart=reward.transform.position;stage=1;waitUntil=Time.realtimeSinceStartup+3;return;
            }
            var at=reward.transform.position;
            r["reward fell to the ground"]=at.y<rewardStart.y-.4f&&at.y>d.transform.position.y-.05f;
            p.Teleport(new Vector3(at.x,d.transform.position.y,at.z)+d.transform.right*.8f);
            r["reward can be picked up"]=i.TryInteract(reward)&&i.heldItem==reward;
            d.Talk();
            r["afterwards: no choice again"]=!d.Choosing;
            Debug.Log($"DRUNKCHECK reward landed at {at}");
        }
        catch(System.Exception e){Debug.LogException(e);r["no exception"]=false;}
        EditorApplication.update-=Step;SessionState.SetBool(Key,false);
        bool ok=true;
        foreach(var kv in r){Debug.Log($"DRUNKCHECK {(kv.Value?"PASS":"FAIL")} {kv.Key}");ok&=kv.Value;}
        EditorApplication.Exit(ok?0:1);
    }
}
