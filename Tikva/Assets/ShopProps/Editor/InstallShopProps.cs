using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Shop interior props (counter, cash register, scales, island and wall shelving, fridges, produce
// bins, packing table, stool, baskets) in the grocery of GameScene, in place of the blockout
// placeholders, which are switched off (not deleted). One atlas material (URP Lit), glass apart.
// The vodka goes onto a real shelf. A model's front faces local -Z; every one gets a mesh collider.
public static class InstallShopProps
{
    const string Root = "Assets/ShopProps/";
    static readonly string[] Placeholders = { "Checkout", "Till", "Scale", "Gondola base", "Gondola shelf", "Milk refrigerator",
                                              "Refrigerator front", "Wooden produce crate", "Sparse groceries / packs" };
    static readonly string[] Floors = { "Plane", "Ground", "Shop tiled floor", "Grocery interior" };   // the props stand on these

    [MenuItem("Tools/Install shop props")]
    public static void Apply()
    {
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var atlas=AtlasMaterial();var glass=GlassMaterial();
        foreach(var path in AssetDatabase.FindAssets("t:Model",new[]{Root+"Models"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var mi=(ModelImporter)AssetImporter.GetAtPath(path);
            mi.animationType=ModelImporterAnimationType.None;mi.importAnimation=false;mi.importCameras=mi.importLights=false;
            mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"ShopProps_Atlas"),atlas);
            foreach(var g in new[]{"glass","glass_green"})mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),g),glass);
            mi.SaveAndReimport();
        }
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        var shop=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).First(t=>t.name=="Checkout").parent;
        var old=GameObject.Find("Shop props");if(old)Object.DestroyImmediate(old);
        var parent=new GameObject("Shop props").transform;
        // the street plane (street.fbx) lies at y 0 like the top of the shop floor and flickers through it:
        // the floor goes 1 cm above it, and everything in the shop stands on the floor
        var floor=shop.Find("Shop tiled floor");float floorY=.01f;
        var floorTop=floor.GetComponent<Renderer>().bounds.max.y;
        if(Mathf.Abs(floorTop-floorY)>.0001f)floor.position+=Vector3.up*(floorY-floorTop);
        // everything a bit larger than life (shelving 25 %, the rest 10-15 %) so the shop reads as full
        float Scale(string model)=>model is "Shop_Gondola" or "Shop_WallShelf" or "Shop_Fridge" or "Shop_ProduceBin"?1.25f
                                  :model is "Shop_PackingTable" or "Shop_Basket"?1.15f:1.1f;
        GameObject Put(string model,float yaw)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/"+model+".fbx"),scene);
            // the FBX root carries Blender's Z-up turn: keep it and turn about the world up on top of it
            var upright=go.transform.rotation;var scale=go.transform.localScale;
            go.transform.SetParent(parent,false);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,yaw,0)*upright);
            go.transform.localScale=scale*Scale(model);
            if(!go.GetComponent<MeshCollider>())go.AddComponent<MeshCollider>();
            return go;
        }
        void PutAt(string model,float x,float y,float z,float yaw)=>Put(model,yaw).transform.position=new Vector3(x,y+floorY,z);
        // a run of n modules standing on the floor, its box starting at x (or ending there, maxX) and z (or ending, maxZ)
        void Row(string model,float yaw,int n,float x,float z,bool alongX,bool maxX=false,bool maxZ=false)
        {
            for(int i=0;i<n;i++)
            {
                var go=Put(model,yaw);var b=go.GetComponent<Renderer>().bounds;
                go.transform.position+=new Vector3((maxX?x-b.size.x:x)-b.min.x,floorY-b.min.y,(maxZ?z-b.size.z:z)-b.min.z);
                if(alongX)x+=maxX?-b.size.x:b.size.x;else z+=maxZ?-b.size.z:b.size.z;
            }
        }
        // inside of the walls: x -20.88 .. -7.13, z -21.88 (back) .. -12.09 (shopfront, door at x -18.4 .. -16.6)
        // counter, panels to the customers (-Z); register and scales on it, keyboard to the cashier
        float top=1.1f*Scale("Shop_Counter");
        PutAt("Shop_Counter",-9.0f,0,-14.2f,0);
        PutAt("Shop_CashRegister",-9.55f,top,-14.2f,180);
        PutAt("Shop_Scales",-8.23f,top,-14.2f,180);
        PutAt("Shop_Stool",-10.0f,0,-12.7f,0);
        Row("Shop_WallShelf",90,1,-7.14f,-13.66f,false,maxX:true);              // behind the counter, back on the east wall
        Row("Shop_WallShelf",180,7,-19.35f,-21.87f,true);                     // back wall, clear of the fridges and the east wall run
        Row("Shop_WallShelf",90,3,-7.14f,-21.25f,false,maxX:true);            // east wall up to the queue at the counter
        // in the corners one run stops short of the other: nothing stands in front of a shelf or a fridge door
        Row("Shop_Fridge",-90,6,-20.87f,-21.25f,false);                       // west wall, doors to the shop
        Row("Shop_WallShelf",-90,2,-20.87f,-15.9f,false);                     // west wall on to the door
        // four island runs across the shop, the one by the counter shorter to leave the queue free
        foreach(var (x,n) in new[]{(-18.0f,3),(-15.4f,3),(-12.8f,3),(-10.2f,2)})Row("Shop_Gondola",90,n,x-.6125f,-20.07f,false);
        Row("Shop_ProduceBin",0,3,-16.45f,-12.1f,true,maxZ:true);            // under the display windows, low side to the shop
        Row("Shop_PackingTable",0,1,-19.9f,-12.1f,true,maxZ:true);          // in the corner by the door
        var table=parent.Cast<Transform>().Last(t=>t.name=="Shop_PackingTable").GetComponent<Renderer>().bounds;
        Physics.SyncTransforms();
        foreach(var dx in new[]{-.25f,.25f})                                   // two baskets on the table by the door
        {
            var basket=Put("Shop_Basket",90);var at=new Vector3(table.center.x+dx,table.max.y+.5f,table.center.z);
            Physics.Raycast(at,Vector3.down,out var on,1f,~0,QueryTriggerInteraction.Ignore);
            basket.transform.position=new Vector3(at.x,0,at.z);
            basket.transform.position+=Vector3.up*(on.point.y+.002f-basket.GetComponent<Renderer>().bounds.min.y);
        }
        // placeholders off
        var off=new List<string>();
        foreach(var t in shop.GetComponentsInChildren<Transform>(true))
            if(Placeholders.Contains(t.name)&&t.gameObject.activeSelf){t.gameObject.SetActive(false);off.Add(t.name);}
        // vodka on the top shelf of the third island, front module, aisle side to the counter
        var vodka=GameObject.Find("vodka");
        Physics.SyncTransforms();
        var vb=vodka.GetComponent<Renderer>().bounds;var spot=new Vector3(-12.8f+.49f,floorY+1.36f*1.25f+.3f,-15.92f);
        if(Physics.Raycast(spot,Vector3.down,out var shelf,.6f,~0,QueryTriggerInteraction.Ignore))
            vodka.transform.position=shelf.point+Vector3.up*(vodka.transform.position.y-vb.min.y+.002f);
        Physics.SyncTransforms();
        foreach(Transform prop in parent)
        {
            var b=prop.GetComponent<Renderer>().bounds;b.Expand(-.04f);var hits=new List<string>();
            foreach(var c in Physics.OverlapBox(b.center,b.extents*.92f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                if(c.transform!=prop&&c.gameObject!=vodka&&!Floors.Contains(c.name))hits.Add(c.name);
            Debug.Log($"SHOPPROP {prop.name} centre {b.center} size {b.size} overlaps [{string.Join(", ",hits.Distinct())}]");
        }
        bool onShelf=Physics.Raycast(vodka.transform.position,Vector3.down,out var hit,.4f,~0,QueryTriggerInteraction.Ignore)&&hit.transform.IsChildOf(parent);
        Debug.Log($"SHOPPROP vodka at {vodka.transform.position} on {(onShelf?hit.transform.name:"nothing")}, gap {(onShelf?hit.distance-.25f:-1):0.000} m; placeholders off: {off.Count} ({string.Join(", ",off.Distinct())})");
        InstallShopProducts.Place(scene);                                  // groceries on the new shelves
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    static Texture2D Tex(string name,bool normal,bool srgb)
    {
        var path=Root+"Textures/"+name+".png";var ti=(TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=srgb;ti.maxTextureSize=4096;ti.mipmapEnabled=true;
        ti.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material Mat(string name)
    {
        var path=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        return m;
    }
    static Material AtlasMaterial()
    {
        var m=Mat("ShopProps_Atlas");
        m.SetTexture("_BaseMap",Tex("ShopProps_Atlas_BaseColor",false,true));m.SetColor("_BaseColor",Color.white);
        m.SetTexture("_BumpMap",Tex("ShopProps_Atlas_Normal",true,false));m.SetFloat("_BumpScale",1);m.EnableKeyword("_NORMALMAP");
        m.SetTexture("_MetallicGlossMap",Tex("ShopProps_Atlas_MetallicSmoothness",false,false));m.EnableKeyword("_METALLICSPECGLOSSMAP");
        m.SetFloat("_Metallic",1);m.SetFloat("_Smoothness",1);m.SetFloat("_SmoothnessTextureChannel",0);
        EditorUtility.SetDirty(m);return m;
    }
    static Material GlassMaterial()
    {
        var m=Mat("ShopProps_Glass");
        m.SetColor("_BaseColor",new Color(.62f,.72f,.70f,.25f));m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.92f);
        m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_ZWrite",0);m.SetFloat("_AlphaClip",0);
        m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);m.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
        m.SetOverrideTag("RenderType","Transparent");m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=(int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);return m;
    }
}
