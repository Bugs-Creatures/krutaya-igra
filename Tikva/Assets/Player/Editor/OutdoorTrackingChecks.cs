using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class OutdoorTrackingChecks
{
    public static object Run()
    {
        if(!Application.isPlaying)throw new System.InvalidOperationException("Play mode required.");
        var p=Object.FindAnyObjectByType<SurvivalPlayerController>();var rig=p.cameraRig;
        Vector3 original=p.transform.position;Quaternion rotation=p.transform.rotation;bool manual=p.firstPerson;
        p.SetFirstPerson(false);var results=new Dictionary<string,bool>();
        var first=OutdoorTrackingCamera.Active.First(c=>c.name=="outside_camera_1");
        var second=OutdoorTrackingCamera.Active.First(c=>c.name=="outside_camera_2");
        Vector3 Visible(OutdoorTrackingCamera camera)
        {
            foreach(var candidate in new[]{new Vector3(camera==first?-20:20,.05f,0),new Vector3(camera==first?-10:10,.05f,4)})
                if(camera.Contains(candidate)&&camera.CanSeePoint(candidate)&&!CameraBlindZone.Active.Any(z=>z.Contains(candidate,.5f)))return candidate;
            for(float x=-44;x<45;x+=2)for(float z=-5;z<8;z+=2)
            {
                var candidate=new Vector3(x,.05f,z);
                if(camera.Contains(candidate)&&camera.CanSeePoint(candidate)&&!CameraBlindZone.Active.Any(b=>b.Contains(candidate,.5f))&&!FixedCameraZone.Active.Any(b=>b.Accepts(candidate)))return candidate;
            }
            throw new System.InvalidOperationException("No visible test point for "+camera.name);
        }
        void Advance(int frames){for(int i=0;i<frames;i++)rig.UpdateView(1f/60,0);p.SetFirstPerson(p.firstPerson);}
        var a=Visible(first);var b=Visible(second);
        p.Teleport(a);Advance(150);results["first outdoor camera"]=rig.ActiveOutdoorCamera==first&&!rig.IsFirstPerson;
        Vector3 anchor=first.transform.position;
        p.Teleport(b);Advance(150);results["second outdoor camera"]=rig.ActiveOutdoorCamera==second&&!rig.IsFirstPerson;
        var blind=CameraBlindZone.Active.First(z=>!FixedCameraZone.Active.Any(c=>c.Contains(new Vector3(z.transform.position.x,.05f,z.transform.position.z))));
        var blindPoint=new Vector3(blind.transform.position.x,.05f,blind.transform.position.z);
        p.Teleport(blindPoint);Advance(8);results["short visibility loss ignored"]=!rig.AutomaticFirstPerson;
        Advance(100);results["blind zone automatic FPS"]=rig.AutomaticFirstPerson&&p.EffectiveFirstPerson&&!p.firstPerson;
        results["model hidden in automatic FPS"]=p.modelRenderers.All(r=>r.forceRenderingOff);
        bool stable=true;
        for(int i=0;i<10;i++){p.Teleport(a);Advance(6);stable&=rig.AutomaticFirstPerson;p.Teleport(blindPoint);Advance(6);stable&=rig.AutomaticFirstPerson;}
        results["boundary oscillation does not flicker"]=stable;
        p.Teleport(a);Advance(100);results["stable visibility returns camera"]=!rig.AutomaticFirstPerson&&rig.ActiveOutdoorCamera==first&&p.modelRenderers.All(r=>!r.forceRenderingOff);
        first.Present(rig.output,p.transform,.016f,true);float near=rig.output.fieldOfView;
        p.Teleport(first.transform.position+first.transform.forward*55-Vector3.up);
        for(int i=0;i<150;i++)first.Present(rig.output,p.transform,1f/60,false);
        results["far target zooms in"]=rig.output.fieldOfView<near-2;
        p.Teleport(a);for(int i=0;i<150;i++)first.Present(rig.output,p.transform,1f/60,false);
        results["near target zooms back out"]=Mathf.Abs(rig.output.fieldOfView-near)<1;
        results["camera anchor does not move"]=first.transform.position==anchor;
        p.SetFirstPerson(true);Advance(10);results["manual FPS override works"]=rig.IsFirstPerson;
        p.SetFirstPerson(false);
        void Static(string label,Vector3 point,string name){p.Teleport(point);Advance(2);results[label]=rig.ActiveFixedCamera&&rig.ActiveFixedCamera.name.Trim()==name&&!rig.AutomaticFirstPerson;}
        Static("room zone preserved",new Vector3(5.3f,3.05f,15),"room_camera");
        Static("entrance zone preserved",new Vector3(0,.05f,13),"entrance_camera");
        Static("shop zone preserved",new Vector3(-17.5f,.05f,-10),"shop_camera");
        Static("bakery zone preserved",new Vector3(12.5f,.05f,-15),"bakery_inside_camera");
        results["occlusion transparency disabled"]=!rig.occlusionFader||!rig.occlusionFader.enabled;
        p.Teleport(original);p.transform.rotation=rotation;p.SetFirstPerson(manual);Advance(100);return results;
    }
}
