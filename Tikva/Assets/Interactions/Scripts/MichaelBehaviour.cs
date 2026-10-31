using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class MichaelBehaviour : MonoBehaviour, ITalkTarget
{
    public Transform[] waypoints;
    public Animator animator;
    public InteractionObject mapReward;
    public float walkSpeed=1.1f;
    public float fastMultiplier=1.65f;
    public float turnSpeed=360;
    public bool learned,walkForward,walkFaster;
    public bool InConversation {get;private set;}
    public bool RewardGiven {get;private set;}
    CharacterController controller;
    NpcDialoguePanel dialogue;
    int next;
    void Start()
    {
        controller=GetComponent<CharacterController>();if(!animator)animator=GetComponentInChildren<Animator>();
        animator.applyRootMotion=false;dialogue=gameObject.AddComponent<NpcDialoguePanel>();
        if(waypoints!=null&&waypoints.Length>1){float best=float.MaxValue;for(int n=0;n<waypoints.Length;n++)if(waypoints[n]&&(waypoints[n].position-transform.position).sqrMagnitude<best){best=(waypoints[n].position-transform.position).sqrMagnitude;next=(n+1)%waypoints.Length;}}
    }
    void Update()
    {
        if(!animator)return;animator.SetBool("walkForward",walkForward);animator.SetBool("isWalking",!InConversation);animator.speed=walkFaster&&!InConversation?fastMultiplier:1;
        if(InConversation||waypoints==null||waypoints.Length==0||!waypoints[next])return;
        var direction=waypoints[next].position-transform.position;direction.y=0;
        if(direction.magnitude<.2f){next=(next+1)%waypoints.Length;return;}
        transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(walkForward?direction:-direction),turnSpeed*Time.deltaTime);
        controller.Move((direction.normalized*walkSpeed*(walkFaster?fastMultiplier:1)+Vector3.down*2)*Time.deltaTime);
    }
    public void Talk()
    {
        var player=Object.FindAnyObjectByType<SurvivalPlayerController>();if(InConversation||player.dialogueLocked||player.modalInputLocked)return;
        if(learned)
        {
            if(dialogue.IsOpen)return;
            dialogue.Begin(false);dialogue.Say("Майкл: Ещё раз спасибо!",hold:2);return;
        }
        InConversation=true;animator.SetBool("isWalking",false);animator.speed=1;
        var direction=player.transform.position-transform.position;direction.y=0;if(direction.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(direction);
        dialogue.Begin();
        dialogue.Say("Майкл: Что-то случилось?","Ты как-то странно ходишь.","Не важно.",FirstChoice);
    }
    void FirstChoice(int choice)
    {
        if(choice==1){Finish();return;}
        dialogue.Say("Майкл: А как ходить правильно?","Надо идти лицом вперёд.","Надо идти быстрее.",Learn);
    }
    void Learn(int choice)
    {
        learned=true;walkForward=choice==0;walkFaster=choice==1;
        dialogue.Say("Майкл: Спасибо! Я то я всё время прихожу не туда.",hold:1,complete:()=>dialogue.Say("Майкл: Держи, может тебе пригодится.",hold:1,complete:()=>{GiveMap();Finish();}));
    }
    void GiveMap()
    {
        if(RewardGiven||!mapReward)return;RewardGiven=true;
        // Place the reward on the player's side, outside the walking lane.
        var player=Object.FindAnyObjectByType<SurvivalPlayerController>();
        Vector3 lane=waypoints!=null&&waypoints.Length>0?waypoints[next].position-transform.position:transform.forward;lane.y=0;if(lane.sqrMagnitude<.01f)lane=transform.forward;
        var side=Vector3.Cross(Vector3.up,lane.normalized);if(Vector3.Dot(side,player.transform.position-transform.position)<0)side=-side;
        Vector3 point=transform.position+side*1.2f+Vector3.up*.6f;
        var hits=Physics.OverlapBox(point,new Vector3(.3f,.15f,.3f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
        bool blocked=false;foreach(var hit in hits)if(!hit.transform.IsChildOf(transform)&&!hit.transform.IsChildOf(player.transform))blocked=true;
        if(blocked)point=transform.position-side*1.2f+Vector3.up*.6f;
        mapReward.transform.SetParent(null,true);mapReward.transform.position=point;mapReward.gameObject.SetActive(true);
        var body=mapReward.GetComponent<Rigidbody>();if(body){body.isKinematic=false;body.useGravity=true;body.position=point;body.linearVelocity=Vector3.zero;}
    }
    void Finish(){dialogue.End();InConversation=false;}
    void OnDisable(){if(dialogue)dialogue.End();InConversation=false;}
}
