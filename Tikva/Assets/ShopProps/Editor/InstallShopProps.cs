using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// The grocery of GameScene fitted out as a counter-service shop of the 90s: wall cabinets round the
// back and side walls, the seller's walkway in front of them, then a U of serve-over counters (glass
// to the customers, open to the seller); a double-sided island with produce crates at its ends in the
// middle, the checkout with the salesman not far from the door, glass-door fridges and the packing
// table with baskets by the shopfront. Tiled floor, painted walls, panel ceiling and fluorescent
// fittings with their lights. The blockout placeholders are switched off (not deleted). Two atlas
// materials (URP Lit), glass and lamp tubes apart. The vodka goes onto the island, within the player's
// reach. A model's front faces local -Z; every prop gets a mesh collider.
public static class InstallShopProps
{
    const string Root = "Assets/ShopProps/";
    static readonly string[] Placeholders = { "Checkout", "Till", "Scale", "Gondola base", "Gondola shelf", "Milk refrigerator",
                                              "Refrigerator front", "Wooden produce crate", "Sparse groceries / packs" };
    static readonly string[] Floors = { "Plane", "Ground", "Shop tiled floor", "Grocery interior" };   // the props stand on these
    // inside of the walls, the floor (1 cm over the street plane) and the underside of the ceiling lining
    const float West = -20.875f, East = -7.135f, Back = -21.875f, Front = -12.095f, FloorY = .01f, CeilY = 3.12f;
    const float DoorX0 = -18.4f, DoorX1 = -16.6f, DoorTop = 2.2f;

    [MenuItem("Tools/Install shop props")]
    public static void Apply()
    {
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var remap=new Dictionary<string,Material>{["ShopProps_Atlas"]=AtlasMaterial("ShopProps_Atlas"),["ShopProps2_Atlas"]=AtlasMaterial("ShopProps2_Atlas"),
                                                 ["glass"]=GlassMaterial(),["glass_green"]=GlassMaterial(),["lamp_tube"]=TubeMaterial()};
        foreach(var path in AssetDatabase.FindAssets("t:Model",new[]{Root+"Models"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var mi=(ModelImporter)AssetImporter.GetAtPath(path);
            mi.animationType=ModelImporterAnimationType.None;mi.importAnimation=false;mi.importCameras=mi.importLights=false;
            mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var kv in remap)mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),kv.Key),kv.Value);
            mi.SaveAndReimport();
        }
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        var shop=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).First(t=>t.name=="Checkout").parent;
        foreach(var n in new[]{"Shop props","Shop interior"}){var old=GameObject.Find(n);if(old)Object.DestroyImmediate(old);}
        var parent=new GameObject("Shop props").transform;
        // the street plane (street.fbx) lies at y 0 like the top of the shop floor and flickers through it:
        // the floor goes 1 cm above it, and everything in the shop stands on the floor
        var floor=shop.Find("Shop tiled floor");
        var floorTop=floor.GetComponent<Renderer>().bounds.max.y;
        if(Mathf.Abs(floorTop-FloorY)>.0001f)floor.position+=Vector3.up*(FloorY-floorTop);
        // shelving a quarter larger than life, the counter and the small things a tenth or so, the new counters as built
        float Scale(string model)=>model switch{"Shop_Gondola" or "Shop_Fridge" or "Shop_ProduceBin"=>1.25f,"Shop_WallShelf"=>1.24f,
                                               "Shop_PackingTable" or "Shop_Basket"=>1.15f,"Shop_Vitrine" or "Shop_VitrineCorner" or "Shop_Lamp"=>1f,_=>1.1f};
        GameObject Put(string model,float yaw,Transform under=null)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/"+model+".fbx"),scene);
            // the FBX root carries Blender's Z-up turn: keep it and turn about the world up on top of it
            var upright=go.transform.rotation;var scale=go.transform.localScale;
            go.transform.SetParent(under?under:parent,false);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,yaw,0)*upright);
            go.transform.localScale=scale*Scale(model);
            if(!under&&!go.GetComponent<MeshCollider>())go.AddComponent<MeshCollider>();
            return go;
        }
        void PutAt(string model,float x,float y,float z,float yaw)=>Put(model,yaw).transform.position=new Vector3(x,y+FloorY,z);
        // a run of n modules standing on the floor, its box starting at x (or ending there, maxX) and z (or ending, maxZ)
        void Row(string model,float yaw,int n,float x,float z,bool alongX,bool maxX=false,bool maxZ=false)
        {
            for(int i=0;i<n;i++)
            {
                var go=Put(model,yaw);var b=go.GetComponent<Renderer>().bounds;
                go.transform.position+=new Vector3((maxX?x-b.size.x:x)-b.min.x,FloorY-b.min.y,(maxZ?z-b.size.z:z)-b.min.z);
                if(alongX)x+=maxX?-b.size.x:b.size.x;else z+=maxZ?-b.size.z:b.size.z;
            }
        }

        // counters all round: cabinets on the walls, the seller's walkway, then the serve-over counters.
        // In the back corners the side cabinets start in front of the back run, so nothing stands before a shelf.
        const float cab=.49f*1.24f,walk=.835f,corner=.95f;
        float inW=West+.005f+cab+walk,inE=East-.005f-cab-walk,inB=Back+.005f+cab+walk;   // seller's side of the counters
        Row("Shop_WallShelf",180,8,West+.005f+cab+.005f,Back+.005f,true);               // back wall
        Row("Shop_WallShelf",-90,4,West+.005f,Back+.005f+cab+.005f,false);              // west wall, back towards the door
        Row("Shop_WallShelf",90,4,East-.005f,Back+.005f+cab+.005f,false,maxX:true);     // east wall
        Row("Shop_VitrineCorner",0,1,inW,inB,true);
        Row("Shop_VitrineCorner",0,1,inE,inB,true,maxX:true);
        Row("Shop_Vitrine",180,6,inW+corner,inB,true);                                  // glass to the customers (+Z)
        Row("Shop_Vitrine",-90,3,inW,inB+corner,false);                                 // to +X
        Row("Shop_Vitrine",90,3,inE,inB+corner,false,maxX:true);                        // to -X
        // the checkout by the shopfront, a few steps from the door: panels to the customers (-Z), register and
        // scales on it, keyboard to the cashier, who stands between it and the shopfront
        float top=1.1f*Scale("Shop_Counter"),checkX=-13.3f,checkZ=-13.72f;
        PutAt("Shop_Counter",checkX,0,checkZ,0);
        PutAt("Shop_CashRegister",checkX-.55f,top,checkZ,180);
        PutAt("Shop_Scales",checkX+.77f,top,checkZ,180);
        PutAt("Shop_Stool",checkX+.95f,0,-12.55f,0);
        var salesman=GameObject.Find("salesman");
        salesman.transform.SetPositionAndRotation(new Vector3(checkX-.45f,salesman.transform.position.y,-12.62f),Quaternion.Euler(0,180,0));
        // the island in the hall, its two sides to the back counters and to the checkout, a produce crate at each end, low side out
        const float islandX=-14.0f,islandZ=-16.75f;
        Row("Shop_Gondola",0,2,islandX-1.6625f,islandZ-.6125f,true);
        Row("Shop_ProduceBin",90,1,islandX-1.6625f-.02f,islandZ-.6875f,true,maxX:true);
        Row("Shop_ProduceBin",-90,1,islandX+1.6625f+.02f,islandZ-.6875f,true);
        Row("Shop_Fridge",0,4,East-.005f,Front-.005f,true,maxX:true,maxZ:true);         // glass-door fridges along the shopfront, east
        Row("Shop_PackingTable",0,1,West+.015f,Front-.005f,true,maxZ:true);             // packing table in the corner by the door
        var table=parent.Cast<Transform>().Last(t=>t.name=="Shop_PackingTable").GetComponent<Renderer>().bounds;
        Physics.SyncTransforms();
        foreach(var dx in new[]{-.25f,.25f})                                            // two baskets on it
        {
            var basket=Put("Shop_Basket",90);var at=new Vector3(table.center.x+dx,table.max.y+.5f,table.center.z);
            Physics.Raycast(at,Vector3.down,out var on,1f,~0,QueryTriggerInteraction.Ignore);
            basket.transform.position=new Vector3(at.x,0,at.z);
            basket.transform.position+=Vector3.up*(on.point.y+.002f-basket.GetComponent<Renderer>().bounds.min.y);
        }

        Interior(shop,floor,(model,yaw,under)=>Put(model,yaw,under));
        // placeholders off
        var off=new List<string>();
        foreach(var t in shop.GetComponentsInChildren<Transform>(true))
            if(Placeholders.Contains(t.name)&&t.gameObject.activeSelf){t.gameObject.SetActive(false);off.Add(t.name);}
        // vodka on the island's top shelf, on the side to the checkout
        var vodka=GameObject.Find("vodka");
        Physics.SyncTransforms();
        var vb=vodka.GetComponent<Renderer>().bounds;var spot=new Vector3(islandX+.6f,FloorY+1.36f*1.25f+.3f,islandZ+.49f);
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

    // tiled floor; walls and ceiling lined 1 cm inside the blockout (its outside stays as it is);
    // nine two-tube fittings on the ceiling with a point light each, in place of the blockout's one light
    static void Interior(Transform shop,Transform floor,Func<string,float,Transform,GameObject> put)
    {
        var root=new GameObject("Shop interior").transform;
        var floorMat=Surface("Interior_Floor",new Vector2(14/1.2f,10/1.2f),normal:true,gloss:true);
        floor.GetComponent<Renderer>().sharedMaterial=floorMat;
        var wall=Surface("Interior_Wall",Vector2.one,normal:false,gloss:true);
        var ceiling=Surface("Interior_Ceiling",Vector2.one,normal:true,gloss:false);
        float height=CeilY-FloorY;
        // a wall panel from a to b (on the floor) up to y1, facing into the shop; texture 1 m wide, the full height tall
        void Wall(string name,Vector3 a,Vector3 b,Vector3 facing,float y0=FloorY,float y1=CeilY)
        {
            var u=b-a;float len=u.magnitude;
            Panel(root,name,new Vector3(a.x,y0,a.z),u,Vector3.up*(y1-y0),facing,wall,
                  p=>new Vector2(Vector3.Dot(p-a,u/len),(p.y-FloorY)/height));
        }
        Wall("Back lining",new Vector3(West,0,Back+.005f),new Vector3(East,0,Back+.005f),Vector3.forward);
        Wall("West lining",new Vector3(West+.005f,0,Back),new Vector3(West+.005f,0,Front),Vector3.right);
        Wall("East lining",new Vector3(East-.005f,0,Back),new Vector3(East-.005f,0,Front),Vector3.left);
        Wall("Shopfront lining west",new Vector3(West,0,Front-.005f),new Vector3(DoorX0,0,Front-.005f),Vector3.back);
        Wall("Shopfront lining east",new Vector3(DoorX1,0,Front-.005f),new Vector3(East,0,Front-.005f),Vector3.back);
        Wall("Shopfront lining over the door",new Vector3(DoorX0,0,Front-.005f),new Vector3(DoorX1,0,Front-.005f),Vector3.back,DoorTop);
        Panel(root,"Ceiling lining",new Vector3(West,CeilY,Back),Vector3.right*(East-West),Vector3.forward*(Front-Back),Vector3.down,ceiling,
              p=>new Vector2(p.x/1.2f,p.z/1.2f));
        // fittings in three rows across the shop, the tray against the ceiling, a cold point light under each
        var old=shop.GetComponentsInChildren<Light>(true).FirstOrDefault(l=>l.name=="Cold fluorescent");
        if(old)old.gameObject.SetActive(false);
        foreach(var z in new[]{-20.2f,-16.8f,-13.6f})foreach(var x in new[]{-18.0f,-14.0f,-10.0f})
        {
            var lamp=put("Shop_Lamp",0,root);var b=lamp.GetComponent<Renderer>().bounds;
            lamp.transform.position+=new Vector3(x,CeilY,z)-new Vector3(b.center.x,b.max.y,b.center.z);
            var light=new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(root,false);light.transform.position=new Vector3(x,CeilY-.18f,z);
            light.type=LightType.Point;light.range=6f;light.intensity=1.5f;light.color=new Color(.90f,.96f,1f);light.shadows=LightShadows.None;
        }
    }

    // one quad facing `facing`, its UVs from the world position by `uv`
    static void Panel(Transform root,string name,Vector3 origin,Vector3 u,Vector3 v,Vector3 facing,Material mat,Func<Vector3,Vector2> uv)
    {
        var mesh=new Mesh{name=name};
        var corners=new[]{origin,origin+u,origin+u+v,origin+v};
        mesh.vertices=corners.Select(c=>c-origin).ToArray();
        mesh.uv=corners.Select(uv).ToArray();
        mesh.triangles=Vector3.Dot(Vector3.Cross(u,v),facing)>0?new[]{0,1,2,0,2,3}:new[]{0,2,1,0,3,2};
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
        go.transform.SetParent(root,false);go.transform.position=origin;
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;
        GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
    }

    static Texture2D Tex(string name,bool normal,bool srgb)
    {
        var path=Root+"Textures/"+name+".png";var ti=(TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=srgb;ti.maxTextureSize=4096;ti.mipmapEnabled=true;
        ti.wrapMode=TextureWrapMode.Repeat;ti.anisoLevel=normal?1:4;
        ti.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material Mat(string name)
    {
        var path=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        return m;
    }
    static Material AtlasMaterial(string name)=>Surface(name,Vector2.one,normal:true,gloss:true);
    // URP Lit from <name>_BaseColor, _Normal and _MetallicSmoothness (metallic in R, smoothness in A)
    static Material Surface(string name,Vector2 tiling,bool normal,bool gloss)
    {
        var m=Mat(name);
        m.SetTexture("_BaseMap",Tex(name+"_BaseColor",false,true));m.SetColor("_BaseColor",Color.white);m.SetTextureScale("_BaseMap",tiling);
        if(normal){m.SetTexture("_BumpMap",Tex(name+"_Normal",true,false));m.SetFloat("_BumpScale",1);m.EnableKeyword("_NORMALMAP");}
        if(gloss)
        {
            m.SetTexture("_MetallicGlossMap",Tex(name+"_MetallicSmoothness",false,false));m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetFloat("_Metallic",1);m.SetFloat("_Smoothness",1);m.SetFloat("_SmoothnessTextureChannel",0);
        }
        else{m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.15f);}
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
    static Material TubeMaterial()                                      // lit fluorescent tubes and the counters' light strips
    {
        var m=Mat("ShopProps_LampTube");
        m.SetColor("_BaseColor",new Color(.95f,.97f,1f));m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.6f);
        m.SetColor("_EmissionColor",new Color(.9f,.95f,1f)*2.5f);m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(m);return m;
    }
}
