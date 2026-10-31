using UnityEngine;

public sealed class ZaikaDialogue : MonoBehaviour, ITalkTarget
{
    public InteractionObject hookReward;
    [Min(0)] public float speechPause=.18f;
    public bool learned;
    public bool respectful;
    public bool RewardGiven {get;private set;}
    NpcDialoguePanel dialogue;
    void Start(){dialogue=gameObject.AddComponent<NpcDialoguePanel>();}
    public void Talk()
    {
        var player=Object.FindAnyObjectByType<SurvivalPlayerController>();
        if(player.dialogueLocked||player.modalInputLocked||player.ShopReturning||dialogue.IsOpen)return;
        if(learned)
        {
            dialogue.Begin(false);dialogue.Say(respectful?"Чудик: Простите.":"Чудик: Ещё раз спасибо!",hold:2,periodPause:respectful?speechPause:0);return;
        }
        var direction=player.transform.position-transform.position;direction.y=0;
        if(direction.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(direction);
        dialogue.Begin();
        dialogue.Say("Чудик: Привет. Чего. Ты. Пристал?","Что у тебя с речью?","Прости, не хотел тревожить.",FirstChoice,periodPause:speechPause);
    }
    void FirstChoice(int answer)
    {
        if(answer==1){dialogue.End();return;}
        dialogue.Say("Чудик: А. Что. С. Ней. Не. Так?","Речь медленная и прерывистая.","Поуважительнее надо.",Learn,periodPause:speechPause);
    }
    void Learn(int answer)
    {
        learned=true;respectful=answer==1;
        dialogue.Say(respectful?"Чудик: Простите. Вот. Вам. В. Знак. Извинения.":"Чудик: Спасибо, вот возьми, это за твою помощь.",hold:1,complete:()=>{GiveReward();dialogue.End();},periodPause:respectful?speechPause:0);
    }
    void GiveReward()
    {
        if(RewardGiven||!hookReward)return;RewardGiven=true;
        var player=Object.FindAnyObjectByType<SurvivalPlayerController>();Vector3 point=transform.position+transform.forward*.9f+Vector3.up*.6f;
        for(int n=0;n<8;n++)
        {
            float angle=n==0?0:((n+1)/2)*15*(n%2==1?1:-1);
            var candidate=transform.position+Quaternion.Euler(0,angle,0)*transform.forward*.9f+Vector3.up*.6f;
            bool free=true;foreach(var collider in Physics.OverlapSphere(candidate,.2f,~0,QueryTriggerInteraction.Ignore))
                if(!collider.transform.IsChildOf(transform)&&!collider.transform.IsChildOf(player.transform))free=false;
            if(free){point=candidate;break;}
        }
        hookReward.transform.SetParent(null,true);hookReward.transform.position=point;hookReward.gameObject.SetActive(true);
        hookReward.Attach(transform);hookReward.Drop(point,GetComponentsInChildren<Collider>());
    }
    void OnDisable(){if(dialogue)dialogue.End();}
}
