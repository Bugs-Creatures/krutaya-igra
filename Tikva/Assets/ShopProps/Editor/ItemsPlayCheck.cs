using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Play-mode check of the quest items' new models: the cursor finds them where they lie, the pliers come
// to hand, and the rope and the hook still make the rope-and-hook, with its model. Batch:
// Unity.exe -batchmode -projectPath Tikva -executeMethod ItemsPlayCheck.Run   (exit code 0 = all passed)
public static class ItemsPlayCheck
{
    const string Key="ItemsPlayCheck.running";
    static float waitUntil;
    static readonly Dictionary<string,bool> r=new();
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        SessionState.SetBool(Key,true);
        Hook();
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
        waitUntil=Time.realtimeSinceStartup+1.5f;EditorApplication.update-=Step;EditorApplication.update+=Step;
    }
    static InteractionObject Item(string name)=>Object.FindObjectsByType<InteractionObject>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(o=>o.name==name);
    static bool Aimed(PlayerInteraction i,InteractionObject item)
    {
        var c=item.GetComponent<Collider>().bounds.center;var from=c+new Vector3(.8f,1.0f,.3f);
        return i.TargetAt(new Ray(from,(c-from).normalized))==item;
    }
    static void Step()
    {
        if(Time.realtimeSinceStartup<waitUntil)return;
        EditorApplication.update-=Step;
        try
        {
            var i=Object.FindAnyObjectByType<PlayerInteraction>();var p=i.player;
            foreach(var n in new[]{"Ключ","Плоскогубцы","Лопата","vodka"})
            {
                var it=Item(n);p.Teleport(it.transform.position+new Vector3(.9f,0,0)-Vector3.up*it.transform.position.y);
                r[n+": under the cursor"]=Aimed(i,it);
                r[n+": still where it lay"]=it.transform.Find("Model")&&it.GetComponent<Collider>().bounds.min.y>-.01f;
            }
            var pliers=Item("Плоскогубцы");p.Teleport(pliers.transform.position+new Vector3(.9f,0,0)-Vector3.up*pliers.transform.position.y);
            r["pliers come to hand"]=i.TryInteract(pliers)&&i.heldItem==pliers;
            i.DropHeld();
            r["key may be taken in first person"]=Item("Ключ").Allowed(true);
            var craft=i.GetComponent<RopeHookCrafting>();
            var rope=craft.rope;var hook=craft.hook;
            rope.gameObject.SetActive(true);hook.gameObject.SetActive(true);
            var spot=new Vector3(2f,0.1f,4f);
            rope.transform.position=spot;hook.transform.position=spot+new Vector3(.6f,0,0);
            Physics.SyncTransforms();
            p.Teleport(spot+new Vector3(.3f,-.1f,-.8f));
            r["rope comes to hand"]=i.TryInteract(rope)&&i.heldItem==rope;
            r["rope onto the hook makes the rope-and-hook"]=i.TryInteract(hook)&&craft.LastCrafted;
            r["the rope-and-hook has its model"]=craft.LastCrafted&&craft.LastCrafted.transform.Find("Model")&&!craft.LastCrafted.transform.Find("Visual");
        }
        catch(System.Exception e){Debug.LogException(e);r["ran without errors"]=false;}
        foreach(var kv in r)Debug.Log("ITEMCHECK "+(kv.Value?"PASS ":"FAIL ")+kv.Key);
        SessionState.EraseBool(Key);
        var failed=r.Values.Any(v=>!v);
        EditorApplication.ExitPlaymode();
        EditorApplication.delayCall+=()=>EditorApplication.Exit(failed?1:0);
    }
}
