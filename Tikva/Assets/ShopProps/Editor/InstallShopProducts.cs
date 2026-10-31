using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Groceries on the shelves of the shop props: dry goods (packs, tins, cheese) on the island and
// wall shelving, milk, sour cream and butter in the fridges. Blocks of 2-5 of one product with
// gaps (a half-empty shop of the 90s), each item put down where a ray from above meets the
// shelf, upright on its surface, label to the aisle. Decoration only: no colliders.
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
        var rnd=new System.Random(1994);
        Physics.SyncTransforms();
        int placed=0,missed=0,tris=0;

        // one shelf: items along [a,b] of axis `along`, their front `front` metres out along `outward`
        void Shelf(Transform prop,Vector3 origin,Vector3 along,Vector3 outward,float level,float front,float a,float b,string[] pool)
        {
            float pos=a;
            while(pos<b)
            {
                var name=pool[rnd.Next(pool.Length)];var s=size[name];float w=Mathf.Abs(s.x)+.012f;
                int count=rnd.Next(2,6);
                for(int k=0;k<count&&pos+w<=b;k++,pos+=w)
                {
                    if(rnd.NextDouble()>.75)continue;                               // a gap: something sold out
                    var above=origin+along*(pos+w/2)+outward*(front-Mathf.Abs(s.y)/2)+Vector3.up*(level+.15f);
                    if(vodka&&Vector3.Distance(Flat2(above),Flat2(vodka.transform.position))<.2f){missed++;continue;}
                    if(!Physics.Raycast(above,Vector3.down,out var hit,.3f,~0,QueryTriggerInteraction.Ignore)||hit.transform!=prop||hit.normal.y<.9f){missed++;continue;}
                    var face=Vector3.ProjectOnPlane(outward,hit.normal).normalized;
                    var turn=Quaternion.AngleAxis((float)(rnd.NextDouble()*10-5),hit.normal)*Quaternion.LookRotation(-face,hit.normal);
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(models[name],scene);
                    var upright=go.transform.rotation;
                    go.transform.SetParent(root,false);go.transform.SetPositionAndRotation(hit.point,turn*upright);
                    GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
                    placed++;tris+=go.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3;
                }
                pos+=(float)rnd.NextDouble()*.12f;
            }
        }

        foreach(Transform prop in props)
        {
            var upright=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/"+prop.name+".fbx").transform.rotation;
            var yaw=prop.rotation*Quaternion.Inverse(upright);
            var along=yaw*Vector3.right;var front=yaw*Vector3.back;var o=prop.position;
            switch(prop.name)
            {
                case "Shop_Gondola":       // both sides, three tilted shelves, one row behind the price rail
                    foreach(var side in new[]{front,-front})foreach(var z in new[]{.62f,1.0f,1.36f})Shelf(prop,o,along,side,z,.455f,-.6f,.6f,Dry);
                    break;
                case "Shop_WallShelf":     // four shelves, one row behind the price strip
                    foreach(var z in new[]{.124f,.604f,1.084f,1.564f})Shelf(prop,o,along,front,z,.195f,-.54f,.54f,Dry);
                    break;
                case "Shop_Fridge":        // three wire shelves and the floor, behind the glass
                    foreach(var z in new[]{.15f,.562f,.962f,1.362f})Shelf(prop,o,along,front,z,.22f,-.24f,.24f,Cold);
                    break;
            }
        }
        Debug.Log($"SHOPPRODUCTS placed {placed}, skipped {missed}, {tris} tris");
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
