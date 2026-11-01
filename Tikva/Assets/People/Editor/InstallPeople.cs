using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The people of GameScene (the player, the salesman, the drunk, Michael, Zaika) were Mixamo's Y Bot; they
// become the team's HumGen man (Assets/People/T-Pose.fbx), rigged by Mixamo too: the same bone names and
// axes, so the generic animations (bone turns and the height of the hips) drive him as they drove the Y Bot.
// For each of them the Y Bot's meshes and skeleton go and the HumGen meshes and skeleton take their place
// under the same Animator; whatever hung on a Y Bot bone (the player's right hand grip) moves to the same
// bone of the new skeleton, and the player's list of body renderers (hidden in first person) follows. His
// legs are a little longer than the Y Bot's, so the skeleton is a touch smaller, or his feet would sink into
// the ground the 2 cm. Everyone but the player gets a T-shirt, trousers and skin of his own, and the model
// has no hair and one face, so they are told apart by what they wear on the head (Assets/People/Accessories,
// built round this model's head in its own space and hung on the head bone). The man standing in T-pose in
// the street is switched off. Safe to run again.
public static class InstallPeople
{
    const string Model="Assets/People/T-Pose.fbx",Variants="Assets/People/Variants/",Accessories="Assets/People/Accessories/";
    const float Size=.972f;   // the Y Bot's hips-to-ankle over the HumGen man's
    // person: T-shirt, trousers, skin (multiplying the textures; null keeps the model's own), on the head
    static readonly (string person,Color? shirt,Color? trousers,Color? skin,string[] head)[] People =
    {
        ("Player", null, null, null, new string[0]),
        ("salesman", new Color(.30f,.43f,.66f), new Color(.38f,.36f,.34f), new Color(1f,.93f,.88f), new[]{"Acc_ClerkCap"}),
        ("drunk", new Color(.42f,.42f,.30f), new Color(.30f,.28f,.25f), new Color(1f,.84f,.80f), new string[0]),
        ("Michael", new Color(.62f,.16f,.14f), new Color(.30f,.34f,.48f), new Color(.92f,.80f,.70f), new[]{"Acc_Moustache"}),
        ("Zaika", new Color(.24f,.46f,.32f), new Color(.52f,.50f,.46f), new Color(1f,.96f,.93f), new[]{"Acc_Glasses"}),
    };
    // accessory materials: colour, metallic, smoothness
    static readonly Dictionary<string,(Color colour,float metal,float smooth)> Cloth=new()
    {
        ["Acc_Hair"]=(new Color(.16f,.11f,.08f),0,.25f),
        ["Acc_Frame"]=(new Color(.08f,.07f,.06f),.6f,.6f),["Acc_White"]=(new Color(.92f,.92f,.90f),0,.2f),
    };

    [MenuItem("Tools/Dress people")]
    public static void Apply()
    {
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        var animators=Object.FindObjectsByType<Animator>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var headRest=model.GetComponentsInChildren<Transform>(true).First(t=>t.name=="mixamorig:Head").localToWorldMatrix;
        foreach(var (person,shirt,trousers,skin,headWear) in People)
        {
            var animator=animators.FirstOrDefault(a=>a.name==person);
            if(!animator){Debug.LogWarning("PEOPLE no "+person);continue;}
            var root=animator.transform;
            if(PrefabUtility.GetOutermostPrefabInstanceRoot(root.gameObject)==root.gameObject)
                PrefabUtility.UnpackPrefabInstance(root.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            // what hangs on the old bones, by the name of its bone
            var hung=root.GetComponentsInChildren<Transform>(true).Where(t=>t.parent&&t.parent.name.StartsWith("mixamorig:")&&!t.name.StartsWith("mixamorig:")&&!t.name.StartsWith("Acc_"))
                .Select(t=>(t,bone:t.parent.name)).ToList();
            foreach(var (t,_) in hung)t.SetParent(root,false);
            foreach(var old in root.Cast<Transform>().Where(t=>t.name=="mixamorig:Hips"||t.GetComponent<SkinnedMeshRenderer>()).ToArray())Object.DestroyImmediate(old.gameObject);
            var man=(GameObject)PrefabUtility.InstantiatePrefab(model,scene);
            PrefabUtility.UnpackPrefabInstance(man,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            man.transform.SetParent(root,false);man.transform.localPosition=Vector3.zero;man.transform.localRotation=Quaternion.identity;man.transform.localScale=Vector3.one;
            foreach(var part in man.transform.Cast<Transform>().ToArray())part.SetParent(root,false);
            Object.DestroyImmediate(man);
            root.Find("mixamorig:Hips").localScale=Vector3.one*Size;
            var bones=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("mixamorig:")).ToDictionary(t=>t.name);
            foreach(var (t,bone) in hung)t.SetParent(bones[bone],false);
            var renderers=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach(var r in renderers)
            {
                var mats=r.sharedMaterials;
                for(int i=0;i<mats.Length;i++)
                {
                    var tint=mats[i].name.Contains("TSHIRT")?shirt:mats[i].name.Contains("Chinos")?trousers:mats[i].name=="_Human"?skin:null;
                    if(tint!=null)mats[i]=Variant(mats[i],person,tint.Value);
                }
                r.sharedMaterials=mats;
            }
            foreach(var name in headWear)   // where it sits on the model's head at rest, kept on this head bone
            {
                var acc=(GameObject)PrefabUtility.InstantiatePrefab(Accessory(name),scene);
                var local=headRest.inverse*acc.transform.localToWorldMatrix;
                acc.transform.SetParent(bones["mixamorig:Head"],false);
                acc.transform.localPosition=local.GetColumn(3);acc.transform.localRotation=local.rotation;acc.transform.localScale=local.lossyScale;
            }
            var player=root.GetComponent<SurvivalPlayerController>();
            if(player){player.modelRenderers=renderers.Cast<Renderer>().ToArray();EditorUtility.SetDirty(player);}
            animator.Rebind();
            Debug.Log($"PEOPLE {person}: {renderers.Length} meshes, {hung.Count} things moved onto the new bones ({string.Join(", ",hung.Select(h=>h.t.name))})");
        }
        var street=GameObject.Find("T-Pose");
        if(street&&!street.GetComponentInParent<Animator>())street.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }

    // an accessory's model, its materials on the project's own (flat colours, URP Lit)
    static GameObject Accessory(string name)
    {
        var path=Accessories+name+".fbx";var mi=(ModelImporter)AssetImporter.GetAtPath(path);
        mi.animationType=ModelImporterAnimationType.None;mi.importAnimation=false;mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        foreach(var kv in Cloth)
        {
            var mp=Accessories+kv.Key+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(mp);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,mp);}
            m.SetColor("_BaseColor",kv.Value.colour);m.SetFloat("_Metallic",kv.Value.metal);m.SetFloat("_Smoothness",kv.Value.smooth);EditorUtility.SetDirty(m);
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),kv.Key),m);
        }
        mi.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    // the model's material, its colour multiplied by tint, kept as an asset per person
    static Material Variant(Material source,string person,Color tint)
    {
        if(!AssetDatabase.IsValidFolder(Variants.TrimEnd('/')))AssetDatabase.CreateFolder("Assets/People","Variants");
        var path=Variants+person+source.name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(source);AssetDatabase.CreateAsset(m,path);}
        else m.CopyPropertiesFromMaterial(source);
        m.SetColor("_BaseColor",source.GetColor("_BaseColor")*tint);
        EditorUtility.SetDirty(m);return m;
    }
}
