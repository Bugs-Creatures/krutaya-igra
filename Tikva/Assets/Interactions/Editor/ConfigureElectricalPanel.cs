using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigureElectricalPanel
{
    static BoxCollider FitCollider(Transform target,MeshFilter mesh)
    {
        var box=target.GetComponent<BoxCollider>();if(!box)box=target.gameObject.AddComponent<BoxCollider>();
        var bounds=mesh.sharedMesh.bounds;var local=new Bounds();bool first=true;
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
        {
            var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
            var point=target.InverseTransformPoint(mesh.transform.TransformPoint(corner));
            if(first){local=new Bounds(point,Vector3.zero);first=false;}else local.Encapsulate(point);
        }
        box.center=local.center;box.size=local.size;box.isTrigger=false;box.enabled=true;
        EditorUtility.SetDirty(box);return box;
    }
    static InteractionObject Target(Transform root,string label,string text)
    {
        var item=root.GetComponent<InteractionObject>();if(!item)item=root.gameObject.AddComponent<InteractionObject>();
        item.kind=InteractionKind.ToolAction;item.displayName=label;item.suspicious=false;
        item.reachDistance=3;item.hoverTextSize=14;item.availableHoverText=text;
        var outline=root.GetComponent<InteractionOutline>();if(!outline)outline=root.gameObject.AddComponent<InteractionOutline>();
        outline.outlineShader=Shader.Find("Interaction/Outline");outline.width=3;
        EditorUtility.SetDirty(item);EditorUtility.SetDirty(outline);return item;
    }
    static Material WireMaterial(string name,Color color)
    {
        const string folder="Assets/Interactions/Materials";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Interactions","Materials");
        var path=folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.25f);material.SetFloat("_Metallic",0);
        EditorUtility.SetDirty(material);return material;
    }
    [MenuItem("Tools/Interactions/Configure electrical panel")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
        var root=GameObject.Find("Держатель щитка").transform;
        var cover=root.Find("Крышка");var cabinet=root.Find("Щиток");
        var panel=root.GetComponent<ElectricalPanel>();if(!panel)panel=root.gameObject.AddComponent<ElectricalPanel>();
        panel.coverTarget=Target(cover,"Крышка щитка","открутить");
        panel.panelTarget=Target(cabinet,"Щиток","перерезать провод");
        panel.screws=cover.Cast<Transform>().Where(t=>t.name.StartsWith("Circle")).Select(t=>t.gameObject).ToArray();
        panel.intactWire=cabinet.Find("Не перезанные").gameObject;
        panel.cutWire=cabinet.Find("Перерезанные").gameObject;
        panel.coverFallDirection=Vector3.left;
        FitCollider(cover,cover.Find("Cube.001").GetComponent<MeshFilter>());
        FitCollider(cabinet,cabinet.Find("Cube").GetComponent<MeshFilter>());
        foreach(var t in cover.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        panel.panelTarget.enabled=false;panel.intactWire.SetActive(true);panel.cutWire.SetActive(false);
        var tools=Object.FindObjectsByType<InteractionObject>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        void Action(InteractionObject target,ToolActionKind kind,string toolName,string failure)
        {
            var action=target.GetComponent<ToolActionTarget>();if(!action)action=target.gameObject.AddComponent<ToolActionTarget>();
            action.action=kind;action.electricalPanel=panel;action.requiredTool=tools.First(t=>t.ItemName==toolName);
            action.missingToolText=failure;EditorUtility.SetDirty(action);
        }
        Action(panel.coverTarget,ToolActionKind.RemovePanelCover,"Отвёртка","нужно чем-то открутить");
        Action(panel.panelTarget,ToolActionKind.CutPanelWire,"Плоскогубцы","чем-то нужно перерезать");
        cabinet.Find("Синий").GetComponent<Renderer>().sharedMaterial=WireMaterial("Wire_Blue",new Color(.04f,.2f,.75f));
        cabinet.Find("Зеленый").GetComponent<Renderer>().sharedMaterial=WireMaterial("Wire_Green",new Color(.045f,.48f,.12f));
        EditorUtility.SetDirty(panel);EditorUtility.SetDirty(panel.panelTarget);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        EditorSceneManager.SaveScene(root.gameObject.scene);
    }

    [MenuItem("Tools/Interactions/Refit bakery camera zone")]
    public static void RefitBakeryZone()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
        var camera=GameObject.Find("bakery_camera").GetComponent<Camera>();var zone=camera.GetComponent<FixedCameraZone>();
        var main=zone.volume;
        float aspectScale=camera.aspect/Mathf.Min(camera.aspect,16f/9);
        var boxes=new System.Collections.Generic.List<BoxCollider>();
        {
            // Five shallow volumes follow the trapezoid visible from the relocated camera.
            // Check both feet and head with a screen margin, so cuts keep the whole actor in frame.
            for(int i=0;i<5;i++)
            {
                float nearZ=-7-i,farZ=nearZ-1,minX=float.PositiveInfinity,maxX=float.NegativeInfinity;
                for(float x=7;x<=25;x+=.05f)
                {
                    bool visible=true;
                    foreach(float z in new[]{nearZ,farZ})foreach(float y in new[]{.1f,1.75f})
                    {
                        var p=camera.WorldToViewportPoint(new Vector3(x,y,z));
                        p.x=(p.x-.5f)*aspectScale+.5f;
                        if(p.z<=0||p.x<.055f||p.x>.945f||p.y<.055f||p.y>.945f)visible=false;
                    }
                    if(visible){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);}
                }
                if(maxX<=minX)continue;
                BoxCollider box;
                if(i==0)box=main;
                else
                {
                    string name="Bakery coverage strip "+(i+1);var child=main.transform.Find(name);
                    if(!child){child=new GameObject(name).transform;child.SetParent(main.transform);}
                    box=child.GetComponent<BoxCollider>();if(!box)box=child.gameObject.AddComponent<BoxCollider>();
                }
                box.transform.SetPositionAndRotation(new Vector3((minX+maxX)/2,1.5f,(nearZ+farZ)/2),Quaternion.identity);
                box.transform.localScale=Vector3.one;box.center=Vector3.zero;box.size=new Vector3(maxX-minX,4,1);
                box.isTrigger=true;box.gameObject.layer=2;boxes.Add(box);
                EditorUtility.SetDirty(box);EditorUtility.SetDirty(box.transform);
            }
        }
        if(boxes.Count==0)throw new System.InvalidOperationException("Camera does not cover the bakery approach.");
        zone.volume=boxes[0];zone.additionalVolumes=boxes.Skip(1).ToArray();
        EditorUtility.SetDirty(zone);Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(zone.gameObject.scene);EditorSceneManager.SaveScene(zone.gameObject.scene);
    }
}
