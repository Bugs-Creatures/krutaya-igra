using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
public static class InstallBasementInteractions
{
    static GameObject Visual(GameObject root,string name)
    {
        var found=root.transform.Find(name);if(found)return found.gameObject;
        var visual=new GameObject(name);visual.transform.SetParent(root.transform,false);
        var filter=root.GetComponent<MeshFilter>();var renderer=root.GetComponent<MeshRenderer>();
        if(filter){visual.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;Object.DestroyImmediate(filter);}
        if(renderer){visual.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;Object.DestroyImmediate(renderer);}
        return visual;
    }
    static InteractionObject Target(GameObject go,InteractionKind kind,bool suspicious,string text)
    {
        var item=go.GetComponent<InteractionObject>();if(!item)item=go.AddComponent<InteractionObject>();
        item.kind=kind;item.displayName=go.name;item.suspicious=suspicious;item.hoverTextSize=14;item.availableHoverText=text;
        var outline=go.GetComponent<InteractionOutline>();if(!outline)outline=go.AddComponent<InteractionOutline>();outline.outlineShader=Shader.Find("Interaction/Outline");
        EditorUtility.SetDirty(item);EditorUtility.SetDirty(outline);return item;
    }
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
        foreach(var name in new[]{"Ключ","Лопата","Плоскогубцы"})
        {
            var go=GameObject.Find(name);if(!go)throw new System.Exception(name+" missing");
            Visual(go,"Visual");Target(go,InteractionKind.Pickup,name!="Плоскогубцы","подобрать");
            if(!go.GetComponent<Collider>())go.AddComponent<BoxCollider>();
            if(!go.GetComponent<Rigidbody>()){var body=go.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;}
        }
        var chainObject=GameObject.Find("Цепи");var doorObject=GameObject.Find("Дверь в подвал");var point=GameObject.Find("teleport place");
        if(!chainObject||!doorObject||!point)throw new System.Exception("Chains, door or destination missing");
        Target(chainObject,InteractionKind.ToolAction,true,"сломать цепи");var intact=Visual(chainObject,"IntactVisual");
        var broken=chainObject.transform.Find("BrokenVisual");
        if(!broken)
        {
            broken=new GameObject("BrokenVisual").transform;broken.SetParent(chainObject.transform,false);
            for(int n=0;n<2;n++){var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name="Broken piece "+(n+1);cube.transform.SetParent(broken,false);cube.transform.localPosition=new Vector3(n==0?-.3f:.3f,0,.1f);cube.transform.localScale=new Vector3(.25f,1,.6f);cube.transform.localRotation=Quaternion.Euler(0,n==0?-20:20,0);Object.DestroyImmediate(cube.GetComponent<Collider>());var original=intact.GetComponent<Renderer>();if(original)cube.GetComponent<Renderer>().sharedMaterials=original.sharedMaterials;}
        }
        broken.gameObject.SetActive(false);intact.SetActive(true);
        var chains=chainObject.GetComponent<ToolActionTarget>();if(!chains)chains=chainObject.AddComponent<ToolActionTarget>();
        chains.action=ToolActionKind.BreakChains;chains.requiredTool=Object.FindObjectsByType<InteractionObject>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(o=>o.name.ToLowerInvariant()=="crowbar");chains.missingToolText="руками не сломать";chains.intactVisual=intact;chains.brokenVisual=broken.gameObject;
        Target(doorObject,InteractionKind.ToolAction,false,"войти");var door=doorObject.GetComponent<ToolActionTarget>();if(!door)door=doorObject.AddComponent<ToolActionTarget>();
        door.action=ToolActionKind.EnterBasement;door.requiredTool=GameObject.Find("Ключ").GetComponent<InteractionObject>();door.missingToolText="закрыто на ключ";door.chains=chains;door.destination=point.transform;
        EditorUtility.SetDirty(chains);EditorUtility.SetDirty(door);EditorSceneManager.MarkSceneDirty(doorObject.scene);EditorSceneManager.SaveScene(doorObject.scene);
    }
}

