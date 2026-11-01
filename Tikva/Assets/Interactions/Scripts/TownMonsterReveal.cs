using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class RevealedNpcPlacement
{
    public InteractionObject person;
    public Transform destination;
}

public sealed class TownMonsterReveal : MonoBehaviour
{
    public CrimsonMassDialogue sourceDialogue;
    public GameObject monsterPrefab;
    public InteractionObject neighbour;
    public InteractionObject[] townspeople;
    public RevealedNpcPlacement[] placements=System.Array.Empty<RevealedNpcPlacement>();
    [TextArea] public string revealedPhrase="Сделай что должен.";
    public bool IsRevealed {get;private set;}
    readonly List<RevealedTownMonster> monsters=new();
    public IReadOnlyList<RevealedTownMonster> Monsters=>monsters;

    public void Reveal()
    {
        if(IsRevealed||!sourceDialogue||!monsterPrefab||townspeople==null)return;
        IsRevealed=true;
        var group=new GameObject("Город после раскрытия");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(group,gameObject.scene);
        var template=sourceDialogue.GetComponent<BoxCollider>();
        var visited=new HashSet<InteractionObject>();
        foreach(var person in townspeople)
        {
            if(!person||person==neighbour||person.gameObject==sourceDialogue.gameObject||!visited.Add(person)||!person.gameObject.activeInHierarchy)continue;
            Transform destination=person.transform;
            if(placements!=null)
                foreach(var placement in placements)
                    if(placement!=null&&placement.person==person&&placement.destination){destination=placement.destination;break;}
            var monster=Instantiate(monsterPrefab,destination.position,destination.rotation,group.transform);
            monster.name=person.name+" — раскрытый монстр";
            monster.transform.localScale=sourceDialogue.transform.lossyScale;
            var animator=monster.GetComponent<Animator>();if(animator)animator.applyRootMotion=false;
            var collider=monster.AddComponent<BoxCollider>();
            collider.center=template?template.center:new Vector3(0,.75f,0);
            collider.size=template?template.size:new Vector3(1.4f,1.5f,1.2f);
            var target=monster.AddComponent<InteractionObject>();
            target.kind=InteractionKind.Talk;target.displayName="???";target.availableHoverText="поговорить";
            target.reachDistance=person.reachDistance;target.hoverTextSize=person.hoverTextSize;
            var originalIndicator=person.GetComponent<TalkIndicator>();
            var indicator=monster.AddComponent<TalkIndicator>();
            if(originalIndicator)
            {
                indicator.icon=originalIndicator.icon;indicator.showDistance=originalIndicator.showDistance;
                indicator.iconSize=originalIndicator.iconSize;indicator.fadeSeconds=originalIndicator.fadeSeconds;
            }
            indicator.headOffset=(collider.center.y+collider.size.y*.5f)*monster.transform.lossyScale.y-1.8f+.3f;
            var talk=monster.AddComponent<RevealedTownMonster>();talk.reveal=this;monsters.Add(talk);
            // Disabling the original also stops patrols, shop logic and old interaction colliders.
            person.gameObject.SetActive(false);
        }
    }

    public void Speak(){if(IsRevealed&&sourceDialogue)sourceDialogue.SayAmbient(revealedPhrase);}
}
