using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class BuildBasementExit
{
    [MenuItem("Tools/Secret Room/Build basement exit ladder")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
        if(GameObject.Find("Basement exit ladder"))throw new System.InvalidOperationException("Ladder already exists; edit it in the scene.");
        var hatch=GameObject.Find("RustyHatch");
        if(!hatch)throw new System.InvalidOperationException("RustyHatch is missing.");
        var old=GameObject.Find("Basement exit stairs");if(old)Undo.DestroyObjectImmediate(old);
        AssetDatabase.DeleteAsset("Assets/SecretRoom/Basement stairs collision.asset");
        var root=new GameObject("Basement exit ladder");
        Undo.RegisterCreatedObjectUndo(root,"Build basement exit ladder");
        // Decoration only: the player activates the hatch from the floor.
        root.transform.position=new Vector3(1.52f,.8217216f,50.474f);
        var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/SecretRoom/Materials/Aged utility steel.mat");
        var rust=AssetDatabase.LoadAssetAtPath<Material>("Assets/SecretRoom/Materials/Oxidised pipes.mat");
        const float height=3.98f;
        for(int side=-1;side<=1;side+=2)
        {
            Cube(root.transform,"Ladder upright",new Vector3(0,height*.5f,side*.34f),new Vector3(.065f,height,.065f),rust);
            foreach(float y in new[]{.36f,2f,3.65f})
                Cube(root.transform,"Wall bracket",new Vector3(.35f,y,side*.34f),new Vector3(.7f,.045f,.045f),steel);
        }
        for(int i=0;i<12;i++)Cube(root.transform,"Rung "+(i+1),new Vector3(-.015f,.28f+i*.31f,0),new Vector3(.075f,.045f,.68f),steel);
        var destination=GameObject.Find("Basement return point");
        if(!destination){destination=new GameObject("Basement return point");Undo.RegisterCreatedObjectUndo(destination,"Create basement return point");}
        destination.transform.SetPositionAndRotation(new Vector3(1.1f,.08f,13.55f),Quaternion.Euler(0,180,0));
        // Fit in hatch-local space, including its imported scale and rotation.
        bool first=true;Bounds bounds=default;
        foreach(var filter in hatch.GetComponentsInChildren<MeshFilter>())
        {
            var b=filter.sharedMesh.bounds;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {
                var p=hatch.transform.InverseTransformPoint(filter.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));
                if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);
            }
        }
        var collider=hatch.GetComponent<BoxCollider>();if(!collider)collider=Undo.AddComponent<BoxCollider>(hatch);
        collider.center=bounds.center;collider.size=bounds.size;
        var target=hatch.GetComponent<InteractionObject>();if(!target)target=Undo.AddComponent<InteractionObject>(hatch);
        target.kind=InteractionKind.ToolAction;target.displayName="Люк";target.availableHoverText="вернуться в подъезд";target.reachDistance=5;target.hoverTextSize=14;target.suspicious=false;
        var action=hatch.GetComponent<ToolActionTarget>();if(!action)action=Undo.AddComponent<ToolActionTarget>(hatch);
        action.action=ToolActionKind.ExitBasement;action.destination=destination.transform;
        EditorUtility.SetDirty(target);EditorUtility.SetDirty(action);EditorUtility.SetDirty(collider);
        EditorSceneManager.MarkSceneDirty(hatch.scene);
        EditorSceneManager.SaveScene(hatch.scene);AssetDatabase.SaveAssets();
        Selection.activeGameObject=root;
    }

    static void Cube(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);
        o.transform.localPosition=position;o.transform.localScale=scale;
        Object.DestroyImmediate(o.GetComponent<Collider>());o.GetComponent<Renderer>().sharedMaterial=material;
    }
}
