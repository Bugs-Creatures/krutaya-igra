using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using Object=UnityEngine.Object;

public static class StyleSecretRoom
{
    const string Folder="Assets/SecretRoom";
    const float West=-6.13158f,East=2.22028f,South=47.26172f,North=53.60128f,Floor=.8217216f,Ceiling=4.89928f;
    static Material metal,concrete,paint,trim;
    static void Folders()
    {
        foreach(string name in new[]{"Materials","Meshes","Prefabs"})if(!AssetDatabase.IsValidFolder(Folder+"/"+name))AssetDatabase.CreateFolder(Folder,name);
    }
    static Material Lit(string name,Color color,float smoothness,Vector2 tiling,bool texture=true)
    {
        string path=Folder+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat)
        {
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Concrete_Tile.mat");
            mat=texture?new Material(source):new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);
        }
        mat.SetColor("_BaseColor",color);mat.SetColor("_Color",color);mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",smoothness);
        mat.SetTextureScale("_BaseMap",tiling);mat.SetTextureScale("_BumpMap",tiling);mat.DisableKeyword("_METALLICSPECGLOSSMAP");
        mat.SetFloat("_BumpScale",.85f);EditorUtility.SetDirty(mat);return mat;
    }
    static Material Glow(string name,Color color,float strength)
    {
        var mat=Lit(name,color,.28f,Vector2.one,false);mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*strength);EditorUtility.SetDirty(mat);return mat;
    }
    static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material mat,bool collider=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    static Mesh MeshAsset(string name,Vector3[] points,int[] triangles,Vector2[] uv)
    {
        var mesh=new Mesh{name=name};mesh.vertices=points;mesh.triangles=triangles;mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateBounds();
        var path=Folder+"/Meshes/"+name+".asset";AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static GameObject Plane(string name,Transform parent,Vector3 origin,Vector3 horizontal,Vector3 vertical,float width,float height,Material mat,Vector3 normal)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.position=origin;
        var a=horizontal.normalized*width*.5f;var b=vertical.normalized*height*.5f;
        var triangles=Vector3.Dot(Vector3.Cross(horizontal,vertical),normal)>0?new[]{0,1,2,0,2,3}:new[]{0,2,1,0,3,2};
        var mesh=MeshAsset(name,new[]{-a-b,a-b,a+b,-a+b},triangles,new[]{Vector2.zero,new Vector2(width/1.8f,0),new Vector2(width/1.8f,height/1.8f),new Vector2(0,height/1.8f)});
        mesh.normals=Enumerable.Repeat(normal,4).ToArray();go.GetComponent<MeshFilter>().sharedMesh=mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial=mat;return go;
    }
    static void Pipe(string name,Transform parent,Vector3 start,Vector3 end,float radius,Material mat)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent,false);go.transform.position=(start+end)*.5f;
        go.transform.rotation=Quaternion.FromToRotation(Vector3.up,(end-start).normalized);go.transform.localScale=new Vector3(radius*2,(end-start).magnitude*.5f,radius*2);
        go.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(go.GetComponent<Collider>());
    }
    static void Peeling(string name,Transform parent,Vector3 centre,Vector3 horizontal,Vector3 inward,float width)
    {
        var random=new System.Random(name.GetHashCode());var points=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        for(int n=0;n<24;n++)
        {
            float x=((float)random.NextDouble()-.5f)*(width-.25f),y=(float)random.NextDouble()*1.15f-.52f;
            float rx=.0125f+(float)random.NextDouble()*.065f,ry=.0125f+(float)random.NextDouble()*.055f;
            int first=points.Count;
            for(int k=0;k<7;k++)
            {
                float angle=k*Mathf.PI*2/7;float jag=.7f+(float)random.NextDouble()*.5f;
                var q=new Vector2(x+Mathf.Cos(angle)*rx*jag,y+Mathf.Sin(angle)*ry*jag);
                points.Add(horizontal*q.x+Vector3.up*q.y+inward*.004f);uv.Add(new Vector2(q.x/1.8f,q.y/1.8f));
            }
            for(int k=1;k<6;k++)triangles.AddRange(Vector3.Dot(Vector3.Cross(horizontal,Vector3.up),inward)>0?new[]{first,first+k,first+k+1}:new[]{first,first+k+1,first+k});
        }
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.position=centre;
        var mesh=MeshAsset(name,points.ToArray(),triangles.ToArray(),uv.ToArray());mesh.normals=Enumerable.Repeat(inward,points.Count).ToArray();
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=concrete;
        go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
    }
    static void WallBand(string name,Transform parent,Vector3 centre,Vector3 horizontal,Vector3 inward,float width)
    {
        centre.y=Floor+.675f;centre+=inward*.015f;
        var panel=Plane(name,parent,centre,horizontal,Vector3.up,width,1.35f,paint,inward);panel.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        Peeling(name+" peeling paint",parent,centre,horizontal,inward,width);
        var bottom=centre;bottom.y=Floor+.045f;
        var baseboard=Plane(name+" damp skirting",parent,bottom+inward*.003f,horizontal,Vector3.up,width,.09f,trim,inward);baseboard.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
    }
    static TextMeshPro Text(string name,Transform parent,string content,Vector3 position,Vector2 size,float fontSize,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,180,0);
        var text=go.AddComponent<TextMeshPro>();text.font=Object.FindAnyObjectByType<InteractionPrompt>().font;text.text=content;text.fontSize=fontSize;text.alignment=TextAlignmentOptions.Center;text.color=color;text.rectTransform.sizeDelta=size;
        text.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return text;
    }
    static GameObject MakeMonitor()
    {
        var root=new GameObject("Surveillance Monitor 16x9");
        var body=Lit("Monitor enclosure",new Color(.075f,.08f,.075f),.28f,Vector2.one,false);
        Box("Enclosure",root.transform,new Vector3(0,0,-.045f),new Vector3(1.93f,1.18f,.34f),body);
        var bezel=Lit("Monitor bezel",new Color(.025f,.028f,.026f),.18f,Vector2.one,false);
        Box("Top bezel",root.transform,new Vector3(0,.527f,.143f),new Vector3(1.92f,.125f,.1f),bezel);
        Box("Bottom bezel",root.transform,new Vector3(0,-.527f,.143f),new Vector3(1.92f,.125f,.1f),bezel);
        Box("Left bezel",root.transform,new Vector3(-.895f,0,.143f),new Vector3(.13f,.935f,.1f),bezel);
        Box("Right bezel",root.transform,new Vector3(.895f,0,.143f),new Vector3(.13f,.935f,.1f),bezel);
        Box("Wall bracket",root.transform,new Vector3(0,0,-.26f),new Vector3(.6f,.5f,.12f),metal);
        var screen=new GameObject("Screen 16x9",typeof(MeshFilter),typeof(MeshRenderer));screen.transform.SetParent(root.transform,false);screen.transform.localPosition=new Vector3(0,0,.195f);
        const float width=1.66f,height=width*9/16;
        screen.GetComponent<MeshFilter>().sharedMesh=MeshAsset("Monitor screen 16x9",new[]{new Vector3(-width/2,-height/2),new Vector3(width/2,-height/2),new Vector3(width/2,height/2),new Vector3(-width/2,height/2)},new[]{0,1,2,0,2,3},new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
        var screenMat=new Material(Shader.Find("SecretRoom/MonitorScreen"));AssetDatabase.CreateAsset(screenMat,Folder+"/Materials/Monitor screen.mat");
        var renderer=screen.GetComponent<MeshRenderer>();renderer.sharedMaterial=screenMat;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        var placeholder=Text("No signal placeholder",root.transform,"КАНАЛ 01\nНЕТ СИГНАЛА",new Vector3(0,0,.201f),new Vector2(1.5f,.65f),1.25f,new Color(.18f,.22f,.24f));
        Text("Enclosure label",root.transform,"КОНТРОЛЬ  /  67",new Vector3(-.48f,-.535f,.201f),new Vector2(.7f,.06f),.32f,new Color(.55f,.57f,.51f));
        var led=Glow("Standby LED",new Color(.22f,.65f,.3f),1.5f);Box("Status LED",root.transform,new Vector3(.79f,-.536f,.205f),new Vector3(.025f,.012f,.015f),led);
        for(int n=0;n<4;n++)Box("Bezel screw "+(n+1),root.transform,new Vector3(n%2==0?-.91f:.91f,n<2?-.55f:.55f,.202f),new Vector3(.025f,.025f,.008f),metal);
        var light=new GameObject("White screen light").AddComponent<Light>();light.transform.SetParent(root.transform,false);light.transform.localPosition=new Vector3(0,0,.27f);
        light.type=LightType.Spot;light.spotAngle=110;light.innerSpotAngle=75;light.range=8;light.shadows=LightShadows.Soft;light.shadowBias=.02f;light.shadowNormalBias=.1f;light.shadowStrength=.85f;
        var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,0,-.045f);collider.size=new Vector3(1.93f,1.18f,.34f);
        var monitor=root.AddComponent<SurveillanceMonitor>();monitor.screenRenderer=renderer;monitor.screenLight=light;monitor.placeholderLabel=placeholder.gameObject;monitor.Refresh();
        return root;
    }
    static void LocalGrade(Transform parent)
    {
        var go=new GameObject("Basement local atmosphere");go.transform.SetParent(parent,false);go.transform.position=new Vector3((West+East)/2,(Floor+Ceiling)/2,(South+North)/2);
        var box=go.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(East-West,Ceiling-Floor,North-South);
        var volume=go.AddComponent<Volume>();volume.isGlobal=false;volume.priority=20;volume.blendDistance=.35f;
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Folder+"/Basement atmosphere.asset");volume.sharedProfile=profile;
        var grade=profile.Add<ColorAdjustments>();grade.postExposure.Override(-.35f);grade.contrast.Override(16);grade.saturation.Override(-12);AssetDatabase.AddObjectToAsset(grade,profile);
        var bloom=profile.Add<Bloom>();bloom.threshold.Override(.95f);bloom.intensity.Override(.3f);bloom.scatter.Override(.55f);AssetDatabase.AddObjectToAsset(bloom,profile);
        var vignette=profile.Add<Vignette>();vignette.intensity.Override(.28f);vignette.smoothness.Override(.55f);AssetDatabase.AddObjectToAsset(vignette,profile);
        var grain=profile.Add<FilmGrain>();grain.type.Override(FilmGrainLookup.Thin1);grain.intensity.Override(.12f);grain.response.Override(.75f);AssetDatabase.AddObjectToAsset(grain,profile);
        EditorUtility.SetDirty(profile);
    }
    [MenuItem("Tools/Secret Room/Style basement room")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
        if(GameObject.Find("Secret Basement Dressing"))throw new System.InvalidOperationException("Dressing already exists. Edit the created objects directly.");
        Folders();var root=new GameObject("Secret Basement Dressing").transform;Undo.RegisterCreatedObjectUndo(root.gameObject,"Dress secret basement");
        concrete=Lit("Damp concrete walls",new Color(.6f,.6f,.53f),.1f,new Vector2(6.77718f/1.8f,11.0759f/1.8f));
        var floor=Lit("Worn concrete floor",new Color(.4f,.42f,.38f),.3f,new Vector2(10.6389f/1.6f,11.1433f/1.6f));
        var ceiling=Lit("Sooted concrete ceiling",new Color(.4f,.42f,.38f),.08f,new Vector2(10.6389f/1.8f,11.1433f/1.8f));
        paint=Lit("Old olive basement paint",new Color(.44f,.52f,.43f),.18f,Vector2.one);
        trim=Lit("Damp dark skirting",new Color(.12f,.15f,.12f),.12f,Vector2.one);
        metal=Lit("Aged utility steel",new Color(.15f,.17f,.15f),.27f,Vector2.one,false);
        foreach(var name in new[]{"comeback","comeback (1)","comeback (2)","comeback (3)"})
        {var renderer=GameObject.Find(name).GetComponent<Renderer>();Undo.RecordObject(renderer,"Basement wall material");renderer.sharedMaterial=concrete;EditorUtility.SetDirty(renderer);}
        var floorRenderer=GameObject.Find("cubelol").GetComponent<Renderer>();Undo.RecordObject(floorRenderer,"Basement floor material");floorRenderer.sharedMaterial=floor;EditorUtility.SetDirty(floorRenderer);
        var ceilingRenderer=GameObject.Find("cubelol (1)").GetComponent<Renderer>();Undo.RecordObject(ceilingRenderer,"Basement ceiling material");ceilingRenderer.sharedMaterial=ceiling;EditorUtility.SetDirty(ceilingRenderer);
        var finishes=new GameObject("Paint and weathering").transform;finishes.SetParent(root,false);
        WallBand("West lower wall",finishes,new Vector3(West,0,(South+North)/2),Vector3.forward,Vector3.right,North-South);
        WallBand("East lower wall",finishes,new Vector3(East,0,(South+North)/2),Vector3.forward,Vector3.left,North-South);
        WallBand("North lower wall",finishes,new Vector3((West+East)/2,0,North),Vector3.right,Vector3.back,East-West);
        WallBand("South lower wall",finishes,new Vector3((West+East)/2,0,South),Vector3.right,Vector3.forward,East-West);
        var services=new GameObject("Exposed services").transform;services.SetParent(root,false);
        var rust=Lit("Oxidised pipes",new Color(.25f,.13f,.075f),.15f,Vector2.one,false);
        for(int n=0;n<3;n++)Pipe("Ceiling pipe "+(n+1),services,new Vector3(West+.4f+n*.27f,Ceiling-.25f,South+.1f),new Vector3(West+.4f+n*.27f,Ceiling-.25f,North-.1f),n==0?.055f:.03f,n==0?rust:metal);
        for(int n=0;n<3;n++)Box("Pipe hanger "+(n+1),services,new Vector3(West+.67f,Ceiling-.32f,South+1+n*2.1f),new Vector3(.85f,.035f,.06f),metal);
        Pipe("Monitor cable conduit",services,new Vector3(West+.09f,Floor+.4f,52.45f),new Vector3(West+.09f,Ceiling-.35f,52.45f),.022f,metal);
        Box("Cable junction",services,new Vector3(West+.12f,Floor+.55f,52.45f),new Vector3(.1f,.26f,.22f),metal);
        var monitor=MakeMonitor();var prefab=PrefabUtility.SaveAsPrefabAsset(monitor,Folder+"/Prefabs/SurveillanceMonitor_16x9.prefab");Object.DestroyImmediate(monitor);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);instance.name="Monitor 01 - duplicate me";instance.transform.SetParent(root,false);instance.transform.SetPositionAndRotation(new Vector3(West+.27f,2.65f,52.45f),Quaternion.LookRotation(Vector3.right));
        var emergency=new GameObject("Red emergency bulkhead").transform;emergency.SetParent(root,false);emergency.SetPositionAndRotation(new Vector3(-4.6f,3.65f,South+.13f),Quaternion.identity);
        Box("Steel housing",emergency,Vector3.zero,new Vector3(.38f,.22f,.13f),metal);
        var red=Glow("Red emergency lens",new Color(1,.018f,.008f),3);
        Box("Red lens",emergency,new Vector3(0,0,.085f),new Vector3(.3f,.14f,.055f),red);
        for(int n=0;n<3;n++)Box("Lens guard "+(n+1),emergency,new Vector3(-.11f+n*.11f,0,.13f),new Vector3(.014f,.2f,.025f),metal);
        var redLight=new GameObject("Red spill light").AddComponent<Light>();redLight.transform.SetParent(emergency,false);redLight.transform.localPosition=new Vector3(0,0,.25f);
        redLight.type=LightType.Point;redLight.color=new Color(1,.035f,.02f);redLight.intensity=12;redLight.range=10;redLight.shadows=LightShadows.Soft;redLight.shadowNormalBias=.12f;redLight.shadowBias=.025f;
        Pipe("Emergency light conduit",services,new Vector3(-4.6f,3.65f,South+.07f),new Vector3(-4.6f,Ceiling-.15f,South+.07f),.018f,metal);
        var bounce=new GameObject("Monitor reflected white light").AddComponent<Light>();bounce.transform.SetParent(root,false);bounce.transform.position=new Vector3(.3f,3.2f,51.4f);
        bounce.type=LightType.Point;bounce.color=new Color(.86f,.92f,1);bounce.intensity=1.6f;bounce.range=7;bounce.shadows=LightShadows.Soft;bounce.shadowNormalBias=.1f;
        LocalGrade(root);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveScene(root.gameObject.scene);
        Selection.activeGameObject=instance;
    }
}
