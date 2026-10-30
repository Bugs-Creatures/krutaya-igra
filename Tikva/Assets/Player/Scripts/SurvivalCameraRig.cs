using UnityEngine;

public sealed class SurvivalCameraRig : MonoBehaviour
{
    public Transform target;
    public Camera output;
    public IndoorCameraArea[] interiors;
    public float eyeHeight=1.64f;
    [Header("Drone / outdoors")]
    public Vector3 droneOffset=new Vector3(7,9,-7);
    [Min(.01f)] public float followDelay=.25f;
    [Min(.01f)] public float rotationDelay=.2f;
    [Tooltip("Fixed outdoor angle. There are no keyboard or mouse orbit controls.")]
    public float outdoorYaw;
    public bool orthographicDrone=false;
    public float orthographicSize=6;
    public float droneFieldOfView=48;
    [HideInInspector] public bool avoidObstacles=true;
    [HideInInspector] public float collisionRadius=.25f;
    [Tooltip("Solid building envelopes. Small props do not affect camera distance.")]
    public BoxCollider[] buildingExclusionVolumes;
    [HideInInspector] public float buildingClearance=1;
    public float fixedFieldOfViewOverride;
    public CameraOcclusionFader occlusionFader;
    [Header("First person")]
    public float firstPersonFieldOfView=68;
    public bool IsFirstPerson {get;private set;}
    public bool IsIndoors {get;private set;}
    public Camera ActiveFixedCamera {get;private set;}
    Vector3 velocity;
    Vector3 dronePosition;
    string mode;
    public void RefreshSelection()
    {
        if(!target)return;
        Camera fallback=null;IsIndoors=false;
        foreach(var area in interiors)if(area && area.Contains(target.position)){IsIndoors=true;fallback=area.fallbackCamera;break;}
        FixedCameraZone best=null;
        foreach(var z in FixedCameraZone.Active)
        {
            if(!z||!z.isActiveAndEnabled||!z.Accepts(target.position))continue;
            if(!best||z.priority>best.priority||z.priority==best.priority && z.Shot==ActiveFixedCamera)best=z;
        }
        if(best){IsIndoors=true;ActiveFixedCamera=best.Shot;}
        else ActiveFixedCamera=IsIndoors?fallback:null;
    }
    public void SetFirstPerson(bool value)=>IsFirstPerson=value;
    public void UpdateView(float dt,float pitch)
    {
        if(!target||!output)return;
        if(!occlusionFader)occlusionFader=GetComponent<CameraOcclusionFader>();
        RefreshSelection();
        string next=IsFirstPerson?"fps":ActiveFixedCamera?"fixed":"drone";
        bool cut=next!=mode;mode=next;
        // Restore before moving to a different view, so its first rendered frame is opaque.
        // Gradual recovery is only appropriate while staying in the outdoor follow view.
        if(cut && next!="drone" && occlusionFader)occlusionFader.RestoreAll();
        output.nearClipPlane=.06f;
        if(IsFirstPerson)
        {
            output.orthographic=false;output.fieldOfView=firstPersonFieldOfView;
            output.transform.SetPositionAndRotation(target.position+Vector3.up*eyeHeight,Quaternion.Euler(pitch,target.eulerAngles.y,0));if(occlusionFader)occlusionFader.Tick(target,output,false,dt);return;
        }
        if(ActiveFixedCamera)
        {
            var c=ActiveFixedCamera;output.orthographic=c.orthographic;output.orthographicSize=c.orthographicSize;
            output.fieldOfView=fixedFieldOfViewOverride>0?fixedFieldOfViewOverride:c.fieldOfView;
            output.transform.SetPositionAndRotation(c.transform.position,c.transform.rotation);if(occlusionFader)occlusionFader.Tick(target,output,false,dt);return;
        }
        var focus=target.position+Vector3.up;
        var desired=focus+Quaternion.Euler(0,outdoorYaw,0)*droneOffset;
        if(cut){dronePosition=desired;velocity=Vector3.zero;}
        dronePosition=Vector3.SmoothDamp(dronePosition,desired,ref velocity,followDelay,Mathf.Infinity,dt);
        Vector3 candidate=dronePosition;
        // Never shorten the arm towards the actor: canopies, poles and branches must
        // not cause zoom pulses. Occluding geometry fades instead of moving the camera.
        output.transform.position=candidate;
        var rotation=Quaternion.LookRotation(focus-output.transform.position);
        // Retain the fixed angle and delayed tracking independently of scene obstacles.
        output.transform.rotation=cut?rotation:Quaternion.Slerp(output.transform.rotation,rotation,1-Mathf.Exp(-dt/Mathf.Max(.01f,rotationDelay)));
        output.orthographic=orthographicDrone;output.orthographicSize=orthographicSize;output.fieldOfView=droneFieldOfView;
        if(cut)velocity=Vector3.zero;
        if(occlusionFader)occlusionFader.Tick(target,output,true,dt);
    }
    void OnDisable(){if(occlusionFader)occlusionFader.RestoreAll();}
}

