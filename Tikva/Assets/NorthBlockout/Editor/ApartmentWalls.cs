using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static partial class NorthBlockoutBuilder
{
    // A partition is one mesh, so a camera can never remove just its jamb or lintel.
    static void SolidPartition(string name, Transform apartment, List<Bounds> parts)
    {
        var go=new GameObject(name);go.transform.SetParent(apartment,false);
        var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh cube=temp.GetComponent<MeshFilter>().sharedMesh;
        var combine=new List<CombineInstance>();
        foreach(var b in parts)
        {
            combine.Add(new CombineInstance {mesh=cube,transform=Matrix4x4.TRS(b.center,Quaternion.identity,b.size)});
            var c=go.AddComponent<BoxCollider>();c.center=b.center;c.size=b.size;
        }
        string folder=Dir+"/WallMeshes";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder(Dir,"WallMeshes");
        string path=folder+"/"+name+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
        mesh.CombineMeshes(combine.ToArray(),true,true);EditorUtility.SetDirty(mesh);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=M("wallpaper");
        Object.DestroyImmediate(temp);
    }
    static void Wall(string name,Transform apt,bool alongX,float fixedAxis,float from,float to,float door=float.NaN,float width=1.2f)
    {
        var parts=new List<Bounds>();
        void Piece(float a,float b,float low,float high)
        {
            if(b-a<.001f)return;
            parts.Add(new Bounds(alongX?new Vector3((a+b)/2,(low+high)/2,fixedAxis):new Vector3(fixedAxis,(low+high)/2,(a+b)/2),
                alongX?new Vector3(b-a,high-low,.20f):new Vector3(.20f,high-low,b-a)));
        }
        if(float.IsNaN(door))Piece(from,to,3,5.7f);
        else {Piece(from,door-width/2,3,5.7f);Piece(door+width/2,to,3,5.7f);Piece(door-width/2,door+width/2,5.2f,5.7f);}
        SolidPartition(name,apt,parts);
    }
    static void SealApartmentWalls(Transform apt)
    {
        foreach(var t in apt.GetComponentsInChildren<Transform>())
        {
            if(!t || t==apt)continue;
            string n=t.name;
            if(n.StartsWith("Hall to ")||n=="Living / bedroom partition"||n=="Bedroom / kitchen partition"||
                n.StartsWith("Living rear doorway")||n.StartsWith("Bedroom rear doorway")||n.StartsWith("Kitchen rear doorway")||
                n.StartsWith("Bathroom rear corridor door")||n=="Bathroom west"||n=="Bathroom east"||n=="Rear apartment wall"||
                n=="Closed rear west utility"||n=="Closed rear east utility"||n.StartsWith("Window wall "))
                Object.DestroyImmediate(t.gameObject);
        }
        Wall("Partition_LivingWest",apt,false,4.5f,12,17,14.8f);
        Wall("Partition_EntranceHall",apt,false,4.5f,17,20,17.6f);
        Wall("Partition_LivingBedroom",apt,false,7,12,17);
        Wall("Partition_BedroomKitchen",apt,false,10.6f,12,17);
        Wall("Partition_LivingDoor",apt,true,17,4.5f,7,5.7f);
        Wall("Partition_BedroomDoor",apt,true,17,7,10.6f,8.4f);
        Wall("Partition_KitchenDoor",apt,true,17,10.6f,15,12.4f);
        Wall("Partition_RearWest",apt,true,18.2f,4.5f,8);
        Wall("Partition_BathroomDoor",apt,true,18.2f,8,10.6f,9.3f,1.1f);
        Wall("Partition_RearEast",apt,true,18.2f,10.6f,15);
        Wall("Partition_BathroomWest",apt,false,8,18.2f,20);
        Wall("Partition_BathroomEast",apt,false,10.6f,18.2f,20);
        Wall("Partition_RearPerimeter",apt,true,20,3,15);
        // The only other openings in the envelope are the three facade windows.
        var parts=new List<Bounds>();
        void Front(float a,float b,float low,float high)=>parts.Add(new Bounds(new Vector3((a+b)/2,(low+high)/2,12),new Vector3(b-a,high-low,.25f)));
        Front(3,15,3,3.725f);Front(3,15,5.375f,5.7f);
        float edge=3;foreach(float x in new[]{5.2f,8.8f,12.4f}) {Front(edge,x-.725f,3.725f,5.375f);edge=x+.725f;}
        Front(edge,15,3.725f,5.375f);SolidPartition("Partition_Facade",apt,parts);
        AssetDatabase.SaveAssets();
    }
}
