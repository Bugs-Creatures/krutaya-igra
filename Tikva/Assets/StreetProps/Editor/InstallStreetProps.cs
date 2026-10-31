using System.Linq;
using UnityEditor;
using UnityEngine;

// The team's street props (a smashed can, a cigarette pack, a trash bin, a trash bag), one FBX each from
// their Shopopos.fbx with the pivot at the centre of the base, on one URP Lit material with their texture
// set (base colour, metallic in R and smoothness in A, normal map). Each gets a prefab with a convex mesh
// collider, ready to be put in the scene. Safe to run again.
public static class InstallStreetProps
{
    const string Root="Assets/StreetProps/";

    [MenuItem("Tools/Import street props")]
    public static void Apply()
    {
        Texture2D Tex(string name,bool normal,bool srgb)
        {
            var path=Root+"Textures/Shopopos_"+name+".png";var ti=(TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=srgb;ti.maxTextureSize=2048;ti.anisoLevel=8;ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        if(!AssetDatabase.IsValidFolder(Root+"Materials"))AssetDatabase.CreateFolder("Assets/StreetProps","Materials");
        if(!AssetDatabase.IsValidFolder(Root+"Prefabs"))AssetDatabase.CreateFolder("Assets/StreetProps","Prefabs");
        const string matPath=Root+"Materials/StreetProps_Shopopos.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,matPath);}
        mat.SetTexture("_BaseMap",Tex("BaseColor",false,true));mat.SetColor("_BaseColor",Color.white);
        mat.SetTexture("_BumpMap",Tex("Normal",true,false));mat.EnableKeyword("_NORMALMAP");
        mat.SetTexture("_MetallicGlossMap",Tex("MetallicSmoothness",false,false));mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        mat.SetFloat("_Metallic",1);mat.SetFloat("_Smoothness",1);
        EditorUtility.SetDirty(mat);
        foreach(var path in AssetDatabase.FindAssets("t:Model",new[]{Root+"Models"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var mi=(ModelImporter)AssetImporter.GetAtPath(path);
            mi.animationType=ModelImporterAnimationType.None;mi.importAnimation=false;mi.importCameras=mi.importLights=false;
            mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"Shopopos"),mat);
            mi.SaveAndReimport();
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            if(!go.GetComponent<MeshCollider>()){var c=go.AddComponent<MeshCollider>();c.convex=true;}
            PrefabUtility.SaveAsPrefabAsset(go,Root+"Prefabs/"+go.name+".prefab");
            Debug.Log("STREETPROPS "+go.name);
            Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();
    }

    // where they go in GameScene: a bin by the second entrance in place of the blockout's grey one and one by
    // the shop door, bags by the two green containers, cans and cigarette packs about the bench, the entrance,
    // the containers and the shop; each set down where a ray from above meets the ground, turned at random,
    // the packs lying flat
    static readonly (string prop,float x,float z)[] Spots =
    {
        ("Prop_TrashBin",3.0f,10.0f),("Prop_TrashBin",-19.6f,-11.3f),
        ("Prop_TrashBag",27.8f,9.4f),("Prop_TrashBag",28.2f,11.0f),("Prop_TrashBag",32.5f,9.5f),("Prop_TrashBag",3.75f,10.65f),
        ("Prop_SmashedCan",-0.9f,8.2f),("Prop_SmashedCan",3.9f,9.5f),("Prop_SmashedCan",30.2f,8.8f),("Prop_SmashedCan",-16.0f,-10.7f),("Prop_SmashedCan",2.4f,10.7f),
        ("Prop_Cigarettes",-1.0f,9.9f),("Prop_Cigarettes",0.8f,10.9f),("Prop_Cigarettes",-15.2f,-11.0f),
    };

    [MenuItem("Tools/Place street props")]
    public static void Place()
    {
        if(!Application.isBatchMode&&!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        var old=GameObject.Find("Street props");if(old)Object.DestroyImmediate(old);
        var blockoutBin=GameObject.Find("СЕВЕР • 1994 • BLOCKOUT/05 • Двор, дорога и границы/Bin");if(blockoutBin)blockoutBin.SetActive(false);
        var root=new GameObject("Street props").transform;
        var rnd=new System.Random(1994);
        Physics.SyncTransforms();
        foreach(var (prop,x,z) in Spots)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/"+prop+".prefab"),scene);
            var upright=go.transform.rotation;
            go.transform.SetParent(root,true);
            var flat=prop=="Prop_Cigarettes"?Quaternion.Euler(0,0,90):Quaternion.identity;   // the pack's thin side down
            var tilt=prop=="Prop_TrashBag"?Quaternion.Euler((float)rnd.NextDouble()*10-5,0,(float)rnd.NextDouble()*10-5):Quaternion.identity;
            go.transform.rotation=Quaternion.Euler(0,(float)rnd.NextDouble()*360,0)*tilt*flat*upright;
            var ground=Physics.RaycastAll(new Vector3(x,1.5f,z),Vector3.down,5,~0,QueryTriggerInteraction.Ignore)   // from under the entrance canopy
                .Where(h=>!h.collider.transform.IsChildOf(go.transform)).OrderBy(h=>h.distance).First();
            go.transform.position=new Vector3(x,0,z);
            go.transform.position+=Vector3.up*(ground.point.y-go.GetComponent<Renderer>().bounds.min.y);
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
            Physics.SyncTransforms();
            Debug.Log($"STREETPROPS {prop} at ({x},{z}) on {ground.collider.name} y {ground.point.y:F3}");
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    }
}
