using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

public static class FitKeyFirstPerson
{
    [MenuItem("Tools/Interactions/Fit key first person")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
        var key=Object.FindObjectsByType<InteractionObject>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(i=>i.displayName=="Ключ");
        var root=key.transform;var model=root.Find("Model");
        var before=model.GetComponent<Renderer>().bounds.center;
        Undo.RecordObjects(new Object[]{key,root,model},"Correct key pivot and grip");
        PickupPhysics.RecenterFarPivot(key.gameObject);
        // Move the placement rotation to the pickup root, leaving the model in
        // its native orientation for independent first- and third-person grips.
        var children=root.Cast<Transform>().ToArray();var positions=children.Select(t=>t.position).ToArray();var rotations=children.Select(t=>t.rotation).ToArray();
        root.rotation=model.rotation;
        for(int n=0;n<children.Length;n++)children[n].SetPositionAndRotation(positions[n],rotations[n]);
        var points=FitHeldGrips.ModelPoints(key);
        var rotation=Quaternion.LookRotation(new Vector3(-.55f,.75f,.2f),Vector3.back)*Quaternion.Inverse(Quaternion.LookRotation(Vector3.left,Vector3.forward));
        const float scale=1.5f;
        var rotated=points.Select(p=>rotation*p*scale).ToArray();var center=(rotated.Aggregate(Vector3.Min)+rotated.Aggregate(Vector3.Max))*.5f;
        var interaction=Object.FindFirstObjectByType<PlayerInteraction>();
        key.firstPersonHeldScale=scale;key.firstPersonHeldRotation=rotation.eulerAngles;
        key.firstPersonHeldPosition=new Vector3(.24f,-.13f,.37f)-center-interaction.firstPersonAnchor.localPosition;
        var body=key.GetComponent<Rigidbody>();body.position=root.position;body.rotation=root.rotation;
        Physics.SyncTransforms();body.ResetCenterOfMass();body.ResetInertiaTensor();
        foreach(var o in new Object[]{key,root,model,body})EditorUtility.SetDirty(o);
        if(Vector3.Distance(before,model.GetComponent<Renderer>().bounds.center)>.001f)throw new System.InvalidOperationException("Key world placement changed.");
        EditorSceneManager.MarkSceneDirty(key.gameObject.scene);EditorSceneManager.SaveScene(key.gameObject.scene);Selection.activeGameObject=key.gameObject;
    }
}
