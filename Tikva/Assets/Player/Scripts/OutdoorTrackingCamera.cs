using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class OutdoorTrackingCamera : MonoBehaviour
{
    public static readonly HashSet<OutdoorTrackingCamera> Active=new();
    public BoxCollider volume;
    [Min(.01f)] public float rotationDelay=.35f;
    [Min(.01f)] public float zoomDelay=.6f;
    public float normalFieldOfView=60;
    public float farFieldOfView=32;
    public float zoomStartDistance=14;
    public float zoomEndDistance=45;
    public float maximumDistance=80;
    [Range(20,180)] public float maximumPan=130;
    public LayerMask sightBlockers=~0;
    [Tooltip("Check whether colliders block the player. Disable to use camera areas and blind-zone triggers without raycasts.")]
    public bool checkPhysicalVisibility=false;
    public Camera Shot=>GetComponent<Camera>();
    Quaternion trackedRotation;
    float trackedFov;
    public bool Contains(Vector3 p,float margin=0)
    {
        if(!volume)return true;
        return new Bounds(volume.center,volume.size+Vector3.one*margin*2).Contains(volume.transform.InverseTransformPoint(p+Vector3.up));
    }
    public bool CanSee(Transform player)
    {
        if(!player)return false;
        return CanSeePoint(player.position,player);
    }
    public bool CanSeePoint(Vector3 feet,Transform ignore=null)
    {
        Vector3 delta=feet+Vector3.up-transform.position;
        if(delta.magnitude>maximumDistance||delta.magnitude<.5f)return false;
        if(Mathf.Abs(Vector3.SignedAngle(Vector3.ProjectOnPlane(transform.forward,Vector3.up),Vector3.ProjectOnPlane(delta,Vector3.up),Vector3.up))>maximumPan)return false;
        if(!checkPhysicalVisibility)return true;
        // At least one meaningful part of the actor must be visible.
        foreach(float height in new[]{.6f,1.1f,1.65f})
        {
            Vector3 ray=feet+Vector3.up*height-transform.position;bool blocked=false;
            foreach(var hit in Physics.RaycastAll(transform.position,ray.normalized,Mathf.Max(0,ray.magnitude-.3f),sightBlockers,QueryTriggerInteraction.Ignore))
                if(!ignore||!hit.collider.transform.IsChildOf(ignore)){blocked=true;break;}
            if(!blocked)return true;
        }
        return false;
    }
    public void Present(Camera output,Transform player,float dt,bool cut)
    {
        var delta=player.position+Vector3.up-transform.position;
        var desired=Quaternion.LookRotation(delta);
        float fov=Mathf.Lerp(normalFieldOfView,farFieldOfView,Mathf.InverseLerp(zoomStartDistance,zoomEndDistance,delta.magnitude));
        if(cut){trackedRotation=desired;trackedFov=fov;}
        else
        {
            trackedRotation=Quaternion.Slerp(trackedRotation,desired,1-Mathf.Exp(-dt/Mathf.Max(.01f,rotationDelay)));
            trackedFov=Mathf.Lerp(trackedFov,fov,1-Mathf.Exp(-dt/Mathf.Max(.01f,zoomDelay)));
        }
        output.transform.SetPositionAndRotation(transform.position,trackedRotation);
        output.orthographic=false;output.fieldOfView=trackedFov;output.nearClipPlane=.06f;
    }
    void OnEnable(){Active.Add(this);Shot.enabled=false;}
    void OnDisable()=>Active.Remove(this);
}
