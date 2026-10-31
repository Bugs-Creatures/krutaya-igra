using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Groceries on the shelves of the shop props: dry goods (packs, tins, cheese) on the island and
// wall shelving, milk, sour cream and butter in the fridges. Every shelf holds one even row of a
// single product along its whole length, the product changing from shelf to shelf; each item is
// put down where a ray from above meets the shelf, upright on its surface, label to the aisle,
// half as large again as life, less where the shelf above leaves no room. Decoration only: no colliders.
public static class InstallShopProducts
{
    const string Root = "Assets/ShopProps/";
    static readonly string[] Dry = { "Product_PackA", "Product_PackB", "Product_PackSmall", "Product_CanRed", "Product_CanGreen", "Product_Cheese" };
    static readonly string[] Cold = { "Product_MilkBottle", "Product_Cup", "Product_Butter" };
    static readonly Dictionary<string, (Color colour, float metallic, float smoothness)> Flat = new()
    {
        ["Product_CanMetal"] = (new Color(.62f, .62f, .60f), .9f, .65f), ["Product_LabelRed"] = (new Color(.55f, .12f, .08f), 0, .4f),
        ["Product_LabelGreen"] = (new Color(.20f, .34f, .16f), 0, .4f), ["Product_Foil"] = (new Color(.75f, .74f, .70f), .8f, .7f),
        ["Product_Milk"] = (new Color(.88f, .88f, .85f), 0, .75f), ["Product_PlasticWhite"] = (new Color(.86f, .85f, .80f), 0, .55f),
        ["Product_PaperYellow"] = (new Color(.85f, .66f, .20f), 0, .3f),
    };

    [MenuItem("Tools/Install shop products")]
    public static void Apply()
    {
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        Place(scene);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }

    public static void Place(Scene scene)
    {
        Materials();
        var props=GameObject.Find("Shop props").transform;
        var old=GameObject.Find("Shop products");if(old)Object.DestroyImmediate(old);
        var root=new GameObject("Shop products").transform;
        var models=Dry.Concat(Cold).ToDictionary(n=>n,n=>AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Products/"+n+".fbx"));
        var size=models.ToDictionary(kv=>kv.Key,kv=>kv.Value.GetComponent<MeshFilter>().sharedMesh.bounds.size);   // x width, y depth, z height
        var vodka=GameObject.Find("vodka");
        const float Big=1.5f;
        Physics.SyncTransforms();
        int placed=0,missed=0,tris=0,shelves=0;

        // one shelf: a row of `name` along [a,b] of axis `along`, its front `front` metres out along `outward`
        void Shelf(Transform prop,Vector3 origin,Vector3 along,Vector3 outward,float level,float front,float a,float b,string name)
        {
            var s=size[name];float k=Big;
            float step=(b-a)/Mathf.Max(1,Mathf.FloorToInt((b-a)/(Mathf.Abs(s.x)*k+.015f)));
            for(float pos=a+step/2;pos<b;pos+=step)
            {
                var above=origin+along*pos+outward*(front-Mathf.Abs(s.y)*k/2)+Vector3.up*(level+.15f);
                if(vodka&&Vector3.Distance(Flat2(above),Flat2(vodka.transform.position))<.2f){missed++;continue;}
                if(!Physics.Raycast(above,Vector3.down,out var hit,.35f,~0,QueryTriggerInteraction.Ignore)||hit.transform!=prop||hit.normal.y<.9f){missed++;continue;}
                if(k==Big&&Physics.Raycast(hit.point+Vector3.up*.005f,Vector3.up,out var roof,Mathf.Abs(s.z)*Big,~0,QueryTriggerInteraction.Ignore))
                {   // the shelf above is too low: smaller, and the row re-spaced for it
                    k=Mathf.Min(Big,roof.distance*.92f/Mathf.Abs(s.z));
                    step=(b-a)/Mathf.Max(1,Mathf.FloorToInt((b-a)/(Mathf.Abs(s.x)*k+.015f)));pos=a-step/2;continue;
                }
                var face=Vector3.ProjectOnPlane(outward,hit.normal).normalized;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(models[name],scene);
                var upright=go.transform.rotation;
                go.transform.SetParent(root,false);go.transform.SetPositionAndRotation(hit.point,Quaternion.LookRotation(-face,hit.normal)*upright);
                go.transform.localScale*=k;
                GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
                placed++;tris+=go.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3;
            }
            shelves++;
        }

        int module=0;
        foreach(Transform prop in props)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/"+prop.name+".fbx").transform;
            var yaw=prop.rotation*Quaternion.Inverse(prefab.rotation);var m=prop.localScale.x/prefab.localScale.x;
            var along=yaw*Vector3.right;var front=yaw*Vector3.back;var o=prop.position;
            string Pick(string[] pool,int shelf)=>pool[(module*2+shelf)%pool.Length];
            switch(prop.name)
            {
                case "Shop_Gondola":       // both sides, three tilted shelves, one row behind the price rail
                    int side=0;
                    foreach(var dir in new[]{front,-front}){int l=0;foreach(var z in new[]{.62f,1.0f,1.36f})Shelf(prop,o,along,dir,z*m,.455f*m,-.6f*m,.6f*m,Pick(Dry,l++ +3*side));side++;}
                    break;
                case "Shop_WallShelf":     // four shelves, one row behind the price strip
                    {int l=0;foreach(var z in new[]{.124f,.604f,1.084f,1.564f})Shelf(prop,o,along,front,z*m,.195f*m,-.54f*m,.54f*m,Pick(Dry,l++));}
                    break;
                case "Shop_Fridge":        // three wire shelves and the floor, behind the glass
                    {int l=0;foreach(var z in new[]{.15f,.562f,.962f,1.362f})Shelf(prop,o,along,front,z*m,.22f*m,-.24f*m,.24f*m,Pick(Cold,l++));}
                    break;
                default:continue;
            }
            module++;
        }
        Debug.Log($"SHOPPRODUCTS placed {placed} on {shelves} shelves, skipped {missed}, {tris} tris");
    }
    static Vector3 Flat2(Vector3 v)=>new Vector3(v.x,0,v.z);

    static void Materials()
    {
        var made=new Dictionary<string,Material>();
        Material Mat(string name)
        {
            var path=Root+"Materials/Products/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            return made[name]=m;
        }
        Texture2D Tex(string name,bool normal,bool srgb)
        {
            var path=Root+"Products/"+name+".png";var ti=(TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=srgb;ti.maxTextureSize=1024;ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        var pack=Mat("Product_Pack");
        pack.SetTexture("_BaseMap",Tex("Product_Pack_BaseColor",false,true));pack.SetColor("_BaseColor",Color.white);
        pack.SetTexture("_BumpMap",Tex("Product_Pack_Normal",true,false));pack.EnableKeyword("_NORMALMAP");
        pack.SetTexture("_MetallicGlossMap",Tex("Product_Pack_MetallicSmoothness",false,false));pack.EnableKeyword("_METALLICSPECGLOSSMAP");
        pack.SetFloat("_Metallic",1);pack.SetFloat("_Smoothness",1);
        EditorUtility.SetDirty(pack);
        foreach(var kv in Flat)
        {
            var m=Mat(kv.Key);m.SetColor("_BaseColor",kv.Value.colour);m.SetFloat("_Metallic",kv.Value.metallic);m.SetFloat("_Smoothness",kv.Value.smoothness);
            EditorUtility.SetDirty(m);
        }
        foreach(var path in AssetDatabase.FindAssets("t:Model",new[]{Root+"Products"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var mi=(ModelImporter)AssetImporter.GetAtPath(path);
            mi.animationType=ModelImporterAnimationType.None;mi.importAnimation=false;mi.importCameras=mi.importLights=false;
            mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var kv in made)mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),kv.Key),kv.Value);
            mi.SaveAndReimport();
        }
    }
}
