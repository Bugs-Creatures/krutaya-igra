using UnityEngine;

public sealed class ElectricalPanel : MonoBehaviour
{
    public InteractionObject coverTarget;
    public InteractionObject panelTarget;
    public GameObject[] screws;
    public GameObject intactWire;
    public GameObject cutWire;
    [Tooltip("Direction away from the wall, in world space.")]
    public Vector3 coverFallDirection=Vector3.left;
    [Min(0)] public float coverPush=.35f;
    public bool CoverRemoved {get;private set;}
    public bool PowerCut {get;private set;}

    void Awake()
    {
        if(coverTarget)coverTarget.enabled=true;
        if(panelTarget)panelTarget.enabled=false;
        if(intactWire)intactWire.SetActive(true);
        if(cutWire)cutWire.SetActive(false);
    }

    public bool TryUse(ToolActionTarget action,PlayerInteraction interaction,out string failure)
    {
        failure=null;
        if(PowerCut||!interaction||!interaction.player)return false;
        bool removing=action.action==ToolActionKind.RemovePanelCover;
        if(removing?CoverRemoved:!CoverRemoved)return false;
        if(!action.requiredTool||interaction.heldItem!=action.requiredTool)
        {failure=action.missingToolText;return false;}
        if(removing)RemoveCover();
        else
        {
            PowerCut=true;
            if(intactWire)intactWire.SetActive(false);
            if(cutWire)cutWire.SetActive(true);
            DisableTarget(panelTarget);DisableTarget(coverTarget);
            interaction.player.DisableCamerasPermanently();
        }
        return true;
    }

    static void DisableTarget(InteractionObject target)
    {
        if(!target)return;
        if(target.Outline)target.Outline.Show(false,PlayerInteraction.AvailableOutlineColor);
        target.enabled=false;
    }

    void RemoveCover()
    {
        CoverRemoved=true;
        if(screws!=null)foreach(var screw in screws)if(screw)screw.SetActive(false);
        DisableTarget(coverTarget);
        if(panelTarget)panelTarget.enabled=true;
        if(!coverTarget)return;
        var cover=coverTarget.transform;
        cover.SetParent(null,true);
        var direction=coverFallDirection.sqrMagnitude>.001f?coverFallDirection.normalized:Vector3.left;
        // Clear the cabinet face before releasing physics so the thin lid cannot get wedged.
        cover.position+=direction*.025f;
        var body=cover.GetComponent<Rigidbody>();
        if(!body)body=cover.gameObject.AddComponent<Rigidbody>();
        body.mass=1.5f;body.isKinematic=false;body.useGravity=true;
        body.interpolation=RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        var coverColliders=cover.GetComponentsInChildren<Collider>();
        if(panelTarget)foreach(var panelCollider in panelTarget.GetComponentsInChildren<Collider>())
            foreach(var coverCollider in coverColliders)Physics.IgnoreCollision(coverCollider,panelCollider);
        body.AddForce(direction*coverPush,ForceMode.VelocityChange);
        body.AddTorque(Vector3.Cross(Vector3.up,direction)*.5f,ForceMode.VelocityChange);
    }
}
