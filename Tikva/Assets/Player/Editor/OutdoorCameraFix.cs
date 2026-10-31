using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OutdoorCameraFix
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode.");
        var rig=Object.FindAnyObjectByType<SurvivalCameraRig>();
        var root=GameObject.Find("Outdoor camera building bounds");if(!root)root=new GameObject("Outdoor camera building bounds");
        var boxes=new List<BoxCollider>();
        void Building(string name,Vector3 center,Vector3 size)
        {
            var child=root.transform.Find(name);if(!child){child=new GameObject(name).transform;child.SetParent(root.transform);}
            child.position=center;child.rotation=Quaternion.identity;child.localScale=Vector3.one;child.gameObject.layer=2;
            var box=child.GetComponent<BoxCollider>();if(!box)box=child.gameObject.AddComponent<BoxCollider>();
            box.center=Vector3.zero;box.size=size;box.isTrigger=true;boxes.Add(box);
        }
        Building("Home",new Vector3(0,7.8f,18),new Vector3(30.5f,15.6f,12.5f));
        Building("West housing",new Vector3(-32,7.8f,21),new Vector3(26.5f,15.6f,12.5f));
        Building("East housing",new Vector3(34,7.8f,21),new Vector3(26.5f,15.6f,12.5f));
        Building("Bakery housing",new Vector3(16,7.8f,-18),new Vector3(16.5f,15.6f,12.5f));
        Building("Grocery",new Vector3(-14,1.8f,-17),new Vector3(14.5f,3.6f,10.5f));
        rig.buildingExclusionVolumes=boxes.ToArray();rig.outdoorYaw=0;rig.avoidObstacles=true;rig.buildingClearance=1;
        foreach(var zone in Object.FindObjectsByType<FixedCameraZone>())
            if(zone.Shot.name=="entrance_camera" && zone.volume)
            {
                var volume=zone.volume;var p=volume.transform.position;p.z=16.8f;volume.transform.position=p;
                var size=volume.size;size.z=14.4f;volume.size=size;
                EditorUtility.SetDirty(volume);EditorUtility.SetDirty(volume.transform);
            }
        EditorUtility.SetDirty(rig);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
    public static object Check()
    {
        if(!Application.isPlaying)throw new System.InvalidOperationException("Play mode required.");
        var p=Object.FindAnyObjectByType<SurvivalPlayerController>();var rig=p.cameraRig;
        Vector3 origin=p.transform.position;Quaternion rot=p.transform.rotation;bool fps=p.firstPerson;
        p.SetFirstPerson(false);int tooClose=0,inside=0;float minimum=float.MaxValue,largestStep=0;
        var points=new List<Vector3>();
        for(float z=11.8f;z>=3;z-=.1f)points.Add(new Vector3(0,.05f,z));
        // Traverse the lamp line: poles must not affect the boom at all.
        for(float x=-34;x<=40;x+=.3f)points.Add(new Vector3(x,.05f,1.8f));
        bool first=true;Vector3 previous=Vector3.zero;
        foreach(var point in points)
        {
            p.Teleport(point);rig.UpdateView(1f/60,0);
            if(rig.ActiveFixedCamera){first=true;continue;}
            Vector3 camera=rig.output.transform.position;float distance=Vector3.Distance(camera,point+Vector3.up);
            minimum=Mathf.Min(minimum,distance);if(distance<8)tooClose++;
            foreach(var box in rig.buildingExclusionVolumes)if(box.bounds.Contains(camera))inside++;
            if(!first && Mathf.Abs(point.x)<.01f)largestStep=Mathf.Max(largestStep,Vector3.Distance(camera,previous));
            first=false;previous=camera;
        }
        // Sweep possible endpoints across the opposite buildings as well.
        foreach(var point in new[]{new Vector3(10,.05f,-9),new Vector3(-18,.05f,-10),new Vector3(0,.05f,26)})
        {
            p.Teleport(point);for(int i=0;i<100;i++)rig.UpdateView(1f/60,0);
            foreach(var box in rig.buildingExclusionVolumes)if(box.bounds.Contains(rig.output.transform.position))inside++;
        }
        p.Teleport(origin);p.transform.rotation=rot;p.SetFirstPerson(fps);rig.UpdateView(1f/60,0);
        return new{samples=points.Count,tooClose,insideBuildings=inside,minimumDistance=minimum,largestEntranceCameraStep=largestStep};
    }
}
