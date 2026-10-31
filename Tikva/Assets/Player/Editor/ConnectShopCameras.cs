using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ConnectShopCameras
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode.");
        var cameras=Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
        var player=Object.FindAnyObjectByType<SurvivalPlayerController>();
        var parent=GameObject.Find("Camera activation areas");
        if(!parent)parent=new GameObject("Camera activation areas");
        Camera Connect(string name,Vector3 center,Vector3 size,int priority)
        {
            var camera=cameras.First(c=>c.name.Trim()==name);
            var zone=camera.GetComponent<FixedCameraZone>();if(!zone)zone=camera.gameObject.AddComponent<FixedCameraZone>();
            var volume=parent.transform.Find(name+" activation area");
            if(!volume){volume=new GameObject(name+" activation area").transform;volume.SetParent(parent.transform);}
            volume.gameObject.layer=2;volume.position=center;volume.rotation=Quaternion.identity;volume.localScale=Vector3.one;
            var box=volume.GetComponent<BoxCollider>();if(!box)box=volume.gameObject.AddComponent<BoxCollider>();
            box.center=Vector3.zero;box.size=size;box.isTrigger=true;
            Undo.RecordObject(zone,"Assign shop camera zone");zone.volume=box;zone.additionalVolumes=new BoxCollider[0];
            zone.priority=priority;zone.requireVisibleInCamera=false;zone.enabled=true;
            zone.GetComponent<BoxCollider>().enabled=false;camera.enabled=false;
            foreach(var listener in camera.GetComponents<AudioListener>())Undo.DestroyObjectImmediate(listener);
            EditorUtility.SetDirty(camera);EditorUtility.SetDirty(zone);EditorUtility.SetDirty(box);return camera;
        }
        Connect("shop_camera",new Vector3(-14,1.5f,-9.5f),new Vector3(16,4,5),40);
        var shop=Connect("shop_inside_camera",new Vector3(-14,1.5f,-17),new Vector3(14,4,10.1f),50);
        Connect("bakery_camera",new Vector3(16,1.5f,-9.5f),new Vector3(18,4,5),40);
        var bakery=Connect("bakery_inside_camera",new Vector3(16,1.5f,-18),new Vector3(16,4,12.1f),50);
        foreach(var area in player.cameraRig.interiors)
        {
            if(area.name=="Grocery interior")area.fallbackCamera=shop;
            if(area.name=="Bakery interior")area.fallbackCamera=bakery;
            EditorUtility.SetDirty(area);
        }
        player.tankControlsIndoors=true;player.tankControlsOutdoors=true;EditorUtility.SetDirty(player);
        player.cameraRig.RefreshSelection();player.cameraRig.UpdateView(.016f,0);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
    public static object Check()
    {
        if(!Application.isPlaying)throw new System.InvalidOperationException("Play mode required.");
        var p=Object.FindAnyObjectByType<SurvivalPlayerController>();var rig=p.cameraRig;
        var start=p.transform.position;var rotation=p.transform.rotation;bool fps=p.firstPerson;p.SetFirstPerson(false);
        var results=new System.Collections.Generic.Dictionary<string,bool>();
        void Shot(string key,Vector3 point,string expected)
        {
            p.Teleport(point);rig.UpdateView(.016f,0);
            results[key]=(rig.ActiveFixedCamera?rig.ActiveFixedCamera.name.Trim():"drone")==expected;
        }
        Shot("shop approach",new Vector3(-17.5f,.05f,-10),"shop_camera");
        Shot("shop inside",new Vector3(-17.5f,.05f,-14),"shop_inside_camera");
        Shot("shop exit",new Vector3(-17.5f,.05f,-11),"shop_camera");
        Shot("bakery approach",new Vector3(12.5f,.05f,-10),"bakery_camera");
        Shot("bakery inside",new Vector3(12.5f,.05f,-15),"bakery_inside_camera");
        Shot("bakery exit",new Vector3(12.5f,.05f,-11),"bakery_camera");
        Shot("open street",new Vector3(0,.05f,6),"drone");
        Shot("entrance unchanged",new Vector3(0,.05f,13),"entrance_camera");
        p.Teleport(new Vector3(0,.05f,6));p.transform.rotation=Quaternion.identity;
        var before=p.transform.position;p.Move(Vector2.right,Vector2.zero,false,false,.1f);
        var delta=p.transform.position-before;delta.y=0;
        results["outdoor D turns without strafing"]=p.transform.eulerAngles.y>10 && delta.magnitude<.02f;
        p.Teleport(new Vector3(0,.05f,6));p.transform.rotation=Quaternion.identity;
        for(int i=0;i<20;i++)p.Move(Vector2.up,Vector2.zero,false,false,1f/60);
        results["outdoor W walks forward"]=p.transform.position.z>6.5f && Mathf.Abs(p.transform.position.x)<.01f;
        p.Teleport(new Vector3(0,.05f,6));p.transform.rotation=Quaternion.identity;
        for(int i=0;i<20;i++)p.Move(Vector2.down,Vector2.zero,false,false,1f/60);
        results["outdoor S walks backward"]=p.transform.position.z<5.5f && p.animator.GetBool("isWalkingBackward");
        p.Teleport(start);p.transform.rotation=rotation;p.SetFirstPerson(fps);rig.UpdateView(.016f,0);return results;
    }
}
