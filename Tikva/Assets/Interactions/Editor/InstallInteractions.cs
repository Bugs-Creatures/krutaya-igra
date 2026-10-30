using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallInteractions
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode.");
        var p=Object.FindAnyObjectByType<SurvivalPlayerController>();
        var interactor=p.GetComponent<PlayerInteraction>();if(!interactor)interactor=p.gameObject.AddComponent<PlayerInteraction>();interactor.player=p;
        var handBone=p.GetComponentsInChildren<Transform>(true).First(t=>t.name=="mixamorig:RightHand");
        var grip=handBone.Find("RightHandGrip");
        if(!grip)
        {
            grip=new GameObject("RightHandGrip").transform;grip.SetParent(handBone,false);
            grip.localPosition=new Vector3(0,.09f,0);
            grip.localRotation=Quaternion.identity;
        }
        interactor.rightHand=grip;
        var firstPersonAnchor=interactor.firstPersonAnchor;
        if(!firstPersonAnchor)
        {
            firstPersonAnchor=p.cameraRig.output.transform.Find("FirstPersonItemAnchor");
            if(!firstPersonAnchor)
            {
                firstPersonAnchor=new GameObject("FirstPersonItemAnchor").transform;
                firstPersonAnchor.SetParent(p.cameraRig.output.transform,false);
                firstPersonAnchor.localPosition=new Vector3(.31f,-.27f,.7f);
            }
            interactor.firstPersonAnchor=firstPersonAnchor;
        }
        InteractionObject ObjectFor(string name,InteractionKind kind,bool suspicious)
        {
            var g=Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include).First(o=>o.name.ToLower()==name.ToLower());
            var item=g.GetComponent<InteractionObject>();bool created=!item;if(!item)item=g.AddComponent<InteractionObject>();
            item.kind=kind;item.suspicious=suspicious;item.displayName=name;if(created)item.reachDistance=3;
            if(string.IsNullOrWhiteSpace(item.availableHoverText))item.availableHoverText=kind==InteractionKind.Door?"открыть":"подобрать";
            if(string.IsNullOrWhiteSpace(item.suspiciousHoverText))item.suspiciousHoverText="подозрительное действие";
            if(string.IsNullOrWhiteSpace(item.openedDoorHoverText))item.openedDoorHoverText="закрыть";
            if(string.IsNullOrWhiteSpace(item.tooFarHoverText))item.tooFarHoverText="вы далеко";
            if(item.hoverTextSize<8)item.hoverTextSize=22;
            if(!g.GetComponent<InteractionOutline>())g.AddComponent<InteractionOutline>();
            var outline=g.GetComponent<InteractionOutline>();outline.outlineShader=Shader.Find("Interaction/Outline");EditorUtility.SetDirty(outline);
            if(g.GetComponentsInChildren<Collider>().Length==0)
            {
                var f=g.GetComponent<MeshFilter>();var box=g.AddComponent<BoxCollider>();box.center=f.sharedMesh.bounds.center;
                var scale=g.transform.lossyScale;box.size=f.sharedMesh.bounds.size+new Vector3(.02f/Mathf.Abs(scale.x),.02f/Mathf.Abs(scale.y),.02f/Mathf.Abs(scale.z));
            }
            if(kind==InteractionKind.Pickup)
            {
                var rb=g.GetComponent<Rigidbody>();if(!rb)rb=g.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;rb.mass=suspicious?.8f:.2f;rb.interpolation=RigidbodyInterpolation.Interpolate;
                EditorUtility.SetDirty(rb);
                if(created)
                {
                    item.thirdPersonHeldPosition=suspicious?new Vector3(-.05f,0,0):new Vector3(0,.06f,0);
                    item.thirdPersonHeldRotation=suspicious?new Vector3(-90,0,0):new Vector3(0,0,180);
                    item.firstPersonHeldRotation=item.thirdPersonHeldRotation;
                }
            }
            EditorUtility.SetDirty(item);return item;
        }
        var door=ObjectFor("EntranceDoor",InteractionKind.Door,false);
        if(!door.hinge)
        {
            var t=door.transform;var mesh=door.GetComponent<MeshFilter>().sharedMesh;
            var pivot=new GameObject("EntranceDoor hinge").transform;pivot.SetParent(t.parent,true);
            var point=mesh.bounds.center;point.x=mesh.bounds.min.x;pivot.SetPositionAndRotation(t.TransformPoint(point),t.rotation);
            t.SetParent(pivot,true);door.hinge=pivot;EditorUtility.SetDirty(door);
        }
        ObjectFor("Crowbar",InteractionKind.Pickup,true);ObjectFor("Screwdriver",InteractionKind.Pickup,false);
        var hud=Object.FindAnyObjectByType<InteractionPrompt>();
        if(!hud){var g=new GameObject("Interaction HUD",typeof(RectTransform),typeof(Canvas));hud=g.AddComponent<InteractionPrompt>();}
        const string cursorPath="Assets/Interactions/Cursors/WindowsLink.png";
        var importer=AssetImporter.GetAtPath(cursorPath) as TextureImporter;
        if(importer && (importer.textureType!=TextureImporterType.Cursor || !importer.isReadable || importer.mipmapEnabled || importer.textureCompression!=TextureImporterCompression.Uncompressed))
        {
            importer.textureType=TextureImporterType.Cursor;
            importer.isReadable=true;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.filterMode=FilterMode.Point;
            importer.alphaIsTransparency=true;
            importer.SaveAndReimport();
        }
        hud.linkCursor=AssetDatabase.LoadAssetAtPath<Texture2D>(cursorPath);
        EditorUtility.SetDirty(hud);
        interactor.prompt=hud;EditorUtility.SetDirty(interactor);EditorUtility.SetDirty(p);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
