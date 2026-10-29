using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using NorthBlockout;

public static partial class NorthBlockoutBuilder
{
    const string Dir = "Assets/NorthBlockout";
    const string ScenePath = Dir + "/NorthernTown_Blockout.unity";
    static Transform root, group;
    static Dictionary<string, Material> mats;
    static readonly List<Renderer> homeRoof = new(), homeCut = new(), shopRoof = new(), shopCut = new(), bakeryRoof = new(), bakeryCut = new();
    static readonly List<Transform> shots = new();
    static Material M(string key) => mats[key];
    static Transform Group(string name, Transform parent = null)
    {
        var g = new GameObject(name).transform; g.SetParent(parent ? parent : root); return g;
    }
    static GameObject Box(string name, Vector3 p, Vector3 s, string mat, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
        go.transform.SetParent(parent ? parent : group); go.transform.position = p; go.transform.localScale = s;
        go.GetComponent<Renderer>().sharedMaterial = M(mat); return go;
    }
    static GameObject B(string n, float x,float y,float z,float sx,float sy,float sz,string m,Transform p=null) => Box(n,new Vector3(x,y,z),new Vector3(sx,sy,sz),m,p);
    static void NonSolid(GameObject go) { Object.DestroyImmediate(go.GetComponent<Collider>()); }
    static void Collect(Transform t, List<Renderer> list) => list.AddRange(t.GetComponentsInChildren<Renderer>());
    static void Material(string name, string hex, bool glow=false)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var color);
        string path = Dir + "/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
        mat.color=color; mat.SetFloat("_Smoothness", .05f);
        if(glow) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor",color * .65f); }
        mats.Add(name,mat);
    }
    static void Label(string text, Vector3 pos, float size, Color color, float yaw=0)
    {
        var g=new GameObject(text);g.transform.SetParent(group);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);
        var t=g.AddComponent<TextMesh>(); t.text=text;t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;
        g.GetComponent<MeshRenderer>().sharedMaterial=t.font.material;
        g.AddComponent<BlockoutWorldLabel>();
    }
    // Wall along X, with a full-height doorway and lintel. Bottom is walkable floor level.
    static void DoorX(string n,float x,float y,float z,float length,float door,float width,string m,float height=2.7f)
    {
        float a=x-length/2,b=x+length/2,l=door-width/2,r=door+width/2;
        if(l>a) B(n+" left",(a+l)/2,y+height/2,z,l-a,height,.18f,m);
        if(b>r) B(n+" right",(r+b)/2,y+height/2,z,b-r,height,.18f,m);
        B(n+" lintel",door,y+2.45f,z,width,.5f,.18f,m);
    }
    static void DoorZ(string n,float x,float y,float z,float length,float door,float width,string m,float height=2.7f)
    {
        float a=z-length/2,b=z+length/2,l=door-width/2,r=door+width/2;
        if(l>a) B(n+" left",x,y+height/2,(a+l)/2,.18f,height,l-a,m);
        if(b>r) B(n+" right",x,y+height/2,(r+b)/2,.18f,height,b-r,m);
        B(n+" lintel",x,y+2.45f,door,.18f,.5f,width,m);
    }
    static void Window(float x,float y,float z,bool front=true,Transform p=null)
    {
        float sign=front?-1:1;
        B("Window / old frame",x,y,z,1.45f,1.65f,.12f,"frame",p);
        B("Cold glass",x,y,z+sign*.075f,1.25f,1.45f,.035f,((Mathf.RoundToInt(x*3+y*7)%13)==0)?"warm":"glass",p);
        B("Mullion",x,y,z+sign*.10f,.055f,1.45f,.04f,"frame",p);
        B("Sill",x,y-.85f,z+sign*.13f,1.65f,.10f,.32f,"concrete",p);
    }
    static void Housing(string name,float cx,float cz,float width,float depth,bool playable=false,bool bakery=false)
    {
        group=Group(name); var shell=Group("Upper floors / camera cutaway",group);
        if(!playable && !bakery) B("Unplayable residential mass",cx,7.5f,cz,width,15,depth,"plaster");
        else B("Upper residential mass",cx,bakery?9:10.5f,cz,width,bakery?12:9,depth,"plaster",shell);
        B("Flat tar roof",cx,15.15f,cz,width+.45f,.3f,depth+.45f,"roof",playable||bakery?shell:group);
        B("Foundation",cx,-.12f,cz,width+.15f,.24f,depth+.15f,"concrete");
        for(int f=0;f<5;f++)
        {
            float y=f*3+1.55f;
            var parent=(playable&&f>=2)||(bakery&&f>=1)?shell:group;
            if(playable && f<2)
            {
                // Actual facade with openings, instead of windows pasted over solid walls.
                float front=cz-depth/2,back=cz+depth/2;
                B("Facade sill band west",cx-width/4-1.5f,f*3+.35f,front,width/2-3,.7f,.25f,"plaster",parent);
                B("Facade sill band east",cx+width/4+1.5f,f*3+.35f,front,width/2-3,.7f,.25f,"plaster",parent);
                B("Facade head band",cx,f*3+2.7f,front,width,.6f,.25f,"plaster",parent);
                for(float x=cx-width/2+.6f;x<cx+width/2;x+=3) if(Mathf.Abs(x-cx)>2.5f) B("Facade pier",x,f*3+1.55f,front,1.55f,1.8f,.25f,"plaster",parent);
                B("Rear wall",cx,f*3+1.5f,back,width,3,.25f,"plaster",parent);
            }
            for(float x=cx-width/2+2.2f;x<cx+width/2-1;x+=3.6f)
            {
                if(playable && Mathf.Abs(x-cx)<3) continue;
                if(bakery && f==0)continue;
                Window(x,y,cz-depth/2-.08f,true,parent);
                Window(x,y,cz+depth/2+.08f,false,parent);
            }
            if(f>0) B("Floor seam",cx,f*3,cz-depth/2-.04f,width,.06f,.06f,"concrete",parent);
        }
        for(int i=0;i<3;i++) B("Roof ventilation",cx-width*.3f+i*width*.3f,15.7f,cz+.5f,.8f,1,.9f,"concrete",shell);
        if(playable)
        {
            B("West end wall",cx-width/2,3,cz,.25f,6,depth,"plaster");
            var east=B("East end wall",cx+width/2,3,cz,.25f,6,depth,"plaster");homeCut.Add(east.GetComponent<Renderer>());
            B("Closed ground-floor flats west",-8.8f,1.45f,18,12.1f,2.9f,11.5f,"plaster");
            B("Closed ground-floor flats east",8.8f,1.45f,18,12.1f,2.9f,11.5f,"plaster");
            B("Other flats / upper west",-8.9f,4.45f,18,11.8f,2.9f,11.5f,"plaster",shell);
            B("Other flats / upper east",13,4.45f,18,3.7f,2.9f,11.5f,"plaster",shell);
            B("Apartment ceiling",7,5.84f,16.5f,8,.22f,7,"ceiling",shell);
            Collect(shell,homeRoof);
            foreach(var r in group.GetComponentsInChildren<Renderer>())
                if(r.bounds.max.z < cz-depth/2+.4f && r.bounds.max.y < 6.1f)homeCut.Add(r);
        }
        if(bakery)Collect(shell,bakeryRoof);
    }
    static void Point(string name,Vector3 p,Color color,float range,float intensity)
    {
        var go=new GameObject(name);go.transform.SetParent(group);go.transform.position=p;
        var l=go.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.range=range;l.intensity=intensity;l.shadows=LightShadows.None;
    }
    static void Stairs()
    {
        group=Group("02 • Подъезд / 1.8 m flights");
        B("Entrance floor",0,-.09f,18,6,.18f,12,"tile");
        DoorX("Entrance facade",0,0,12,6,0,1.8f,"plaster");
        DoorX("Entrance paint",0,0,11.84f,6,0,1.8f,"green",1.25f);
        B("Entrance canopy",0,2.8f,11,3.4f,.18f,2.5f,"concrete");
        B("Open metal door",-1.05f,1.05f,11.4f,.12f,2.1f,1.1f,"green").transform.rotation=Quaternion.Euler(0,-18,0);
        Label("ПОДЪЕЗД 2",new Vector3(0,2.5f,11.72f),.07f,Color.black);
        homeCut.Add(group.GetChild(group.childCount-1).GetComponent<Renderer>());
        var facade=Group("Upper stair facade / cutaway",group);
        B("Upper stair facade",0,4.5f,12,6,3,.22f,"plaster",facade);
        Window(0,4.5f,11.84f,true,facade);Collect(facade,homeCut);
        for(int floor=0;floor<4;floor++)
        {
            float y=floor*3;
            var level=Group("Stair floor "+(floor+1),group);Transform old=group;group=level;
            for(int i=0;i<10;i++)
            {
                float h=(i+1)*.15f;
                B("Flight A step "+i,-1.35f,y+h-.09f,14+i*.32f,1.9f,.18f,.32f,"concrete");
                B("Flight B step "+i,1.35f,y+1.5f+h-.09f,16.88f-i*.32f,1.9f,.18f,.32f,"concrete");
            }
            B("Half landing",0,y+1.41f,18.05f,5.7f,.18f,2.05f,"tile");
            B("Floor landing",0,y+2.91f,12.9f,5.7f,.18f,1.9f,"tile");
            B("Center rail A",-.31f,y+1.28f,15.44f,.06f,.06f,3.55f,"rust").transform.rotation=Quaternion.Euler(-25,0,0);
            B("Center rail B",.31f,y+2.78f,15.44f,.06f,.06f,3.55f,"rust").transform.rotation=Quaternion.Euler(25,0,0);
            for(int i=0;i<5;i++)
            {
                B("Rail post A",-.31f,y+.8f+i*.3f,14+i*.64f,.045f,.85f,.045f,"rust");
                B("Rail post B",.31f,y+2.3f+i*.3f,16.88f-i*.64f,.045f,.85f,.045f,"rust");
            }
            B("Landing backstop",0,y+2.15f,19.1f,5.7f,1.3f,.18f,"green");
            Point("Dim stair bulb",new Vector3(0,y+2.6f,13.2f),new Color(.82f,.76f,.52f),6,1.8f);
            if(floor>=1) Collect(level,homeRoof);
            group=old;
        }
        var west=B("West stair wall",-3,2.9f,18,.18f,5.8f,12,"green");homeCut.Add(west.GetComponent<Renderer>());
        DoorZ("East stair wall",3,3,18,12,13.55f,1.4f,"green");
        for(int i=0;i<5;i++) B("Mailboxes",-2.8f,1.5f,12.7f+i*.42f,.2f,.45f,.36f,"rust");
        B("Entrance bulb",0,2.55f,11.3f,.28f,.12f,.18f,"warm");
        Point("Entrance light",new Vector3(0,2.5f,11),new Color(1,.72f,.38f),7,2);
    }
    static void Apartment()
    {
        group=Group("01 • Квартира 12 / 3 facade bays / second floor");
        B("Apartment floor",7,2.9f,16.5f,8,.2f,7,"wood");
        B("Kitchen linoleum",9,3.012f,18.5f,4,.018f,3,"linoleum");
        B("Bathroom tile",5.75f,3.015f,19.1f,2.5f,.025f,1.8f,"tile");
        var front=Group("Near walls / fixed camera cutaway",group);var old=group;group=front;
        B("South outer wall",7,4.35f,13,8,2.7f,.2f,"wallpaper");
        Window(5.2f,4.55f,13.13f,false);
        Window(8.8f,4.55f,13.13f,false);
        DoorZ("Bedroom wall",8,3,15,4,15.7f,1.2f,"wallpaper");
        B("East outer wall",11,4.35f,16.5f,.2f,2.7f,7,"wallpaper");
        Collect(front,homeCut);group=old;
        B("North apartment wall",7,4.35f,20,8,2.7f,.18f,"wallpaper");
        DoorZ("Hall to living",4.5f,3,15,4,14.8f,1.2f,"wallpaper");
        DoorZ("Hall to foyer",4.5f,3,18.5f,3,17.6f,1.2f,"wallpaper");
        B("Living / kitchen partition",7.75f,4.35f,17,6.5f,2.7f,.18f,"wallpaper");
        DoorZ("Kitchen doorway",7,3,18.5f,3,17.65f,1.2f,"wallpaper");
        DoorX("Bathroom door",5.75f,3,18.2f,2.5f,5.7f,1.05f,"wallpaper");
        B("Apartment door / open against wall",3.7f,4.05f,13.15f,1.1f,2.1f,.10f,"wood");
        B("Sofa",6.05f,3.28f,16.45f,2.6f,.55f,.8f,"sofa");
        B("Sofa back",6.05f,3.65f,16.85f,2.6f,.85f,.15f,"sofa");
        B("Rug",6.1f,3.03f,15.1f,2.35f,.035f,1.7f,"rug");
        B("TV stand",6.2f,3.3f,13.4f,1.1f,.6f,.5f,"wood");
        B("CRT television",6.2f,3.9f,13.4f,.8f,.65f,.55f,"metal");
        B("CRT screen",6.2f,3.9f,13.69f,.64f,.45f,.03f,"glass");
        B("Wardrobe",10.55f,4.05f,16.3f,.7f,2.1f,1.2f,"wood");
        B("Bed base",9.35f,3.22f,14.25f,1.4f,.44f,2,"wood");
        B("Blanket",9.35f,3.5f,14.25f,1.4f,.16f,2,"sofa");
        B("Pillow",9.35f,3.63f,13.55f,.8f,.15f,.4f,"ceiling");
        B("Kitchen counter",10.55f,3.44f,18.2f,.7f,.88f,2.1f,"cream");
        B("Sink",10.55f,3.9f,18.3f,.6f,.06f,.65f,"metal");
        B("Gas stove",9.6f,3.45f,19.55f,.65f,.9f,.65f,"cream");
        for(int i=0;i<4;i++)B("Burner",9.43f+(i%2)*.32f,3.92f,19.38f+(i/2)*.32f,.2f,.035f,.2f,"metal");
        B("Refrigerator",8.2f,3.85f,19.5f,.7f,1.7f,.7f,"cream");
        B("Small kitchen table",8.65f,3.74f,18.3f,1.1f,.12f,.75f,"wood");
        B("Table pedestal",8.65f,3.35f,18.3f,.2f,.7f,.2f,"wood");
        B("Stool",8.6f,3.23f,17.5f,.4f,.46f,.4f,"wood");
        B("Bathtub",6.5f,3.3f,19.15f,.75f,.6f,1.45f,"cream");
        B("Toilet",5.05f,3.25f,19.5f,.42f,.5f,.65f,"cream");
        B("Coat cupboard",3.55f,4,19.25f,.65f,2,1,"wood");
        Point("Apartment weak tungsten",new Vector3(6,5.3f,15.4f),new Color(1,.82f,.57f),7,2);
        Point("Kitchen light",new Vector3(9,5.3f,18.5f),new Color(1,.85f,.64f),5,2);
        FitApartmentToFacade();
    }
    static Transform Direct(Transform parent,string name) { foreach(Transform t in parent)if(t.name==name)return t;return null; }
    static void FitApartmentToFacade()
    {
        Transform apartment=group;
        string[] remove={"South outer wall","Window / old frame","Cold glass","Mullion","Sill","Bedroom wall left","Bedroom wall right","Bedroom wall lintel","East outer wall","North apartment wall","Living / kitchen partition","Kitchen doorway left","Kitchen doorway right","Kitchen doorway lintel","Bathroom door left","Bathroom door right","Bathroom door lintel"};
        foreach(var t in apartment.GetComponentsInChildren<Transform>())
            if(System.Array.IndexOf(remove,t.name)>=0)Object.DestroyImmediate(t.gameObject);
        void Place(string n,Vector3 p,Vector3? s=null) { var t=apartment.Find(n);if(!t)return;t.position=p;if(s.HasValue)t.localScale=s.Value; }
        Place("Apartment floor",new Vector3(9,2.9f,16),new Vector3(12,.2f,8));
        Place("Kitchen linoleum",new Vector3(12.8f,3.012f,14.5f),new Vector3(4.4f,.018f,5));
        Place("Bathroom tile",new Vector3(9.3f,3.015f,19.1f),new Vector3(2.6f,.025f,1.8f));
        // Separate rooms all open onto the rear corridor; each takes one existing facade bay.
        B("Living / bedroom partition",7,4.35f,14.5f,.18f,2.7f,5,"wallpaper");
        B("Bedroom / kitchen partition",10.6f,4.35f,14.5f,.18f,2.7f,5,"wallpaper");
        DoorX("Living rear doorway",5.75f,3,17,2.5f,5.7f,1.2f,"wallpaper");
        DoorX("Bedroom rear doorway",8.8f,3,17,3.6f,8.4f,1.2f,"wallpaper");
        DoorX("Kitchen rear doorway",12.8f,3,17,4.4f,12.4f,1.2f,"wallpaper");
        DoorX("Bathroom rear corridor door",9.3f,3,18.2f,2.6f,9.3f,1.1f,"wallpaper");
        B("Bathroom west",8,4.35f,19.1f,.18f,2.7f,1.8f,"wallpaper");
        B("Bathroom east",10.6f,4.35f,19.1f,.18f,2.7f,1.8f,"wallpaper");
        B("Rear apartment wall",9,4.35f,20,12,2.7f,.18f,"wallpaper");
        B("Closed rear west utility",6.25f,4.35f,19.1f,3.3f,2.7f,1.8f,"wallpaper");
        B("Closed rear east utility",12.85f,4.35f,19.1f,4.3f,2.7f,1.8f,"wallpaper");
        Place("Sofa",new Vector3(5.7f,3.28f,16.45f),new Vector3(2.2f,.55f,.8f));
        Place("Sofa back",new Vector3(5.7f,3.65f,16.85f),new Vector3(2.2f,.85f,.15f));
        // Keep the rear living doorway clear: sofa moves against the side wall instead.
        Place("Sofa",new Vector3(6.5f,3.28f,15.5f),new Vector3(.8f,.55f,2));
        Place("Sofa back",new Vector3(6.85f,3.65f,15.5f),new Vector3(.15f,.85f,2));
        Place("TV stand",new Vector3(6.2f,3.3f,13.4f),new Vector3(1.1f,.6f,.5f));
        Place("CRT television",new Vector3(6.2f,3.9f,13.4f));Place("CRT screen",new Vector3(6.2f,3.9f,13.69f));
        Place("Wardrobe",new Vector3(7.55f,4.05f,15.1f));
        Place("Bed base",new Vector3(9.7f,3.22f,14.3f));Place("Blanket",new Vector3(9.7f,3.5f,14.3f));Place("Pillow",new Vector3(9.7f,3.63f,13.6f));
        foreach(var t in apartment.GetComponentsInChildren<Transform>())
            if(t.name=="Kitchen counter"||t.name=="Sink"||t.name=="Gas stove"||t.name=="Burner"||t.name=="Refrigerator"||t.name=="Small kitchen table"||t.name=="Table pedestal"||t.name=="Stool"||t.name=="Kitchen light") t.position+=new Vector3(3.6f,0,-4);
        Place("Refrigerator",new Vector3(14.3f,3.85f,16.4f));
        Place("Bathtub",new Vector3(10.1f,3.3f,19.15f));Place("Toilet",new Vector3(8.55f,3.25f,19.5f));
        // Remove only the old facade parts in this apartment bay; preserve the rest of the house.
        var house=Direct(root,"06 • Наш дом / 5 этажей");
        foreach(var r in house.GetComponentsInChildren<Renderer>())
        {
            var p=r.transform.position;var b=r.bounds;
            if(p.z<12.4f && p.z>11.5f && p.y>3.15f && p.y<5.95f && b.max.x>3.1f)
            {
                if(b.min.x<3) {float start=b.min.x;r.transform.position=new Vector3((start+3)/2,p.y,p.z);var s=r.transform.localScale;s.x=3-start;r.transform.localScale=s;}
                else Object.DestroyImmediate(r.gameObject);
            }
        }
        var closed=Direct(Direct(house,"Upper floors / camera cutaway"),"Other flats / upper east");if(closed)Object.DestroyImmediate(closed.gameObject);
        var ceiling=Direct(Direct(house,"Upper floors / camera cutaway"),"Apartment ceiling");ceiling.position=new Vector3(9,5.84f,16);ceiling.localScale=new Vector3(12,.22f,8);
        var front=Group("Facade-aligned windows / camera cutaway",apartment);group=front;
        B("Window wall lower",9,3.35f,12,12,.7f,.25f,"wallpaper");
        B("Window wall upper",9,5.54f,12,12,.32f,.25f,"wallpaper");
        float cursor=3;
        foreach(float x in new[]{5.2f,8.8f,12.4f})
        {
            float left=x-.725f,right=x+.725f;
            B("Window wall pier",(cursor+left)/2,4.54f,12,left-cursor,1.68f,.25f,"wallpaper");
            B("Window jamb left",left+.04f,4.55f,12,.08f,1.65f,.20f,"frame");
            B("Window jamb right",right-.04f,4.55f,12,.08f,1.65f,.20f,"frame");
            B("Window frame head",x,5.335f,12,1.45f,.08f,.20f,"frame");
            B("Window frame foot",x,3.765f,12,1.45f,.08f,.20f,"frame");
            B("Window glass",x,4.55f,12,1.29f,1.49f,.035f,"glass");
            B("Window mullion",x,4.55f,12,.055f,1.49f,.20f,"frame");
            B("Window sill",x,3.69f,12.10f,1.65f,.10f,.42f,"concrete");
            cursor=right;
        }
        B("Window wall last pier",(cursor+15)/2,4.54f,12,15-cursor,1.68f,.25f,"wallpaper");
        Collect(front,homeCut);group=apartment;
        SealApartmentWalls(apartment);
        Physics.SyncTransforms();
    }
    public static void RepairApartmentOnly()
    {
        root=GameObject.Find("СЕВЕР • 1994 • BLOCKOUT").transform;
        var w=Object.FindAnyObjectByType<BlockoutWalker>();w.fixedMode=false;w.RefreshView();
        mats=new();foreach(string path in Directory.GetFiles(AssetDatabase.IsValidFolder(Dir+"/Materials")?Dir+"/Materials":"Assets/Materials","*.mat"))mats[Path.GetFileNameWithoutExtension(path)]=AssetDatabase.LoadAssetAtPath<Material>(path);
        homeCut.Clear();foreach(Transform t in root)if(t.name.StartsWith("01 •")){Object.DestroyImmediate(t.gameObject);break;}
        Apartment();
        var valid=new List<Renderer>();foreach(var r in w.cutaways[0].nearWalls)if(r)valid.Add(r);valid.AddRange(homeCut);w.cutaways[0].nearWalls=valid.ToArray();
        var roofs=new List<Renderer>();foreach(var r in w.cutaways[0].roof)if(r)roofs.Add(r);w.cutaways[0].roof=roofs.ToArray();
        w.fixedViews[0].position=new Vector3(5.7f,15,11);w.fixedViews[0].LookAt(new Vector3(5.7f,3.6f,14.5f));w.fixedViews[2].position=new Vector3(8.8f,15,11);w.fixedViews[2].LookAt(new Vector3(8.8f,3.6f,14.5f));w.fixedViews[1].position=new Vector3(11,22,15);w.fixedViews[1].LookAt(new Vector3(10.2f,3.5f,17.2f));w.Teleport(new Vector3(5.3f,3.06f,15));w.RefreshView();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
    static void Shop(bool bakery)
    {
        float cx=bakery?16:-14,cz=bakery?-18:-17, w=bakery?16:14,d=bakery?12:10;
        float front=cz+d/2,back=cz-d/2,door=cx-3.5f;
        group=Group(bakery?"04 • Булочная / first floor":"03 • Продукты / 1994");
        B("Shop tiled floor",cx,-.08f,cz,w,.16f,d,"tile");
        B("Back wall",cx,1.6f,back,w,3.2f,.25f,bakery?"cream":"green");
        B("West wall",cx-w/2,1.6f,cz,.25f,3.2f,d,"plaster");
        var cut=Group("Near walls / camera cutaway",group);var old=group;group=cut;
        B("East wall",cx+w/2,1.6f,cz,.25f,3.2f,d,"plaster");
        DoorX("Shopfront",cx,0,front,w,door,1.8f,"plaster",3.2f);
        Collect(cut,bakery?bakeryCut:shopCut);group=old;
        // Open door, glazed display panels on the solid storefront.
        B("Open door leaf",door-.95f,1.1f,front+.55f,.10f,2.2f,1.1f,"frame");
        for(int i=0;i<3;i++) B("Display window",cx+i*2.4f-1,1.4f,front+.14f,2,1.6f,.06f,"glass");
        B("Faded sign panel",cx,2.85f,front+.22f,w-.8f,.65f,.18f,bakery?"cream":"red");
        Label(bakery?"Б У Л О Ч Н А Я":"П Р О Д У К Т Ы",new Vector3(cx,2.86f,front+.34f),.19f,bakery?new Color(.24f,.15f,.10f):new Color(.9f,.85f,.68f),180);
        Label("8–20",new Vector3(door+1.2f,1.6f,front+.26f),.12f,Color.white,180);
        foreach(var r in group.GetComponentsInChildren<Renderer>())
            if(r.bounds.min.z > front-.3f)(bakery?bakeryCut:shopCut).Add(r);
        var roof=Group("Ceiling / camera cutaway",group);
        B("Ceiling",cx,3.25f,cz,w+.2f,.25f,d+.2f,"roof",roof);Collect(roof,bakery?bakeryRoof:shopRoof);
        if(bakery)
        {
            B("Bread counter base",16.8f,.48f,-18,6.2f,.96f,1.05f,"wood");
            B("Counter glass block",16.8f,1.16f,-18,6.2f,.35f,1,"glass");
            for(int i=0;i<8;i++) B("Loaf",14.2f+i*.72f,1.4f,-18,.5f,.16f,.27f,"bread");
            for(int s=0;s<3;s++)
            {
                B("Bread shelf",16, .6f+s*.65f,-23.5f,7,.10f,.65f,"wood");
                for(int i=0;i<7;i++)B("Bread on shelf",13.4f+i*.85f,.76f+s*.65f,-23.5f,.6f,.22f,.35f,"bread");
            }
            B("Oven silhouette",22,1.1f,-22.7f,2,2.2f,1.4f,"metal");
            B("Oven door",22,1.1f,-21.95f,1.5f,.65f,.08f,"glass");
            B("Preparation table",19.5f,.86f,-22.6f,2,.12f,1.2f,"metal");
            B("Cash register",19.2f,1.35f,-18,.6f,.4f,.5f,"cream");
            B("Customer standing table",10,1,-16,.9f,.1f,.9f,"wood");
            B("Standing table base",10,.48f,-16,.15f,1,.15f,"metal");
            Point("Bakery warm pool",new Vector3(16,2.7f,-17),new Color(1,.69f,.32f),12,4);
        }
        else
        {
            for(int row=0;row<2;row++)
            {
                float x=-14.8f+row*3.3f;
                B("Gondola base",x,.15f,-17.5f,1.1f,.3f,4,"green");
                for(int shelf=0;shelf<3;shelf++)
                {
                    B("Gondola shelf",x,.5f+shelf*.48f,-17.5f,1.1f,.08f,4,"cream");
                    for(int i=0;i<5;i++)
                    {
                        string mat=(i+shelf)%3==0?"red":(i+shelf)%3==1?"cream":"green";
                        B("Sparse groceries / packs",x,.7f+shelf*.48f,-19+i*.65f,.45f,.3f,.35f,mat);
                    }
                }
            }
            B("Checkout",-9,.55f,-14,2.8f,1.1f,.9f,"wood");
            B("Till",-9.5f,1.3f,-14,.65f,.4f,.5f,"cream");
            B("Scale",-8.3f,1.25f,-14,.55f,.3f,.45f,"metal");
            B("Milk refrigerator",-20,1,-19.8f,1.3f,2,2.8f,"cream");
            B("Refrigerator front",-19.3f,1.15f,-19.8f,.05f,1.3f,2.5f,"glass");
            for(int i=0;i<4;i++) B("Wooden produce crate",-19+i*1.5f,.35f,-21.3f,1.1f,.7f,1,"wood");
            Point("Cold fluorescent",new Vector3(-14,2.8f,-17),new Color(.72f,.86f,.80f),13,3.6f);
        }
    }
    static void Pole(Vector3 p)
    {
        float towardRoad=p.z>-3.5f?-1:1;
        B("Street lamp pole",p.x,3.3f,p.z,.13f,6.6f,.13f,"metal");
        B("Lamp arm",p.x,6.5f,p.z+towardRoad*.65f,.12f,.12f,1.4f,"metal");
        B("Sodium lamp",p.x,6.4f,p.z+towardRoad*1.3f,.4f,.15f,.8f,"warm");
        Point("Sodium light",p+new Vector3(0,6.1f,towardRoad*1.3f),new Color(1,.62f,.25f),13,3);
    }
    public static int RepairStreetLampDirections()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode before saving lamp changes.");
        var all=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        int count=0;
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Face street lamps toward road");
        foreach(var pole in all)
        {
            if(pole.name!="Street lamp pole")continue;
            float direction=pole.position.z>-3.5f?-1:1;
            foreach(var part in all)
            {
                if(part.parent!=pole.parent||Mathf.Abs(part.position.x-pole.position.x)>.05f||Mathf.Abs(part.position.z-pole.position.z)>2)continue;
                float offset=part.name=="Lamp arm"?.65f:part.name=="Sodium lamp"||part.name=="Sodium light"?1.3f:0;
                if(offset==0)continue;
                Undo.RecordObject(part,"Face lamp toward road");var p=part.position;p.z=pole.position.z+direction*offset;part.position=p;
            }
            count++;
        }
        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());return count;
    }
    static void Branch(Vector3 a,Vector3 b,float width)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="Bare branch";g.transform.SetParent(group);
        g.transform.position=(a+b)/2;g.transform.up=(b-a).normalized;g.transform.localScale=new Vector3(width,(b-a).magnitude/2,width);
        g.GetComponent<Renderer>().sharedMaterial=M("bark");NonSolid(g);
    }
    static void Tree(float x,float z,int seed)
    {
        var old=group;group=Group("Bare northern tree",group);
        float h=5.8f+(seed%3)*.7f;
        B("Trunk collider",x,1.7f,z,.25f,3.4f,.25f,"bark");
        Branch(new Vector3(x,0,z),new Vector3(x+.15f,h,z),.24f);
        for(int i=0;i<5;i++)
        {
            float a=i*2.4f+seed;Vector3 start=new Vector3(x,h*.38f+i*.48f,z);
            Vector3 end=start+new Vector3(Mathf.Sin(a)*(1.3f+i*.13f),1.7f,Mathf.Cos(a)*1.6f);
            Branch(start,end,.10f);Branch(Vector3.Lerp(start,end,.65f),end+new Vector3(.5f,.6f,-.3f),.045f);
        }
        group=old;
    }
    static void Outside()
    {
        group=Group("05 • Двор, дорога и границы");
        B("Ground",0,-.4f,0,118,.6f,90,"soil");
        B("Two-way road / 7 m",0,-.04f,-3.5f,110,.08f,7,"asphalt");
        B("Near pavement / 2.6 m",0,-.02f,1.8f,110,.1f,2.6f,"concrete");
        B("Far pavement / 3 m",0,-.02f,-9.5f,110,.1f,3,"concrete");
        for(int side=0;side<2;side++)
        {
            float z=side==0?.3f:-7.3f;
            // Leave a level crossing at x=-3.5; curbs cannot trap the controller.
            B("Curb west",-30,.10f,z,47,.2f,.22f,"concrete");
            B("Curb east",25,.10f,z,49,.2f,.22f,"concrete");
        }
        for(int i=-6;i<=6;i++) B("Worn center dash",i*7,-.0f,-3.5f,2.2f,.015f,.10f,"marking");
        B("Entrance footpath / 2.4 m",0,-.015f,7.6f,2.4f,.12f,8.8f,"concrete");
        B("Courtyard path",-4,-.015f,5.2f,10,.12f,2.2f,"concrete");
        B("Path toward crossing",-8,-.015f,3.5f,2.2f,.12f,3.4f,"concrete");
        B("Shop path",-17.5f,-.015f,-11.1f,2,.12f,3,"concrete");
        B("Bakery path",12.5f,-.015f,-11.1f,2,.12f,3,"concrete");
        // Sparse patches of dirty old snow, concentrated at edges.
        for(int i=0;i<20;i++)
        {
            float x=-45+(i*17%91),z=i%2==0?9:-26;
            NonSolid(B("Old snow",x,-.055f,z,2.6f+(i%3),.10f,1.4f,"snow"));
        }
        for(int i=0;i<8;i++)Tree(-30+i*8,7.8f,i);
        for(int i=0;i<5;i++)Tree(-35+i*16,-27,10+i);
        for(int i=0;i<4;i++) { Pole(new Vector3(-32+i*21,0,1));Pole(new Vector3(-23+i*21,0,-8.2f)); }
        B("Bench seat",-4,.48f,9,2.5f,.12f,.5f,"wood");
        B("Bench back",-4,.87f,9.23f,2.5f,.65f,.10f,"green");
        for(int i=0;i<2;i++) B("Bench legs",-4.9f+i*1.8f,.22f,9,.12f,.44f,.5f,"metal");
        B("Sandpit",-10,.13f,7.5f,3,.26f,2.8f,"rust");
        B("Sandpit fill",-10,.28f,7.5f,2.6f,.08f,2.4f,"soil");
        B("Bin",3,.4f,10,.6f,.8f,.6f,"green");
        for(int i=0;i<2;i++) B("Dumpster",29+i*2.2f,.65f,10,1.5f,1.3f,1.3f,"green");
        B("Heating pipe",-27,.7f,32,40,.4f,.4f,"rust");
        // Recognisable single Lada-like silhouette, without small detail.
        B("Parked car chassis",-26,.5f,-5.7f,4.2f,.6f,1.7f,"car");
        B("Parked car cabin",-26,.98f,-5.7f,2.2f,.65f,1.55f,"glass");
        for(int i=0;i<4;i++) B("Car wheel",-27.3f+(i%2)*2.6f,.32f,-5.7f+(i/2==0?-.85f:.85f),.65f,.65f,.18f,"metal");
        // Street ends are visible physical barriers rather than an invisible level edge.
        for(int end=0;end<2;end++)
        {
            float x=end==0?-49:49;
            B("Road closed concrete barrier",x,.65f,-3.5f,1,1.3f,7,"concrete");
            B("Courtyard boundary",x,1.2f,10,1,2.4f,20,"concrete");
        }
        B("Rear boundary",0,1.3f,33,100,2.6f,.4f,"concrete");
        B("South boundary",0,1.3f,-32,100,2.6f,.4f,"concrete");
    }
    static void Shot(string name,Vector3 p,Vector3 target)
    {
        Transform t=Group(name);t.position=p;t.LookAt(target);shots.Add(t);
    }
    [MenuItem("Tools/North Blockout/Build location")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode before rebuilding.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Save the current scene before generating a new scene.");
        Directory.CreateDirectory(Dir+"/Materials");Directory.CreateDirectory(Dir+"/Previews");
        AssetDatabase.Refresh();mats=new();homeRoof.Clear();homeCut.Clear();shopRoof.Clear();shopCut.Clear();bakeryRoof.Clear();bakeryCut.Clear();shots.Clear();
        Material("plaster","858681");Material("concrete","666D6D");Material("roof","3C4549");Material("asphalt","3C474D");
        Material("soil","4B5148");Material("green","4F6B60");Material("rust","655145");Material("metal","303D41");
        Material("frame","A6A99B");Material("glass","354B53");Material("warm","C9AB70",true);Material("snow","B2B9B1");
        Material("wallpaper","A29F89");Material("ceiling","BCBBA8");Material("wood","6B503B");Material("tile","89958C");
        Material("linoleum","6F7B66");Material("sofa","5A665B");Material("rug","704C48");Material("cream","B9B49B");
        Material("red","83514B");Material("bread","AE8757");Material("marking","ACA991");Material("car","7B8175");Material("bark","605C51");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        root=new GameObject("СЕВЕР • 1994 • BLOCKOUT").transform;
        Outside();Housing("06 • Наш дом / 5 этажей",0,18,30,12,true);
        Stairs();Apartment();
        Housing("07 • Соседняя хрущёвка A",-32,21,26,12);
        Housing("08 • Соседняя хрущёвка Б",34,21,26,12);
        Housing("09 • Дом с булочной",16,-18,16,12,false,true);
        Shop(false);Shop(true);
        group=Group("10 • Свет / северный пасмурный день");
        RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.47f,.53f,.57f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.48f,.55f,.59f);RenderSettings.fogStartDistance=65;RenderSettings.fogEndDistance=220;
        var sun=new GameObject("Cloud-filtered daylight");sun.transform.SetParent(group);sun.transform.rotation=Quaternion.Euler(32,-35,0);
        var l=sun.AddComponent<Light>();l.type=LightType.Directional;l.intensity=1.15f;l.color=new Color(.79f,.86f,.94f);l.shadows=LightShadows.Soft;
        Shot("Shot 01 • living room",new Vector3(5.7f,15,11),new Vector3(5.7f,3.6f,14.5f));
        Shot("Shot 02 • kitchen and foyer",new Vector3(18,13,9),new Vector3(10.8f,3.5f,16));
        Shot("Shot 03 • bedroom",new Vector3(14,10,10),new Vector3(9.4f,3.5f,15));
        Shot("Shot 04 • upper stairs",new Vector3(-7,10,8),new Vector3(0,3.8f,15.8f));
        Shot("Shot 05 • entrance stairs",new Vector3(-8,8,8),new Vector3(0,1,15.8f));
        Shot("Shot 06 • courtyard",new Vector3(27,23,-13),new Vector3(0,1,9));
        Shot("Shot 07 • street",new Vector3(20,25,17),new Vector3(0,1,-10));
        Shot("Shot 08 • grocery",new Vector3(-4,14,-7),new Vector3(-14,0,-17));
        Shot("Shot 09 • bakery",new Vector3(28,15,-6),new Vector3(16,0,-18));
        group=Group("11 • Playtest / Tab changes camera");
        var player=new GameObject("Player • 1.78 m / radius .26 m");player.transform.SetParent(group);player.transform.position=new Vector3(5.3f,3.06f,15);
        var cc=player.AddComponent<CharacterController>();cc.height=1.78f;cc.radius=.26f;cc.center=new Vector3(0,.9f,0);cc.stepOffset=.22f;cc.slopeLimit=48;cc.skinWidth=.025f;
        var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name="Player silhouette / fixed cameras only";body.transform.SetParent(player.transform,false);body.transform.localPosition=new Vector3(0,.9f,0);body.transform.localScale=new Vector3(.48f,.88f,.48f);NonSolid(body);body.GetComponent<Renderer>().sharedMaterial=M("red");body.GetComponent<Renderer>().enabled=false;
        var camgo=new GameObject("Main Camera");camgo.tag="MainCamera";camgo.transform.SetParent(group);var cam=camgo.AddComponent<Camera>();camgo.AddComponent<AudioListener>();cam.fieldOfView=68;cam.farClipPlane=180;cam.backgroundColor=RenderSettings.fogColor;cam.clearFlags=CameraClearFlags.SolidColor;
        var walker=player.AddComponent<BlockoutWalker>();walker.view=cam;walker.body=new[]{body.GetComponent<Renderer>()};walker.fixedViews=shots.ToArray();
        walker.cutaways=new[]{
            new BlockoutCutaway{area=new Bounds(new Vector3(0,7,18),new Vector3(31,18,13)),roof=homeRoof.ToArray(),nearWalls=homeCut.ToArray()},
            new BlockoutCutaway{area=new Bounds(new Vector3(-14,1,-17),new Vector3(15,8,10.4f)),roof=shopRoof.ToArray(),nearWalls=shopCut.ToArray()},
            new BlockoutCutaway{area=new Bounds(new Vector3(16,1,-18),new Vector3(17,8,12.4f)),roof=bakeryRoof.ToArray(),nearWalls=bakeryCut.ToArray()}};
        walker.RefreshView();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
        AssetDatabase.SaveAssets();
        if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(0,2,0),Quaternion.Euler(38,-35,0),65);
        Selection.activeGameObject=root.gameObject;
        Debug.Log("NORTH BLOCKOUT ready: "+ScenePath);
    }
    public static void Preview(string name,Vector3 position,Vector3 target,bool ortho=false,float size=38)
    {
        var go=new GameObject("Preview camera");var cam=go.AddComponent<Camera>();go.transform.position=position;go.transform.LookAt(target);
        cam.orthographic=ortho;cam.orthographicSize=size;cam.fieldOfView=58;cam.nearClipPlane=.1f;cam.farClipPlane=250;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=RenderSettings.fogColor;
        var rt=new RenderTexture(1600,1000,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;
        var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();
        File.WriteAllBytes(Dir+"/Previews/"+name+".png",tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(go);
    }
}


