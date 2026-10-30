using System.Collections.Generic;
using UnityEngine;

public static class SurvivalControlsCheck
{
    public static object Run()
    {
        if(!Application.isPlaying)throw new System.InvalidOperationException("Play mode required.");
        var p=Object.FindAnyObjectByType<SurvivalPlayerController>();var rig=p.cameraRig;
        var original=p.transform.position;var rotation=p.transform.rotation;bool mode=p.firstPerson;
        var results=new Dictionary<string,bool>();
        void Check(string label,Vector3 point,string expected)
        {
            p.Teleport(point);rig.RefreshSelection();rig.UpdateView(.016f,0);
            results[label]=(rig.ActiveFixedCamera?rig.ActiveFixedCamera.name:"drone")==expected;
        }
        p.SetFirstPerson(false);
        Check("room",new Vector3(5.49f,3.05f,14.79f),"room_camera");
        Check("hallway",new Vector3(3.75f,3.05f,16),"hallway_camera");
        Check("rear hallway",new Vector3(9.3f,3.05f,17.6f),"hallway_camera");
        Check("entrance",new Vector3(1.35f,3.05f,13.3f),"entrance_camera");
        Check("street",new Vector3(0,.08f,6),"drone");
        Vector3 cameraBefore=rig.output.transform.position;
        p.Teleport(p.transform.position+Vector3.right*2);rig.UpdateView(.016f,0);
        float cameraTravel=Vector3.Distance(cameraBefore,rig.output.transform.position);
        results["drone delay"]=cameraTravel>0 && cameraTravel<1.9f;
        p.transform.rotation=Quaternion.identity;
        var before=p.transform.position;
        for(int i=0;i<12;i++)p.Move(Vector2.up,Vector2.zero,false,false,1f/60);
        results["movement and walking"]=Vector3.Distance(before,p.transform.position)>.1f && p.animator.GetBool("isWalking");
        p.Move(Vector2.zero,Vector2.zero,false,false,1f/60);results["idle"]=!p.animator.GetBool("isWalking");
        p.Teleport(new Vector3(5.3f,3.05f,15));p.transform.rotation=Quaternion.identity;
        p.Move(Vector2.right,Vector2.zero,false,false,.1f);results["tank rotation"]=p.transform.eulerAngles.y>10;
        p.Teleport(new Vector3(0,.05f,6));p.SetFirstPerson(true);
        bool hidden=true;foreach(var r in p.modelRenderers)hidden&=r.forceRenderingOff;results["first person hidden"]=hidden;
        for(int i=0;i<10;i++)p.Move(Vector2.zero,Vector2.zero,false,false,1f/60);
        float start=p.transform.position.y;
        p.Move(Vector2.zero,new Vector2(100,0),true,false,1f/60);
        for(int i=0;i<10;i++)p.Move(Vector2.zero,Vector2.zero,false,false,1f/60);
        rig.UpdateView(.016f,0);results["jump"]=p.transform.position.y>start+.2f;
        results["first person camera"]=!rig.output.orthographic && Vector3.Distance(rig.output.transform.position,p.transform.position+Vector3.up*rig.eyeHeight)<.01f;
        p.SetFirstPerson(false);bool visible=true;foreach(var r in p.modelRenderers)visible&=!r.forceRenderingOff;results["model restored"]=visible;
        p.Teleport(original);p.transform.rotation=rotation;p.SetFirstPerson(mode);rig.UpdateView(.016f,0);
        return results;
    }
}
