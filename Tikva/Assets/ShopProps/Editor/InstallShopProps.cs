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
        void Put(string model,float x,float y,float z,float yaw)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/"+model+".fbx"),scene);
            // the FBX root carries Blender's Z-up turn: keep it and turn about the world up on top of it
            var upright=go.transform.rotation;
            go.transform.SetParent(parent,false);go.transform.SetPositionAndRotation(new Vector3(x,y,z),Quaternion.Euler(0,yaw,0)*upright);
            if(!go.GetComponent<MeshCollider>())go.AddComponent<MeshCollider>();
        }
        // counter in place of the checkout, panels to the customers (-Z); register and scales on it, keyboard to the cashier
        Put("Shop_Counter",-9.0f,0,-14.0f,0);
        Put("Shop_CashRegister",-9.5f,1.1f,-14.0f,180);
        Put("Shop_Scales",-8.3f,1.1f,-14.0f,180);
        Put("Shop_Stool",-10.0f,0,-12.65f,0);
        Put("Shop_WallShelf",-7.355f,0,-12.85f,90);                       // behind the counter, back on the east wall
        // two island runs of three 1.33 m modules (4 m, as the blockout)
        foreach(var x in new[]{-11.5f,-14.8f})foreach(var z in new[]{-18.83f,-17.5f,-16.17f})Put("Shop_Gondola",x,0,z,90);
        foreach(var z in new[]{-20.85f,-20.15f,-19.45f,-18.75f})Put("Shop_Fridge",-20.49f,0,z,-90);  // west wall, doors to the shop
        foreach(var x in new[]{-19.0f,-17.5f,-16.0f,-14.5f})Put("Shop_ProduceBin",x,0,-21.3f,180);    // in place of the crates, low side to the shop
        foreach(var x in new[]{-12.63f,-11.37f,-10.11f})Put("Shop_WallShelf",x,0,-21.645f,180);        // back on the back wall
        Put("Shop_PackingTable",-15.0f,0,-12.5f,0);                        // by the display window near the door
        Put("Shop_Basket",-16.25f,0,-12.55f,10);
        Put("Shop_Basket",-16.25f,0,-13.05f,-8);
        // placeholders off
        var off=new List<string>();
        foreach(var t in shop.GetComponentsInChildren<Transform>(true))
            if(Placeholders.Contains(t.name)&&t.gameObject.activeSelf){t.gameObject.SetActive(false);off.Add(t.name);}
        // vodka on the top shelf of the island module it stood at, 13 cm off the back panel
        var vodka=GameObject.Find("vodka");
        vodka.transform.position=new Vector3(-11.37f,1.36f-.122f*Mathf.Tan(6*Mathf.Deg2Rad)+.25f,-15.81f);
        Physics.SyncTransforms();
        foreach(Transform prop in parent)
        {
            var b=prop.GetComponent<Renderer>().bounds;var hits=new List<string>();
            foreach(var c in Physics.OverlapBox(b.center,b.extents*.92f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                if(!c.transform.IsChildOf(parent)&&c.gameObject!=vodka&&!Floors.Contains(c.name))hits.Add(c.name);
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
