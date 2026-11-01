using UnityEngine;

public sealed class RevealedTownMonster : MonoBehaviour, ITalkTarget
{
    public TownMonsterReveal reveal;
    public void Talk(){if(reveal)reveal.Speak();}
}
