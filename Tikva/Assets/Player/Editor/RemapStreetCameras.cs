using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class RemapStreetCameras
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode.");
        var cameras=Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
        var positions=cameras.ToDictionary(c=>c,c=>c.transform.position);
        var rotations=cameras.ToDictionary(c=>c,c=>c.transform.rotation);
        void Coverage(string name,float min,float max)
        {
            var tracking=cameras.First(c=>c.name==name).GetComponent<OutdoorTrackingCamera>();
            var b=tracking.volume;Undo.RecordObjects(new Object[]{b,b.transform},"Assign street half");
            var p=b.transform.position;p.x=(min+max)/2;b.transform.position=p;
            var center=b.center;center.x=0;b.center=center;var size=b.size;size.x=max-min;b.size=size;
            tracking.enabled=true;EditorUtility.SetDirty(tracking);EditorUtility.SetDirty(b);EditorUtility.SetDirty(b.transform);
        }
        Coverage("outside_camera_1",-49,1.7f);Coverage("outside_camera_2",1.7f,49);
        var entrance=cameras.First(c=>c.name.Trim()=="entrance_camera"&&c.GetComponent<OutdoorTrackingCamera>());
        var outdoor=entrance.GetComponent<OutdoorTrackingCamera>();outdoor.enabled=false;EditorUtility.SetDirty(outdoor);
        var zone=entrance.GetComponent<FixedCameraZone>();if(!zone)zone=entrance.gameObject.AddComponent<FixedCameraZone>();
        var volume=GameObject.Find("Entrance approach camera zone");
        if(!volume){volume=new GameObject("Entrance approach camera zone");var parent=GameObject.Find("Camera activation areas");if(parent)volume.transform.SetParent(parent.transform);}
        volume.layer=2;volume.transform.SetPositionAndRotation(new Vector3(0,1.5f,5.8f),Quaternion.identity);
        var box=volume.GetComponent<BoxCollider>();if(!box)box=volume.AddComponent<BoxCollider>();box.center=Vector3.zero;box.size=new Vector3(6,4,7.6f);box.isTrigger=true;
        zone.volume=box;zone.additionalVolumes=new BoxCollider[0];zone.priority=19;zone.requireVisibleInCamera=false;zone.enabled=true;
        zone.GetComponent<BoxCollider>().enabled=false;entrance.enabled=false;
        foreach(var listener in entrance.GetComponents<AudioListener>())Undo.DestroyObjectImmediate(listener);
        EditorUtility.SetDirty(zone);EditorUtility.SetDirty(entrance);EditorUtility.SetDirty(box);
        if(cameras.Any(c=>c.transform.position!=positions[c]||c.transform.rotation!=rotations[c]))throw new System.InvalidOperationException("A camera pose changed.");
        Physics.SyncTransforms();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
    public static object Check()
    {
        if(!Application.isPlaying)throw new System.InvalidOperationException("Play mode required.");
        var p=Object.FindAnyObjectByType<SurvivalPlayerController>();var rig=p.cameraRig;
        var original=p.transform.position;var rotation=p.transform.rotation;bool mode=p.firstPerson;p.SetFirstPerson(false);
        var checks=new System.Collections.Generic.Dictionary<string,bool>();
        void At(Vector3 point){p.Teleport(point);for(int i=0;i<150;i++)rig.UpdateView(1f/60,0);p.SetFirstPerson(p.firstPerson);}
        At(new Vector3(-20,.05f,0));checks["grocery half uses outside 1"]=rig.ActiveOutdoorCamera && rig.ActiveOutdoorCamera.name=="outside_camera_1"&&!rig.IsFirstPerson;
        At(new Vector3(20,.05f,0));checks["bakery half uses outside 2"]=rig.ActiveOutdoorCamera && rig.ActiveOutdoorCamera.name=="outside_camera_2"&&!rig.IsFirstPerson;
        At(new Vector3(0,.05f,6));checks["new entrance approach is static"]=rig.ActiveFixedCamera && rig.ActiveFixedCamera.name=="entrance_camera" && rig.ActiveFixedCamera.transform.position.z<2&&!rig.IsFirstPerson;
        At(new Vector3(0,.05f,13));checks["original staircase camera retained"]=rig.ActiveFixedCamera && rig.ActiveFixedCamera.name=="entrance_camera" && rig.ActiveFixedCamera.transform.position.z>12;
        At(new Vector3(-17.5f,.05f,-10));checks["shop approach retained"]=rig.ActiveFixedCamera&&rig.ActiveFixedCamera.name.Trim()=="shop_camera";
        At(new Vector3(12.5f,.05f,-15));checks["bakery interior retained"]=rig.ActiveFixedCamera&&rig.ActiveFixedCamera.name=="bakery_inside_camera";
        checks["exactly two active tracking cameras"]=OutdoorTrackingCamera.Active.Count(c=>c && c.isActiveAndEnabled)==2;
        p.Teleport(original);p.transform.rotation=rotation;p.SetFirstPerson(mode);rig.UpdateView(.016f,0);return checks;
    }
}
