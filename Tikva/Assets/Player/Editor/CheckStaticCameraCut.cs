using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CheckStaticCameraCut
{
    public static object Run()
    {
        if(!Application.isPlaying)throw new System.InvalidOperationException("Play mode required.");
        var p=Object.FindAnyObjectByType<SurvivalPlayerController>();var rig=p.cameraRig;
        Vector3 origin=p.transform.position;Quaternion facing=p.transform.rotation;bool mode=p.firstPerson;
        var checks=new Dictionary<string,bool>();p.SetFirstPerson(false);
        void Check(string label,Vector3 occluded,Vector3 entry,string cameraName)
        {
            rig.occlusionFader.RestoreAll();
            var originals=Object.FindObjectsByType<Renderer>().Where(r=>!r.transform.IsChildOf(p.transform))
                .ToDictionary(r=>r,r=>r.sharedMaterials);
            p.Teleport(occluded);for(int i=0;i<150;i++)rig.UpdateView(1f/60,0);
            checks[label+" starts transparent"]=rig.occlusionFader.FadedRendererCount>0;
            p.Teleport(entry);rig.UpdateView(1f/60,0); // Exactly the first frame of the new shot.
            checks[label+" first frame restored"]=rig.ActiveFixedCamera && rig.ActiveFixedCamera.name.Trim()==cameraName &&
                rig.occlusionFader.FadedRendererCount==0 && originals.All(pair=>pair.Key.sharedMaterials.SequenceEqual(pair.Value));
        }
        Check("shop",new Vector3(-23,.05f,-14),new Vector3(-17.5f,.05f,-10),"shop_camera");
        Check("bakery",new Vector3(3,.05f,-17),new Vector3(12.5f,.05f,-10),"bakery_camera");
        p.Teleport(origin);p.transform.rotation=facing;p.SetFirstPerson(mode);rig.UpdateView(1f/60,0);return checks;
    }
}
