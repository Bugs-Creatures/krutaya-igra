using System.Linq;
using UnityEditor;
using UnityEngine;

// The team's street props (a smashed can, a cigarette pack, a trash bin, a trash bag from their Shopopos.fbx,
// a big green container from ShopoposbIG.fbx), one FBX each with the pivot at the centre of the base, on URP
// Lit materials with their texture sets (base colour, metallic in R and smoothness in A, normal map), and a
// prefab each. The bins stay where they are put and are hollow: its collider is its own mesh, not a hull, so rubbish can fall into it. The
// rubbish is loose: a rigid body each, on a convex hull (the can; the bag's from Prop_TrashBag_Hull, its hull
// thinned under the 255 faces Unity allows one) or a box (the pack). Safe to run again.
public static class InstallStreetProps
{
    const string Root="Assets/StreetProps/";

    [MenuItem("Tools/Import street props")]
    public static void Apply()
    {
        Texture2D Tex(string name,bool normal,bool srgb)
        {
            var path=Root+"Textures/"+name+".png";var ti=(TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=srgb;ti.maxTextureSize=2048;ti.anisoLevel=8;ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        if(!AssetDatabase.IsValidFolder(Root+"Materials"))AssetDatabase.CreateFolder("Assets/StreetProps","Materials");
        if(!AssetDatabase.IsValidFolder(Root+"Prefabs"))AssetDatabase.CreateFolder("Assets/StreetProps","Prefabs");
        Material Mat(string set)   // the texture set's material, by the set's name (the material name in the FBX)
        {
            var matPath=Root+"Materials/StreetProps_"+set+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,matPath);}
            mat.SetTexture("_BaseMap",Tex(set+"_BaseColor",false,true));mat.SetColor("_BaseColor",Color.white);
            mat.SetTexture("_BumpMap",Tex(set+"_Normal",true,false));mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_MetallicGlossMap",Tex(set+"_MetallicSmoothness",false,false));mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetFloat("_Metallic",1);mat.SetFloat("_Smoothness",1);
            EditorUtility.SetDirty(mat);return mat;
        }
        var sets=new[]{"Shopopos","Dumpster"}.ToDictionary(n=>n,Mat);
        foreach(var path in AssetDatabase.FindAssets("t:Model",new[]{Root+"Models"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>!p.EndsWith("_Hull.fbx")))
        {
            var mi=(ModelImporter)AssetImporter.GetAtPath(path);
            mi.animationType=ModelImporterAnimationType.None;mi.importAnimation=false;mi.importCameras=mi.importLights=false;
            mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var kv in sets)mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),kv.Key),kv.Value);
            mi.SaveAndReimport();
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var mesh=go.GetComponent<MeshFilter>().sharedMesh.bounds;   // the model's own space: z up
            void Box(float across){var b=go.AddComponent<BoxCollider>();b.center=mesh.center;b.size=Vector3.Scale(mesh.size,new Vector3(across,across,1));}
            void Body(float mass){var r=go.AddComponent<Rigidbody>();r.mass=mass;r.angularDamping=.5f;r.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;}
            switch(go.name)
            {
                case "Prop_TrashBin":case "Prop_Dumpster":go.AddComponent<MeshCollider>().convex=false;break;
                case "Prop_TrashBag":
                    var hull=go.AddComponent<MeshCollider>();hull.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Models/Prop_TrashBag_Hull.fbx");hull.convex=true;Body(3);break;
                case "Prop_SmashedCan":go.AddComponent<MeshCollider>().convex=true;Body(.05f);break;
                case "Prop_Cigarettes":Box(1);Body(.03f);break;
            }
            PrefabUtility.SaveAsPrefabAsset(go,Root+"Prefabs/"+go.name+".prefab");
            Debug.Log("STREETPROPS "+go.name);
            Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();
    }

    // where they go in GameScene: a bin by the second entrance in place of the blockout's grey one and one to
    // the left of the shop door (seen from the street; Zaika stands on the right), cans and cigarette packs
    // dropped into each; the big containers where the blockout's two green boxes stood, filled with bags, cans
    // and packs, a few more of them on the ground about them. The bins stand where a ray from above meets the
    // ground; the rubbish hangs over its place, turned at random, one piece above the other so none touch,
    // and falls into place when the game starts
    static readonly (float x,float z)[] Bins={(3.0f,10.0f),(-15.7f,-11.55f)};
    static readonly (float x,float z)[] Dumpsters={(29.0f,10.0f),(31.2f,10.0f)};
    const int SmallLoad=3,BigBags=11,BigLoad=6;   // cans and packs into each bin; bags (filling a container past its middle), cans and packs into each container
    static readonly (string prop,float x,float z)[] OnGround =
    {
        ("Prop_TrashBag",27.8f,9.4f),("Prop_TrashBag",28.0f,11.35f),("Prop_TrashBag",32.5f,9.5f),
        ("Prop_SmashedCan",30.2f,8.8f),("Prop_SmashedCan",28.9f,11.4f),("Prop_Cigarettes",32.0f,11.0f),
    };

    // the rubbish let fall in the editor until it lies still, so the scene keeps it resting in its place (the
    // rest of the scene's loose bodies held meanwhile)
    static void Settle(Transform root)
    {
        var mode=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
        var held=Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Where(b=>!b.isKinematic&&!b.transform.IsChildOf(root)).ToArray();
        foreach(var b in held)b.isKinematic=true;
        Physics.SyncTransforms();
        for(int i=0;i<750;i++)Physics.Simulate(.02f);
        foreach(var b in held)b.isKinematic=false;
        Physics.simulationMode=mode;
        Debug.Log($"STREETPROPS settled, {root.GetComponentsInChildren<Rigidbody>().Count(b=>!b.IsSleeping())} still moving");
    }

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
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(t=>t.name=="Dumpster"&&t.parent&&t.parent.name.StartsWith("05 •")).ToArray())t.gameObject.SetActive(false);
        GameObject Put(string prop,float x,float z,float lift,bool tumble,float? yaw=null)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/"+prop+".prefab"),scene);
            var upright=go.transform.rotation;
            go.transform.SetParent(root,true);
            float R()=>(float)rnd.NextDouble()*360;
            go.transform.rotation=(tumble?Quaternion.Euler(R(),R(),R()):Quaternion.Euler(0,yaw??R(),0))*upright;
            var ground=Physics.RaycastAll(new Vector3(x,1.5f,z),Vector3.down,5,~0,QueryTriggerInteraction.Ignore)   // from under the entrance canopy
                .Where(h=>!h.collider.transform.IsChildOf(go.transform)&&!h.collider.attachedRigidbody).OrderBy(h=>h.distance).First();
            go.transform.position=new Vector3(x,0,z);
            go.transform.position+=Vector3.up*(ground.point.y+lift-go.GetComponent<Renderer>().bounds.min.y);
            if(!go.GetComponent<Rigidbody>())GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
            Physics.SyncTransforms();
            return go;
        }
        float J(float r)=>((float)rnd.NextDouble()*2-1)*r;
        GameObject Drop(string prop,float x,float z,float bottom,bool? tumble=null)   // hung with its lowest point at bottom
        {
            var go=Put(prop,x,z,0,tumble??prop!="Prop_TrashBag");
            go.transform.position+=Vector3.up*(bottom-go.GetComponent<Renderer>().bounds.min.y);
            return go;
        }
        foreach(var (x,z) in Bins)   // cans and packs in turn, a column over the opening
        {
            float top=Put("Prop_TrashBin",x,z,0,false).GetComponent<Renderer>().bounds.max.y,lift=top+.05f;
            for(int i=0;i<SmallLoad*2;i++)lift=Drop(i%2==0?"Prop_SmashedCan":"Prop_Cigarettes",x+J(.12f),z+J(.12f),lift).GetComponent<Renderer>().bounds.max.y+.05f;
        }
        foreach(var (x,z) in Dumpsters)   // four bags standing side by side, the rest one over another any way up, then the cans and packs
        {
            float top=Put("Prop_Dumpster",x,z,0,false,0).GetComponent<Renderer>().bounds.max.y,lift=top-.4f,high=lift;
            for(int i=0;i<BigBags;i++)
            {
                bool floor=i<4;var go=Drop("Prop_TrashBag",x+(floor?(i%2*2-1)*.3f:J(.3f)),z+(floor?(i/2*2-1)*.3f:J(.3f)),floor?lift:high+.05f,!floor);
                high=Mathf.Max(high,go.GetComponent<Renderer>().bounds.max.y);
            }
            for(int i=0;i<BigLoad*2;i++)high=Drop(i%2==0?"Prop_SmashedCan":"Prop_Cigarettes",x+J(.45f),z+J(.45f),high+.05f).GetComponent<Renderer>().bounds.max.y;
        }
        foreach(var (prop,x,z) in OnGround)Put(prop,x,z,prop=="Prop_TrashBag"?.15f:.35f,prop!="Prop_TrashBag");
        Settle(root);
        Debug.Log($"STREETPROPS placed {root.childCount}");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    }
}
