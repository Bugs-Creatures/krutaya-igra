using UnityEngine;

public sealed class SurvivalCameraRig : MonoBehaviour
{
    public Transform target;
    public Camera output;
    public IndoorCameraArea[] interiors;
    public float eyeHeight=1.64f;
    [HideInInspector]
    public Vector3 droneOffset=new Vector3(7,9,-7);
    [HideInInspector] public float followDelay=.25f;
    [HideInInspector] public float rotationDelay=.2f;
    [HideInInspector]
    public float outdoorYaw;
    [HideInInspector] public bool orthographicDrone=false;
    [HideInInspector] public float orthographicSize=6;
    [HideInInspector] public float droneFieldOfView=48;
    [HideInInspector] public bool avoidObstacles=true;
    [HideInInspector] public float collisionRadius=.25f;
    [HideInInspector]
    public BoxCollider[] buildingExclusionVolumes;
    [HideInInspector] public float buildingClearance=1;
    public float fixedFieldOfViewOverride;
    [HideInInspector] public CameraOcclusionFader occlusionFader;
    [Header("First person")]
    public float firstPersonFieldOfView=68;
    [Header("Outdoor tracking and blind zones")]
    public float lostVisibilityDelay=.35f;
    public float visibleRecoveryDelay=.7f;
    public float minimumShotHold=.8f;
    public float boundaryMargin=.6f;
    public float blindZoneMargin=.35f;
    public bool AutomaticFirstPerson {get;private set;}
    public bool IsFirstPerson=>manualFirstPerson||AutomaticFirstPerson;
    public bool IsIndoors {get;private set;}
    public Camera ActiveFixedCamera {get;private set;}
    public OutdoorTrackingCamera ActiveOutdoorCamera {get;private set;}
    bool manualFirstPerson;
    float lostTime,visibleTime,holdTime=10;
    CameraBlindZone blind;
    string viewId;
    Camera previousShot;
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
    public void SetFirstPerson(bool value)=>manualFirstPerson=value;
    void UpdateOutdoor(float dt)
    {
        holdTime+=dt;
        if(ActiveFixedCamera)
        {
            AutomaticFirstPerson=false;ActiveOutdoorCamera=null;blind=null;lostTime=visibleTime=0;return;
        }
        if(blind && !blind.Contains(target.position,blindZoneMargin))blind=null;
        if(!blind)foreach(var zone in CameraBlindZone.Active)if(zone && zone.isActiveAndEnabled && zone.Contains(target.position)){blind=zone;break;}
        OutdoorTrackingCamera candidate=null;
        if(!blind)
        {
            if(ActiveOutdoorCamera && ActiveOutdoorCamera.Contains(target.position,boundaryMargin) && ActiveOutdoorCamera.CanSee(target))candidate=ActiveOutdoorCamera;
            if(!candidate)foreach(var shot in OutdoorTrackingCamera.Active)
                if(shot && shot.isActiveAndEnabled && shot.Contains(target.position) && shot.CanSee(target)){candidate=shot;break;}
        }
        if(candidate)
        {
            lostTime=0;visibleTime+=dt;
            if(AutomaticFirstPerson)
            {
                if(visibleTime>=visibleRecoveryDelay && holdTime>=minimumShotHold)
                {AutomaticFirstPerson=false;ActiveOutdoorCamera=candidate;holdTime=0;}
            }
            else if(!ActiveOutdoorCamera || ActiveOutdoorCamera==candidate || holdTime>=minimumShotHold)
            {
                if(ActiveOutdoorCamera!=candidate){ActiveOutdoorCamera=candidate;holdTime=0;}
            }
        }
        else
        {
            visibleTime=0;lostTime+=dt;
            if(!AutomaticFirstPerson && lostTime>=lostVisibilityDelay && holdTime>=minimumShotHold)
            {AutomaticFirstPerson=true;holdTime=0;}
        }
    }
    public void UpdateView(float dt,float pitch)
    {
        if(!target||!output)return;
        RefreshSelection();UpdateOutdoor(dt);
        // Outdoor geometry stays opaque in the new camera system.
        if(occlusionFader && occlusionFader.FadedRendererCount>0)occlusionFader.RestoreAll();
        string id=IsFirstPerson?"fps":ActiveFixedCamera?"fixed":ActiveOutdoorCamera?"outdoor":"waiting";
        Camera shot=ActiveFixedCamera?ActiveFixedCamera:ActiveOutdoorCamera?ActiveOutdoorCamera.Shot:null;
        bool cut=id!=viewId || previousShot!=shot;viewId=id;previousShot=shot;output.nearClipPlane=.06f;
        if(IsFirstPerson)
        {
            output.orthographic=false;output.fieldOfView=firstPersonFieldOfView;
            output.transform.SetPositionAndRotation(target.position+Vector3.up*eyeHeight,Quaternion.Euler(pitch,target.eulerAngles.y,0));return;
        }
        if(ActiveFixedCamera)
        {
            var c=ActiveFixedCamera;output.orthographic=c.orthographic;output.orthographicSize=c.orthographicSize;
            output.fieldOfView=fixedFieldOfViewOverride>0?fixedFieldOfViewOverride:c.fieldOfView;
            output.transform.SetPositionAndRotation(c.transform.position,c.transform.rotation);return;
        }
        if(ActiveOutdoorCamera)ActiveOutdoorCamera.Present(output,target,dt,cut);
    }
    void OnDisable(){if(occlusionFader)occlusionFader.RestoreAll();}
}


