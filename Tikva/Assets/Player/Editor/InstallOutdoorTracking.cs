using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallOutdoorTracking
{
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode.");
        var rig=Object.FindAnyObjectByType<SurvivalCameraRig>();
        var zones=Object.FindObjectsByType<FixedCameraZone>().Where(z=>!z.name.StartsWith("outside_camera")).ToArray();
        string Snapshot()=>string.Join("|",zones.Select(z=>EditorJsonUtility.ToJson(z)+
            (z.volume?EditorJsonUtility.ToJson(z.volume)+EditorJsonUtility.ToJson(z.volume.transform):"")));
        string before=Snapshot();
        var tagManager=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags=tagManager.FindProperty("tags");bool exists=false;for(int i=0;i<tags.arraySize;i++)if(tags.GetArrayElementAtIndex(i).stringValue=="CameraBlindZone")exists=true;
        if(!exists){tags.InsertArrayElementAtIndex(tags.arraySize);tags.GetArrayElementAtIndex(tags.arraySize-1).stringValue="CameraBlindZone";tagManager.ApplyModifiedProperties();}
        Transform Root(string name){var g=GameObject.Find(name);return g?g.transform:new GameObject(name).transform;}
        var areas=Root("Outdoor camera zones");var blindRoot=Root("Camera blind zones - edit triggers");
        OutdoorTrackingCamera Shot(string name,float minX,float maxX)
        {
            var camera=Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).First(c=>c.name.Trim()==name);
            var old=camera.GetComponent<FixedCameraZone>();if(old){old.enabled=false;EditorUtility.SetDirty(old);}
            var shot=camera.GetComponent<OutdoorTrackingCamera>();if(!shot)shot=camera.gameObject.AddComponent<OutdoorTrackingCamera>();
            var area=new GameObject(name+" coverage");area.transform.SetParent(areas);area.transform.position=new Vector3((minX+maxX)/2,4,.5f);area.layer=2;
            shot.volume=area.AddComponent<BoxCollider>();shot.volume.isTrigger=true;shot.volume.size=new Vector3(maxX-minX,10,23);
            shot.normalFieldOfView=camera.fieldOfView;shot.farFieldOfView=32;shot.rotationDelay=.35f;shot.zoomDelay=.6f;
            camera.enabled=false;foreach(var listener in camera.GetComponents<AudioListener>())Object.DestroyImmediate(listener);
            var ownBox=camera.GetComponent<BoxCollider>();if(ownBox)ownBox.enabled=false;
            EditorUtility.SetDirty(shot);EditorUtility.SetDirty(camera);return shot;
        }
        // Camera 1 covers the grocery half; camera 2 covers the bakery half.
        var shots=new[]{Shot("outside_camera_1",-49,1.7f),Shot("outside_camera_2",1.7f,49)};
        if(rig.occlusionFader){rig.occlusionFader.RestoreAll();rig.occlusionFader.enabled=false;EditorUtility.SetDirty(rig.occlusionFader);}
        rig.lostVisibilityDelay=.35f;rig.visibleRecoveryDelay=.7f;rig.minimumShotHold=.8f;rig.boundaryMargin=.6f;
        Physics.SyncTransforms();
        // Coarse initial blind-zone map. Dynamic sight tests remain authoritative between markers.
        const int nx=24,nz=16;const float cell=4;var blind=new bool[nx,nz];
        for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)
        {
            Vector3 p=new Vector3(-46+x*cell,0,-30+z*cell);
            if(!Physics.Raycast(p+Vector3.up*40,Vector3.down,out var ground,45,~0,QueryTriggerInteraction.Ignore)||ground.point.y>.5f)continue;
            p.y=ground.point.y+.05f;
            if(zones.Any(zone=>zone.Contains(p)))continue;
            if(rig.buildingExclusionVolumes.Any(b=>b && p.x>=b.bounds.min.x && p.x<=b.bounds.max.x && p.z>=b.bounds.min.z && p.z<=b.bounds.max.z))continue;
            blind[x,z]=!shots.Any(shot=>shot.Contains(p)&&shot.CanSeePoint(p));
        }
        int count=0;
        for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
        {
            if(!blind[x,z])continue;int width=1,height=1;
            while(x+width<nx&&blind[x+width,z])width++;
            while(z+height<nz&&Enumerable.Range(x,width).All(xx=>blind[xx,z+height]))height++;
            for(int a=x;a<x+width;a++)for(int b=z;b<z+height;b++)blind[a,b]=false;
            var g=new GameObject("BlindZone_"+(++count).ToString("00"));g.tag="CameraBlindZone";g.layer=2;g.transform.SetParent(blindRoot);
            g.transform.position=new Vector3(-46+x*cell+(width-1)*cell/2,2,-30+z*cell+(height-1)*cell/2);
            var marker=g.AddComponent<CameraBlindZone>();marker.GetComponent<BoxCollider>().size=new Vector3(width*cell,6,height*cell);marker.GetComponent<BoxCollider>().isTrigger=true;
        }
        if(before!=Snapshot())throw new System.InvalidOperationException("An existing camera zone changed.");
        EditorUtility.SetDirty(rig);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "Existing zones unchanged; 2 outdoor zones; "+count+" editable blind-zone markers.";
    }
}
