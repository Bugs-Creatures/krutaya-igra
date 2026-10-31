using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallSurvivalControls
{
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode.");
        var player=GameObject.Find("Player");if(!player)throw new System.InvalidOperationException("Player not found.");
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        var old=Object.FindAnyObjectByType<NorthBlockout.BlockoutWalker>();
        var output=old?old.view:Camera.main;
        if(old){old.fixedMode=false;old.RefreshView();Undo.DestroyObjectImmediate(old.gameObject);}
        var cc=player.GetComponent<CharacterController>();if(!cc)cc=Undo.AddComponent<CharacterController>(player);
        cc.height=1.78f;cc.center=new Vector3(0,.9f,0);cc.radius=.26f;cc.stepOffset=.24f;cc.slopeLimit=48;cc.skinWidth=.025f;
        var motor=player.GetComponent<SurvivalPlayerController>();if(!motor)motor=Undo.AddComponent<SurvivalPlayerController>(player);
        var rig=output.GetComponent<SurvivalCameraRig>();if(!rig)rig=Undo.AddComponent<SurvivalCameraRig>(output.gameObject);
        rig.target=player.transform;rig.output=output;
        motor.animator=player.GetComponentInChildren<Animator>();motor.animator.applyRootMotion=false;
        motor.cameraRig=rig;motor.modelRenderers=player.GetComponentsInChildren<Renderer>();
        var room=GameObject.Find("room_camera").GetComponent<Camera>();
        var hall=GameObject.Find("hallway_camera").GetComponent<Camera>();
        var entrance=GameObject.Find("entrance_camera").GetComponent<Camera>();
        var root=new GameObject("Camera activation areas").transform;
        void Shot(Camera cam,Vector3 center,Vector3 size,int priority)
        {
            var z=cam.GetComponent<FixedCameraZone>();if(!z)z=Undo.AddComponent<FixedCameraZone>(cam.gameObject);
            cam.enabled=false;z.priority=priority;z.GetComponent<BoxCollider>().enabled=false;
            var obj=new GameObject(cam.name+" zone");obj.layer=2;obj.transform.SetParent(root);obj.transform.position=center;
            z.volume=obj.AddComponent<BoxCollider>();z.volume.size=size;z.volume.isTrigger=true;
            EditorUtility.SetDirty(cam);EditorUtility.SetDirty(z);
        }
        // Room camera is the apartment fallback until cameras for other rooms are authored.
        Shot(room,new Vector3(9.7f,4.4f,14.5f),new Vector3(10.8f,3,5.05f),10);
        Shot(hall,new Vector3(3.75f,4.4f,16),new Vector3(1.5f,3,8),30);
        Shot(entrance,new Vector3(0,7.5f,18),new Vector3(6,17,12),20);
        // Rear cross-corridor also uses hallway_camera.
        var rear=new GameObject("Rear corridor volume");rear.layer=2;rear.transform.SetParent(root);rear.transform.position=new Vector3(9.7f,4.4f,17.6f);
        var rearVolume=rear.AddComponent<BoxCollider>();rearVolume.size=new Vector3(10.8f,3,1.2f);rearVolume.isTrigger=true;
        hall.GetComponent<FixedCameraZone>().additionalVolumes=new[]{rearVolume};EditorUtility.SetDirty(hall.GetComponent<FixedCameraZone>());
        IndoorCameraArea Interior(string name,Vector3 center,Vector3 size,Camera fallback)
        {
            var g=new GameObject(name);g.layer=2;g.transform.SetParent(root);g.transform.position=center;
            var area=g.AddComponent<IndoorCameraArea>();area.GetComponent<BoxCollider>().size=size;area.fallbackCamera=fallback;return area;
        }
        rig.interiors=new[]{Interior("Home interior",new Vector3(0,7.5f,18),new Vector3(30,17,12),room),
            Interior("Grocery interior",new Vector3(-14,1.5f,-17),new Vector3(14,4,10),null),
            Interior("Bakery interior",new Vector3(16,1.5f,-18),new Vector3(16,4,12),null)};
        foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))c.enabled=c==output;
        foreach(var a in Object.FindObjectsByType<AudioListener>())if(a.gameObject!=output.gameObject)Undo.DestroyObjectImmediate(a);
        if(!output.GetComponent<AudioListener>())output.gameObject.AddComponent<AudioListener>();
        rig.RefreshSelection();rig.UpdateView(.016f,0);
        EditorUtility.SetDirty(motor);EditorUtility.SetDirty(rig);EditorUtility.SetDirty(player);EditorUtility.SetDirty(motor.animator);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());Selection.activeGameObject=player;
    }
}
