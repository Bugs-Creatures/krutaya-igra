using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ConfigureOcclusionFade
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode.");
        var rig=Object.FindAnyObjectByType<SurvivalCameraRig>();
        var fader=rig.GetComponent<CameraOcclusionFader>();if(!fader)fader=rig.gameObject.AddComponent<CameraOcclusionFader>();
        rig.occlusionFader=fader;fader.occludedOpacity=.12f;fader.fadeDuration=.18f;
        var root=GameObject.Find("СЕВЕР • 1994 • BLOCKOUT").transform;
        void Group(string prefix,string envelope)
        {
            var owner=root.Cast<Transform>().FirstOrDefault(t=>t.name.StartsWith(prefix));if(!owner)return;
            var component=owner.GetComponent<CameraOcclusionGroup>();if(!component)component=owner.gameObject.AddComponent<CameraOcclusionGroup>();
            component.envelope=rig.buildingExclusionVolumes.FirstOrDefault(b=>b && b.name==envelope);EditorUtility.SetDirty(component);
        }
        Group("01 •","Home");Group("02 •","Home");Group("06 •","Home");Group("07 •","West housing");Group("08 •","East housing");
        Group("09 •","Bakery housing");Group("04 •","Bakery housing");Group("03 •","Grocery");
        EditorUtility.SetDirty(fader);EditorUtility.SetDirty(rig);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
    public static object Check()
    {
        if(!Application.isPlaying)throw new System.InvalidOperationException("Play mode required.");
        var p=Object.FindAnyObjectByType<SurvivalPlayerController>();var rig=p.cameraRig;var f=rig.occlusionFader;
        Vector3 original=p.transform.position;Quaternion rotation=p.transform.rotation;bool mode=p.firstPerson;
        p.SetFirstPerson(false);
        var roof=GameObject.Find("09 • Дом с булочной").GetComponentsInChildren<Renderer>().First(r=>r.sharedMaterial && r.sharedMaterial.HasProperty("_Surface"));
        var source=roof.sharedMaterial;float alpha=source.color.a;float surface=source.GetFloat("_Surface");
        p.Teleport(new Vector3(3,.05f,-17));
        for(int i=0;i<150;i++)rig.UpdateView(1f/60,0);
        bool cameraMoved=Vector3.Distance(rig.output.transform.position,p.transform.position+Vector3.up+Quaternion.Euler(0,rig.outdoorYaw,0)*rig.droneOffset)>.05f;
        int faded=f.FadedRendererCount;
        var block=new MaterialPropertyBlock();roof.GetPropertyBlock(block,0);
        bool transparent=roof.sharedMaterial!=source && roof.sharedMaterial.GetFloat("_Surface")==1 && block.GetColor("_BaseColor").a<.5f;
        bool sourceUnchanged=source.color.a==alpha && source.GetFloat("_Surface")==surface;
        bool shadowPassPreserved=roof.sharedMaterial.GetShaderPassEnabled("ShadowCaster")==source.GetShaderPassEnabled("ShadowCaster");
        bool collision=roof.GetComponent<Collider>()==null || roof.GetComponent<Collider>().enabled;
        p.Teleport(new Vector3(-30,.05f,6));for(int i=0;i<150;i++)rig.UpdateView(1f/60,0);
        bool restoredWhenClear=roof.sharedMaterial==source;
        p.SetFirstPerson(true);for(int i=0;i<40;i++)rig.UpdateView(1f/60,0);
        bool restored=roof.sharedMaterial==source && f.FadedRendererCount==0;
        p.Teleport(original);p.transform.rotation=rotation;p.SetFirstPerson(mode);rig.UpdateView(1f/60,0);
        return new{cameraKeepsOffset=!cameraMoved,fadedRenderers=faded,transparent,sourceUnchanged,shadowPassPreserved,collidersPreserved=collision,restoredWhenClear,restored};
    }
}
