using UnityEngine;
using UnityEngine.Serialization;

public enum InteractionKind { Pickup, Door, Talk }

public sealed class InteractionObject : MonoBehaviour
{
    public InteractionKind kind;
    public string displayName;
    public bool suspicious;
    [Min(.1f)] public float reachDistance=3;
    [Header("Hover prompt")]
    public string availableHoverText;
    public string suspiciousHoverText="подозрительное действие";
    public string openedDoorHoverText="закрыть";
    public string tooFarHoverText="вы далеко";
    [Min(8)] public float hoverTextSize=14;
    [Header("Held in camera modes - right hand")]
    [FormerlySerializedAs("heldLocalPosition")]
    public Vector3 thirdPersonHeldPosition=new Vector3(.04f,0,.02f);
    [FormerlySerializedAs("heldLocalEuler")]
    public Vector3 thirdPersonHeldRotation=new Vector3(0,0,90);
    [Header("Held in first person - camera anchor")]
    public Vector3 firstPersonHeldPosition;
    public Vector3 firstPersonHeldRotation;
    [Header("Door")]
    public Transform hinge;
    public Vector3 hingeAxis=Vector3.up;
    public float openAngle=90;
    public float openingDuration=.45f;
    [Header("Sounds (optional)")]
    public AudioClip dropSound;
    public AudioClip openSound;
    public AudioClip closeSound;
    [Range(0,1)] public float soundVolume=.8f;
    public bool IsHeld {get;private set;}
    public bool IsOpen {get;private set;}
    public bool HasBeenPickedUpByPlayer {get;private set;}
    public bool IsDroppedByPlayer {get;private set;}
    public void RecordPlayerPickup(){HasBeenPickedUpByPlayer=true;IsDroppedByPlayer=false;}
    public void RecordPlayerDrop(){if(HasBeenPickedUpByPlayer)IsDroppedByPlayer=true;}
    public string ItemName=>string.IsNullOrWhiteSpace(displayName)?name:displayName;
    public string Prompt(bool firstPerson,bool inReach=true)
    {
        if(!inReach)return string.IsNullOrWhiteSpace(tooFarHoverText)?"вы далеко":tooFarHoverText;
        if(suspicious&&!firstPerson)return string.IsNullOrWhiteSpace(suspiciousHoverText)?"подозрительное действие":suspiciousHoverText;
        if(kind==InteractionKind.Door&&IsOpen)return string.IsNullOrWhiteSpace(openedDoorHoverText)?"закрыть":openedDoorHoverText;
        return string.IsNullOrWhiteSpace(availableHoverText)?kind==InteractionKind.Pickup?"подобрать":"открыть":availableHoverText;
    }
    public bool Allowed(bool firstPerson)=>!IsHeld && (!suspicious||firstPerson);
    public InteractionOutline Outline {get;private set;}
    Rigidbody body;
    Collider[] colliders;
    bool[] colliderStates;
    Quaternion closedRotation;
    float openAmount;
    Vector3 worldScale;
    RigidbodyInterpolation originalInterpolation;
    bool originalDetectCollisions;
    bool dropSoundArmed;
    AudioSource soundSource;
    void Awake()
    {
        Outline=GetComponent<InteractionOutline>();if(!Outline)Outline=gameObject.AddComponent<InteractionOutline>();
        colliders=GetComponentsInChildren<Collider>();colliderStates=new bool[colliders.Length];
        for(int i=0;i<colliders.Length;i++)colliderStates[i]=colliders[i].enabled;
        body=GetComponent<Rigidbody>();worldScale=transform.lossyScale;
        if(body){originalInterpolation=body.interpolation;originalDetectCollisions=body.detectCollisions;}
        if(kind==InteractionKind.Door){if(!hinge)hinge=transform;closedRotation=hinge.localRotation;}
    }
    void Update()
    {
        if(kind!=InteractionKind.Door||!hinge)return;
        openAmount=Mathf.MoveTowards(openAmount,IsOpen?1:0,Time.deltaTime/Mathf.Max(.01f,openingDuration));
        float t=Mathf.SmoothStep(0,1,openAmount);
        hinge.localRotation=closedRotation*Quaternion.AngleAxis(openAngle*t,hingeAxis.normalized);
    }
    public void ToggleDoor()
    {
        if(kind!=InteractionKind.Door)return;
        IsOpen=!IsOpen;PlaySound(IsOpen?openSound:closeSound);
    }
    void PlaySound(AudioClip clip)
    {
        if(!clip)return;
        if(!soundSource)
        {
            soundSource=gameObject.AddComponent<AudioSource>();soundSource.playOnAwake=false;
            soundSource.spatialBlend=.6f;soundSource.rolloffMode=AudioRolloffMode.Linear;soundSource.minDistance=1;soundSource.maxDistance=35;
        }
        soundSource.PlayOneShot(clip,soundVolume);
    }
    void OnCollisionEnter(Collision collision)
    {
        if(!dropSoundArmed||IsHeld||kind!=InteractionKind.Pickup||collision.relativeVelocity.sqrMagnitude<.04f)return;
        dropSoundArmed=false;PlaySound(dropSound);
    }
    public void Attach(Transform anchor,bool firstPerson=false)
    {
        if(IsHeld||kind!=InteractionKind.Pickup||!anchor)return;
        dropSoundArmed=false;
        if(Outline)Outline.Show(false,PlayerInteraction.AvailableOutlineColor);
        if(body)
        {
            if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
            originalInterpolation=body.interpolation;originalDetectCollisions=body.detectCollisions;
            body.interpolation=RigidbodyInterpolation.None;
            body.isKinematic=true;body.useGravity=false;body.detectCollisions=false;
        }
        for(int i=0;i<colliders.Length;i++)colliders[i].enabled=false;
        IsHeld=true;
        SetGrip(anchor,firstPerson);
    }
    public void SetGrip(Transform anchor,bool firstPerson)
    {
        if(!IsHeld||!anchor)return;
        transform.SetParent(anchor,true);
        transform.localPosition=firstPerson?firstPersonHeldPosition:thirdPersonHeldPosition;
        transform.localRotation=Quaternion.Euler(firstPerson?firstPersonHeldRotation:thirdPersonHeldRotation);
        var scale=anchor.lossyScale;
        transform.localScale=new Vector3(worldScale.x/scale.x,worldScale.y/scale.y,worldScale.z/scale.z);
        if(body){body.position=transform.position;body.rotation=transform.rotation;}
    }
    public void Drop(Vector3 point,Collider[] playerColliders)
    {
        if(!IsHeld)return;
        transform.SetParent(null,true);transform.position=point;transform.localScale=worldScale;
        for(int i=0;i<colliders.Length;i++)
        {
            colliders[i].enabled=colliderStates[i];
            foreach(var playerCollider in playerColliders)Physics.IgnoreCollision(colliders[i],playerCollider,true);
        }
        if(!body)
        {
            body=gameObject.AddComponent<Rigidbody>();
            originalInterpolation=body.interpolation;
            originalDetectCollisions=body.detectCollisions;
        }
        body.isKinematic=false;body.useGravity=true;body.detectCollisions=originalDetectCollisions;
        body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        // Write the physics pose too: a pick-and-drop in one frame must not resume
        // from the rigidbody's old floor position or its previous hand position.
        body.interpolation=RigidbodyInterpolation.None;
        body.position=point;body.rotation=transform.rotation;body.interpolation=originalInterpolation;
        body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;IsHeld=false;dropSoundArmed=true;
    }
}
