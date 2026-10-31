using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class SurvivalPlayerController : MonoBehaviour
{
    [Header("Mode")]
    [Tooltip("Enable for WASD + mouse + Space. The model is hidden only for the player camera.")]
    public bool firstPerson;
    [System.NonSerialized] public bool dialogueLocked;
    [System.NonSerialized] public bool modalInputLocked;
    [System.NonSerialized] public int dialogueClickFrame=-1;
    public bool ShopReturning {get;private set;}
    Vector3 shopReturnStart,shopReturnEnd,shopLookTarget;
    Quaternion shopStartRotation;
    float shopStartPitch,shopReturnTime,shopReturnDuration;
    public void BeginShopReturn(Vector3 destination,Vector3 lookTarget,float duration)
    {
        if(ShopReturning)return;
        ShopReturning=true;shopReturnStart=transform.position;shopReturnEnd=destination;
        shopLookTarget=lookTarget;shopReturnTime=0;shopReturnDuration=Mathf.Max(.1f,duration);
        shopStartRotation=transform.rotation;shopStartPitch=pitch;
        inputLatched=false;heldForward=Vector3.zero;
        if(!EffectiveFirstPerson)FaceDialogueTarget(lookTarget);
        if(animator){animator.SetBool(walkingParameter,false);animator.SetBool("isWalkingBackward",false);animator.SetBool("isRunning",false);}
    }
    public void AdvanceShopReturn(float dt)
    {
        if(!ShopReturning)return;
        if(!controller)controller=GetComponent<CharacterController>();
        shopReturnTime+=dt;
        float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(shopReturnTime/shopReturnDuration));
        Vector3 desired=Vector3.Lerp(shopReturnStart,shopReturnEnd,t);
        controller.Move(desired-transform.position);
        if(EffectiveFirstPerson)
        {
            Vector3 direction=shopLookTarget-(transform.position+Vector3.up*1.6f);
            Vector3 flat=Vector3.ProjectOnPlane(direction,Vector3.up);
            if(flat.sqrMagnitude>.001f)transform.rotation=Quaternion.Slerp(shopStartRotation,Quaternion.LookRotation(flat),t);
            pitch=Mathf.LerpAngle(shopStartPitch,-Mathf.Atan2(direction.y,flat.magnitude)*Mathf.Rad2Deg,t);
        }
        if(shopReturnTime>=shopReturnDuration){ShopReturning=false;verticalSpeed=0;inputLatched=false;}
    }
    public void FaceDialogueTarget(Vector3 target)
    {
        Vector3 d=target-(transform.position+Vector3.up*1.6f);
        var flat=Vector3.ProjectOnPlane(d,Vector3.up);
        if(flat.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(flat);
        if(EffectiveFirstPerson)pitch=-Mathf.Atan2(d.y,flat.magnitude)*Mathf.Rad2Deg;
        inputLatched=false;
    }
    [Header("Current right-hand item (empty when free)")]
    public string currentHeldItem="";
    public bool EffectiveFirstPerson => firstPerson || (cameraRig && cameraRig.AutomaticFirstPerson);
    public bool tankControlsIndoors=true;
    public bool tankControlsOutdoors=true;
    [Header("Movement")]
    public float walkSpeed=2.4f;
    public float runMultiplier=1.5f;
    public float backwardSpeedMultiplier=.75f;
    public float tankTurnSpeed=140;
    public float turnSpeed=540;
    public float gravity=22;
    public float jumpHeight=1;
    public float mouseSensitivity=.12f;
    public Animator animator;
    public SurvivalCameraRig cameraRig;
    public Renderer[] modelRenderers;
    public string walkingParameter="isWalking";
    CharacterController controller;
    float verticalSpeed,pitch;
    bool appliedMode;
    bool[] rendererStates;
    Vector3 heldForward,heldRight;
    int walkingHash;
    bool inputLatched,latchedTank;
    bool wasInAirAnimation;
    bool jumpInProgress;
    public void SetFirstPerson(bool value){firstPerson=value;if(Application.isPlaying)ApplyMode();}
    public void Teleport(Vector3 point)
    {
        var cc=GetComponent<CharacterController>();cc.enabled=false;transform.position=point;cc.enabled=true;verticalSpeed=0;inputLatched=false;heldForward=Vector3.zero;
        jumpInProgress=false;wasInAirAnimation=false;
        if(animator)animator.SetBool("isJumping",false);
    }
    public void TeleportFirstPerson(Transform destination)
    {
        if(!destination)return;
        Teleport(destination.position);transform.rotation=Quaternion.Euler(0,destination.eulerAngles.y,0);pitch=0;
        SetFirstPerson(true);if(cameraRig)cameraRig.UpdateView(0,pitch);
    }
    void Awake()
    {
        controller=GetComponent<CharacterController>();walkingHash=Animator.StringToHash(walkingParameter);
        if(!animator)animator=GetComponentInChildren<Animator>();
        if(animator)animator.applyRootMotion=false;
        if(modelRenderers==null||modelRenderers.Length==0)modelRenderers=GetComponentsInChildren<Renderer>();
        rendererStates=new bool[modelRenderers.Length];for(int i=0;i<modelRenderers.Length;i++)rendererStates[i]=modelRenderers[i].forceRenderingOff;
        ApplyMode();
    }
    void ApplyMode()
    {
        appliedMode=EffectiveFirstPerson;heldForward=Vector3.zero;inputLatched=false;
        cameraRig.SetFirstPerson(firstPerson);
        for(int i=0;i<modelRenderers.Length;i++)if(modelRenderers[i])modelRenderers[i].forceRenderingOff=EffectiveFirstPerson||rendererStates[i];
        Cursor.lockState=EffectiveFirstPerson?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!EffectiveFirstPerson;
    }
    void Update()
    {
        if(modalInputLocked||dialogueClickFrame==Time.frameCount)
        {
            if(animator){animator.SetBool(walkingParameter,false);animator.SetBool("isWalkingBackward",false);animator.SetBool("isRunning",false);}
            return;
        }
        if(ShopReturning){AdvanceShopReturn(Time.deltaTime);return;}
        if(dialogueLocked){Move(Vector2.zero,Vector2.zero,false,false,Time.deltaTime);return;}
        if(appliedMode!=EffectiveFirstPerson)ApplyMode();
        var k=Keyboard.current;var m=Mouse.current;if(k==null)return;
        if(k.escapeKey.wasPressedThisFrame){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        if(EffectiveFirstPerson && m!=null && m.leftButton.wasPressedThisFrame){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        Vector2 axes=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
        Vector2 look=EffectiveFirstPerson && Cursor.lockState==CursorLockMode.Locked && m!=null?m.delta.ReadValue():Vector2.zero;
        Move(axes,look,EffectiveFirstPerson&&k.spaceKey.wasPressedThisFrame,k.leftShiftKey.isPressed,Time.deltaTime);
    }
    // Also available for deterministic play-mode verification.
    public void Move(Vector2 axes,Vector2 look,bool jump,bool run,float dt)
    {
        if(!controller)controller=GetComponent<CharacterController>();
        cameraRig.RefreshSelection();
        // A doorway/camera cut must not reinterpret keys already held by the player.
        // Keep both the control scheme and camera basis until movement keys are released.
        if(axes.sqrMagnitude<.001f){inputLatched=false;heldForward=Vector3.zero;}
        if(!inputLatched && axes.sqrMagnitude>.001f)
        {
            latchedTank=cameraRig.IsIndoors?tankControlsIndoors:tankControlsOutdoors;
            heldForward=Vector3.ProjectOnPlane(cameraRig.output.transform.forward,Vector3.up).normalized;
            heldRight=Vector3.Cross(Vector3.up,heldForward);inputLatched=true;
        }
        bool tank=!EffectiveFirstPerson && (inputLatched?latchedTank:cameraRig.IsIndoors?tankControlsIndoors:tankControlsOutdoors);
        if(!tank)axes=Vector2.ClampMagnitude(axes,1);
        Vector3 direction;
        if(EffectiveFirstPerson)
        {
            transform.Rotate(0,look.x*mouseSensitivity,0);pitch=Mathf.Clamp(pitch-look.y*mouseSensitivity,-80,80);
            direction=transform.forward*axes.y+transform.right*axes.x;
        }
        else if(tank)
        {
            transform.Rotate(0,axes.x*tankTurnSpeed*dt,0);direction=transform.forward*axes.y;
        }
        else
        {
            if(axes.sqrMagnitude<.001f||heldForward==Vector3.zero)
            {
                heldForward=Vector3.ProjectOnPlane(cameraRig.output.transform.forward,Vector3.up).normalized;
                heldRight=Vector3.Cross(Vector3.up,heldForward);
            }
            direction=heldForward*axes.y+heldRight*axes.x;
            if(direction.sqrMagnitude>.001f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),turnSpeed*dt);
        }
        if(controller.isGrounded && verticalSpeed<0)verticalSpeed=-2;
        if(jump && EffectiveFirstPerson && controller.isGrounded)
        {
            verticalSpeed=Mathf.Sqrt(2*gravity*jumpHeight);
            jumpInProgress=true;
        }
        verticalSpeed=Mathf.Max(verticalSpeed-gravity*dt,-35);
        Vector3 before=transform.position;
        bool backward=(EffectiveFirstPerson||tank) && axes.y<-.1f;
        bool running=run && !backward;
        float speed=walkSpeed*(backward?backwardSpeedMultiplier:running?runMultiplier:1);
        controller.Move((direction*speed+Vector3.up*verticalSpeed)*dt);
        if(jumpInProgress&&verticalSpeed<=0&&controller.isGrounded)jumpInProgress=false;
        Vector3 actual=transform.position-before;
        actual.y=0;
        if(animator)
        {
            bool inAir=jumpInProgress;
            animator.SetBool("isJumping",inAir);
            if(inAir&&!wasInAirAnimation&&animator.HasState(0,Animator.StringToHash("Base Layer.Jump")))
                animator.Play("Base Layer.Jump",0,0);
            wasInAirAnimation=inAir;
            bool moving=!inAir&&actual.sqrMagnitude>.000001f;
            animator.SetBool(walkingHash,moving);
            animator.SetBool("isWalkingBackward",moving && backward);
            animator.SetBool("isRunning",moving && running);
        }
    }
    void LateUpdate(){if(modalInputLocked)return;cameraRig.UpdateView(Time.deltaTime,pitch);if(appliedMode!=EffectiveFirstPerson)ApplyMode();}
    void OnDisable()
    {
        jumpInProgress=false;wasInAirAnimation=false;
        if(animator)animator.SetBool("isJumping",false);
        if(rendererStates!=null)for(int i=0;i<modelRenderers.Length;i++)if(modelRenderers[i])modelRenderers[i].forceRenderingOff=rendererStates[i];
        if(animator){animator.SetBool(walkingParameter,false);animator.SetBool("isWalkingBackward",false);animator.SetBool("isRunning",false);}
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
}

