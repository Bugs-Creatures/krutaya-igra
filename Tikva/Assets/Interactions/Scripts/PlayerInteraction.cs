using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1000)]
public sealed class PlayerInteraction : MonoBehaviour
{
    public SurvivalPlayerController player;
    public Transform rightHand;
    public Transform firstPersonAnchor;
    public InteractionObject heldItem;
    public LayerMask aimLayers=~(1<<2);
    [Header("First person held item motion")]
    [Min(0)] public float walkBobAmount=.018f;
    [Min(0)] public float walkBobFrequency=9f;
    [Min(0)] public float turnPositionLag=.00012f;
    [Min(0)] public float turnRotationLag=.025f;
    [Min(.01f)] public float motionSmoothTime=.12f;
    public InteractionObject Hovered {get;private set;}
    public InteractionPrompt prompt;
    Collider[] playerColliders;
    public static readonly Color AvailableOutlineColor=Color.yellow;
    Vector3 configuredGripPosition,configuredGripRotation;
    Vector3 motionPosition,motionPositionVelocity,motionRotation,motionRotationVelocity;
    Quaternion previousCameraRotation;
    InteractionObject motionItem;
    float bobPhase,bobWeight,bobWeightVelocity;
    bool cameraRotationReady,gripConfigured;
    void Awake()
    {
        if(!player)player=GetComponent<SurvivalPlayerController>();
        playerColliders=GetComponentsInChildren<Collider>();
        EnsureFirstPersonAnchor();
    }
    void EnsureFirstPersonAnchor()
    {
        if(firstPersonAnchor||!player||!player.cameraRig||!player.cameraRig.output)return;
        firstPersonAnchor=new GameObject("FirstPersonItemAnchor").transform;
        firstPersonAnchor.SetParent(player.cameraRig.output.transform,false);
        firstPersonAnchor.localPosition=new Vector3(.31f,-.27f,.7f);
    }
    Transform AnchorForMode(bool firstPerson)
    {
        if(!firstPerson)return rightHand;
        EnsureFirstPersonAnchor();
        return firstPersonAnchor;
    }
    public bool InReach(InteractionObject item)
    {
        var collider=item.GetComponentInChildren<Collider>();var origin=transform.position+Vector3.up;
        var point=collider?collider.ClosestPoint(origin):item.transform.position;
        return Vector3.Distance(origin,point)<=item.reachDistance;
    }
    public InteractionObject TargetAt(Ray ray)
    {
        var hits=Physics.RaycastAll(ray,player.cameraRig.output.farClipPlane,aimLayers,QueryTriggerInteraction.Ignore);
        Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(var hit in hits)
        {
            if(hit.transform.IsChildOf(transform))continue;
            var item=hit.collider.GetComponentInParent<InteractionObject>();
            return item && item.isActiveAndEnabled && !item.IsHeld?item:null;
        }
        return null;
    }
    public bool TryInteract(InteractionObject item)
    {
        if(player.modalInputLocked||player.dialogueLocked||!item||!item.isActiveAndEnabled||!item.Allowed(player.EffectiveFirstPerson)||!InReach(item))return false;
        if(item.kind==InteractionKind.ToolAction){var action=item.GetComponent<ToolActionTarget>();return action&&action.TryUse(this);}
        if(item.kind==InteractionKind.Talk){var talk=item.GetComponent<ITalkTarget>();if(talk!=null)talk.Talk();return true;}
        if(item.kind==InteractionKind.Door){item.ToggleDoor();return true;}
        var map=item.GetComponent<MapItem>();
        if(map){SetHovered(null);map.Collect(this);return true;}
        var crafting=GetComponent<RopeHookCrafting>();
        if(crafting&&crafting.TryCombine(this,item)){SetHovered(null);return true;}
        var firstPerson=player.EffectiveFirstPerson;
        var anchor=AnchorForMode(firstPerson);if(!anchor)return false;
        DropHeld();item.Attach(anchor,firstPerson);heldItem=item;player.currentHeldItem=item.ItemName;SetHovered(null);
        item.RecordPlayerPickup();
        return true;
    }
    public bool ClickTarget(InteractionObject item,Vector2 screen)
    {
        if(!item||player.modalInputLocked||player.dialogueLocked||player.ShopReturning)return false;
        var firstPerson=player.EffectiveFirstPerson;var inReach=InReach(item);
        if(item.suspicious&&!firstPerson)GetComponent<PlayerTutorialHud>()?.SuspiciousAttempt(item);
        if(!inReach||!item.Allowed(firstPerson))
        {
            if(prompt)prompt.ShowClickFeedback(item,screen,firstPerson,inReach);
            return false;
        }
        bool succeeded=TryInteract(item);
        if(!succeeded&&item.kind==InteractionKind.ToolAction)
        {
            var action=item.GetComponent<ToolActionTarget>();
            if(action&&!string.IsNullOrEmpty(action.LastFailure)&&prompt)prompt.ShowClickFeedback(item,screen,firstPerson,inReach,action.LastFailure);
        }
        if(succeeded&&prompt)prompt.ClearFeedback();
        return succeeded;
    }
    public void DropHeld(bool forcedByCamera=false)
    {
        ResetHeldMotion();
        if(!heldItem){heldItem=null;player.currentHeldItem="";return;}
        var point=transform.position+Vector3.up;
        var offsets=new[]{transform.right*.65f+transform.forward*.3f,transform.forward*.75f,-transform.right*.65f,-transform.forward*.75f};
        foreach(var offset in offsets)
        {
            bool free=true;
            foreach(var c in Physics.OverlapSphere(point+offset,.32f,~0,QueryTriggerInteraction.Ignore))if(!c.transform.IsChildOf(transform)&&!c.transform.IsChildOf(heldItem.transform)){free=false;break;}
            if(free){point+=offset;break;}
        }
        heldItem.Drop(point,playerColliders);heldItem.RecordPlayerDrop();
        if(heldItem.IsDroppedByPlayer)GetComponent<PlayerTutorialHud>()?.ItemDropped(heldItem);
        if(forcedByCamera&&heldItem.suspicious)GetComponent<PlayerTutorialHud>()?.SuspiciousDroppedByCamera();
        heldItem=null;player.currentHeldItem="";
    }
    // Hands the held item over to someone else (an NPC takes it): the item stays held, now by them.
    public InteractionObject TakeHeld()
    {
        ResetHeldMotion();var item=heldItem;heldItem=null;player.currentHeldItem="";gripConfigured=false;return item;
    }
    public void EnforceHeldRules()
    {
        if(heldItem && heldItem.suspicious && !player.EffectiveFirstPerson)DropHeld(true);
        if(!heldItem)player.currentHeldItem="";
    }
    public void RefreshHeldGrip()
    {
        if(!heldItem){gripConfigured=false;return;}
        var firstPerson=player.EffectiveFirstPerson;
        var anchor=AnchorForMode(firstPerson);if(!anchor)return;
        var position=firstPerson?heldItem.firstPersonHeldPosition:heldItem.thirdPersonHeldPosition;
        var euler=firstPerson?heldItem.firstPersonHeldRotation:heldItem.thirdPersonHeldRotation;
        bool changed=heldItem.transform.parent!=anchor||!gripConfigured||configuredGripPosition!=position||configuredGripRotation!=euler;
        if(changed)
        {
            heldItem.SetGrip(anchor,firstPerson);
            configuredGripPosition=position;configuredGripRotation=euler;gripConfigured=true;
            ResetHeldMotion();
        }
        else if(!firstPerson)
        {
            if((heldItem.transform.localPosition-position).sqrMagnitude>0.0000001f ||
               Quaternion.Angle(heldItem.transform.localRotation,Quaternion.Euler(euler))>.01f)
                heldItem.SetGrip(anchor,false);
        }
    }
    void ResetHeldMotion()
    {
        motionPosition=motionPositionVelocity=motionRotation=motionRotationVelocity=Vector3.zero;
        bobPhase=bobWeight=bobWeightVelocity=0;
        cameraRotationReady=false;motionItem=null;
    }
    void UpdateFirstPersonHeldMotion(Camera camera)
    {
        if(!heldItem||!player.EffectiveFirstPerson||!camera)return;
        if(motionItem!=heldItem)
        {
            ResetHeldMotion();motionItem=heldItem;
        }
        var currentRotation=camera.transform.rotation;
        if(!cameraRotationReady){previousCameraRotation=currentRotation;cameraRotationReady=true;}
        float dt=Mathf.Max(Time.deltaTime,.0001f);
        Vector3 turn=(Quaternion.Inverse(previousCameraRotation)*currentRotation).eulerAngles;
        turn.x=Mathf.DeltaAngle(0,turn.x);turn.y=Mathf.DeltaAngle(0,turn.y);
        previousCameraRotation=currentRotation;
        float yawSpeed=Mathf.Clamp(turn.y/dt,-360,360);
        float pitchSpeed=Mathf.Clamp(turn.x/dt,-360,360);
        var velocity=GetComponent<CharacterController>().velocity;
        float speed=new Vector2(velocity.x,velocity.z).magnitude;
        float targetWeight=Mathf.Clamp01(speed/Mathf.Max(.1f,player.walkSpeed));
        bobWeight=Mathf.SmoothDamp(bobWeight,targetWeight,ref bobWeightVelocity,motionSmoothTime);
        if(speed>.05f)bobPhase+=dt*walkBobFrequency*Mathf.Clamp(speed/Mathf.Max(.1f,player.walkSpeed),.5f,2f);
        var bob=new Vector3(Mathf.Cos(bobPhase*.5f)*walkBobAmount*.5f,Mathf.Sin(bobPhase)*walkBobAmount,0)*bobWeight;
        var lagPosition=new Vector3(-yawSpeed*turnPositionLag,pitchSpeed*turnPositionLag*.7f,0);
        var targetPosition=bob+lagPosition;
        var targetRotation=new Vector3(-pitchSpeed*turnRotationLag,-yawSpeed*turnRotationLag,-yawSpeed*turnRotationLag*.3f);
        motionPosition=Vector3.SmoothDamp(motionPosition,targetPosition,ref motionPositionVelocity,motionSmoothTime,Mathf.Infinity,dt);
        motionRotation=Vector3.SmoothDamp(motionRotation,targetRotation,ref motionRotationVelocity,motionSmoothTime,Mathf.Infinity,dt);
        heldItem.transform.localPosition=configuredGripPosition+motionPosition;
        heldItem.transform.localRotation=Quaternion.Euler(configuredGripRotation)*Quaternion.Euler(motionRotation);
    }
    void SetHovered(InteractionObject next)
    {
        if(Hovered && Hovered!=next)Hovered.Outline.Show(false,AvailableOutlineColor);
        Hovered=next;
        if(Hovered)Hovered.Outline.Show(Hovered.kind!=InteractionKind.Talk,Hovered.Allowed(player.EffectiveFirstPerson)?AvailableOutlineColor:Color.red);
    }
    void LateUpdate()
    {
        EnforceHeldRules();
        if(player.modalInputLocked||player.dialogueLocked||player.ShopReturning||player.dialogueClickFrame==Time.frameCount){SetHovered(null);return;}
        RefreshHeldGrip();var camera=player.cameraRig.output;UpdateFirstPersonHeldMotion(camera);var mouse=Mouse.current;
        if(!camera||mouse==null)return;
        Vector2 screen=player.EffectiveFirstPerson?new Vector2(Screen.width*.5f,Screen.height*.5f):mouse.position.ReadValue();
        SetHovered(TargetAt(camera.ScreenPointToRay(screen)));
        if(prompt)prompt.Present(Hovered,screen,player.EffectiveFirstPerson);
        if(Hovered && mouse.leftButton.wasPressedThisFrame)
            ClickTarget(Hovered,screen);
    }
    void OnDisable(){ResetHeldMotion();SetHovered(null);if(prompt)prompt.Present(null,Vector2.zero,false);}
}
