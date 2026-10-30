using System.Collections.Generic;
using UnityEngine;

public static class LocomotionExitCheck
{
    public static object Run()
    {
        if(!Application.isPlaying)throw new System.InvalidOperationException("Play mode required.");
        var p=Object.FindAnyObjectByType<SurvivalPlayerController>();var rig=p.cameraRig;
        var origin=p.transform.position;var facing=p.transform.rotation;bool mode=p.firstPerson;
        var result=new Dictionary<string,bool>();p.SetFirstPerson(false);
        void Step(Vector2 axes,bool run,int count)
        {
            for(int i=0;i<count;i++){p.Move(axes,Vector2.zero,false,run,1f/60);p.animator.Update(1f/60);}
        }
        p.Teleport(new Vector3(.4f,.05f,13.1f));p.transform.rotation=Quaternion.Euler(0,180,0);
        Step(Vector2.up,false,100);
        result["exit straight while holding W"]=p.transform.position.z<10 && Mathf.Abs(p.transform.position.x-.4f)<.1f;
        p.Teleport(new Vector3(0,.05f,9));rig.UpdateView(.016f,0);
        p.transform.rotation=Quaternion.identity;
        Step(Vector2.up,false,110);
        result["enter straight without camera-induced turn"]=p.transform.position.z>12.5f && Mathf.Abs(p.transform.position.x)<.1f;
        p.Teleport(new Vector3(0,.05f,14));p.transform.rotation=Quaternion.identity;
        Step(Vector2.down,true,16);
        result["backward parameters"]=p.animator.GetBool("isWalkingBackward")&&!p.animator.GetBool("isRunning");
        result["backward animation"]=p.animator.GetCurrentAnimatorStateInfo(0).IsName("Walk Backward");
        p.Move(Vector2.zero,Vector2.zero,false,false,1f/60);p.Teleport(new Vector3(0,.05f,14));
        Step(Vector2.up,true,16);result["run animation"]=p.animator.GetBool("isRunning")&&p.animator.GetCurrentAnimatorStateInfo(0).IsName("Run");
        p.Move(Vector2.zero,Vector2.zero,false,false,1f/60);for(int i=0;i<16;i++)p.animator.Update(1f/60);result["idle animation"]=p.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle");
        p.Teleport(new Vector3(0,.05f,9));rig.UpdateView(.016f,0);
        result["perspective outdoor camera"]=!rig.output.orthographic;
        // Rotate towards the house: the camera must stop outside solid geometry.
        for(int i=0;i<100;i++)rig.UpdateView(.016f,0);
        result["camera outside geometry"]=!Physics.CheckSphere(rig.output.transform.position,.08f,~0,QueryTriggerInteraction.Ignore);
        p.Teleport(origin);p.transform.rotation=facing;p.SetFirstPerson(mode);rig.UpdateView(.016f,0);return result;
    }
}

