using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Groceries on the shelves of the shop props: dry goods (sugar, salt, soda, condensed milk, stew, soap) on
// the island and wall shelving, milk, Snezhok cups and butter in the fridges, butter and dairy on the decks
// of the serve-over counters. All of them share the team's texture set (Product_Props). Every shelf holds a single product,
// changing from shelf to shelf, in neat blocks of a few facings and a few rows deep; each item is
// put down where a ray from above meets the shelf, label to the aisle, half as large again as life,
// less where the shelf above leaves no room. Decoration only: no colliders.
public static class InstallShopProducts
{
    const string Root = "Assets/ShopProps/";
    static readonly string[] Dry = { "Product_Sugar", "Product_Salt", "Product_Soda", "Product_CondensedMilk", "Product_Stew", "Product_Soap" };
    static readonly string[] Cold = { "Product_MilkBottle", "Product_Cup", "Product_Butter" };
    static readonly string[] Deli = { "Product_Butter", "Product_Cup", "Product_Butter", "Product_MilkBottle", "Product_Cup" };

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

        var rnd=new System.Random(1994);
        // one shelf, one product: blocks of 2-4 facings, each 1 to maxDeep deep from the price rail back towards
        // the panel (or `open` metres where there is none), gaps between blocks; along [a,b] of axis `along`, the
        // front `front` metres out along `outward`. A share `skip` of the shelves stays empty. Only the front row casts shadows.
        void Shelf(Transform prop,Vector3 origin,Vector3 along,Vector3 outward,float level,float front,float a,float b,string name,int maxDeep,float open=.3f,float skip=0)
        {
            if(rnd.NextDouble()<skip){shelves++;return;}
            var s=size[name];float k=Big,w0=Mathf.Abs(s.x),d0=Mathf.Abs(s.y),h0=Mathf.Abs(s.z);
            // the room on this shelf, at its middle: up to the shelf above (smaller items if it is low) and back to the panel
            var mid=origin+along*((a+b)/2)+outward*(front-.02f)+Vector3.up*(level+.15f);
            if(!Physics.Raycast(mid,Vector3.down,out var bed,.35f,~0,QueryTriggerInteraction.Ignore)||bed.transform!=prop){missed++;return;}
            if(Physics.Raycast(bed.point+Vector3.up*.005f,Vector3.up,out var roof,h0*Big,~0,QueryTriggerInteraction.Ignore))k=Mathf.Min(Big,roof.distance*.92f/h0);
            float w=w0*k+.01f,d=d0*k+.008f;
            float room=Physics.Raycast(bed.point+Vector3.up*(h0*k*.5f),-outward,out var panel,1f,~0,QueryTriggerInteraction.Ignore)?panel.distance:open;
            int deepMax=Mathf.Clamp(Mathf.FloorToInt((room+.01f)/d),1,maxDeep);
            for(float pos=a;;)
            {
                int across=rnd.Next(2,5),deep=rnd.Next(1,deepMax+1);
                if(pos+across*w>b){across=Mathf.FloorToInt((b-pos)/w);if(across<1)break;}
                for(int i=0;i<across;i++)for(int j=0;j<deep;j++)
                {
                    var above=origin+along*(pos+w*(i+.5f))+outward*(front-d*(j+.5f))+Vector3.up*(level+.15f);
                    if(vodka&&Vector3.Distance(Flat2(above),Flat2(vodka.transform.position))<.2f){missed++;continue;}
                    if(!Physics.Raycast(above,Vector3.down,out var hit,.35f,~0,QueryTriggerInteraction.Ignore)||hit.transform!=prop||hit.normal.y<.9f){missed++;continue;}
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(models[name],scene);
                    var upright=go.transform.rotation;
                    go.transform.SetParent(root,false);go.transform.SetPositionAndRotation(hit.point,Quaternion.LookRotation(-outward,Vector3.up)*upright);
                    go.transform.localScale*=k;
                    if(j>0)go.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                    GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
                    placed++;tris+=go.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3;
                }
                pos+=across*w+.08f+(float)rnd.NextDouble()*.25f;
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
                case "Shop_Gondola":       // both sides, three shelves, behind the price rail
                    int side=0;
                    foreach(var dir in new[]{front,-front}){int l=0;foreach(var z in new[]{.62f,1.0f,1.36f})Shelf(prop,o,along,dir,z*m,.455f*m,-.6f*m,.6f*m,Pick(Dry,l++ +3*side),2);side++;}
                    break;
                case "Shop_WallShelf":     // four shelves, behind the price strip
                    {int l=0;foreach(var z in new[]{.124f,.604f,1.084f,1.564f})Shelf(prop,o,along,front,z*m,.195f*m,-.54f*m,.54f*m,Pick(Dry,l++),2,skip:.2f);}
                    break;
                case "Shop_Fridge":        // three wire shelves and the floor, behind the glass
                    {int l=0;foreach(var z in new[]{.15f,.562f,.962f,1.362f})Shelf(prop,o,along,front,z*m,.22f*m,-.24f*m,.24f*m,Pick(Cold,l++),2);}
                    break;
                case "Shop_Vitrine":       // the display deck behind the curved glass, up to the seller's work top
                    Shelf(prop,o,along,front,.775f*m,.42f*m,-.68f*m,.68f*m,Pick(Deli,0),3,.5f);
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
            ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=srgb;ti.maxTextureSize=2048;ti.anisoLevel=8;ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        var props=Mat("Product_Props");
        props.SetTexture("_BaseMap",Tex("Product_Props_BaseColor",false,true));props.SetColor("_BaseColor",Color.white);
        props.SetTexture("_BumpMap",Tex("Product_Props_Normal",true,false));props.EnableKeyword("_NORMALMAP");
        props.SetTexture("_MetallicGlossMap",Tex("Product_Props_MetallicSmoothness",false,false));props.EnableKeyword("_METALLICSPECGLOSSMAP");
        props.SetFloat("_Metallic",1);props.SetFloat("_Smoothness",1);
        EditorUtility.SetDirty(props);
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
