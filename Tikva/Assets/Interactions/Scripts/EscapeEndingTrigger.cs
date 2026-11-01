using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class EscapeEndingTrigger : MonoBehaviour
{
    public BreakableTree tree;
    public EndingTransition ending;
    public string endingScene="Ending02";
    void OnTriggerEnter(Collider other)=>TryEscape(other);
    void OnTriggerStay(Collider other)=>TryEscape(other);
    void TryEscape(Collider other)
    {
        if(!tree||!tree.IsBroken||!ending||ending.IsRunning)return;
        var player=other.GetComponentInParent<SurvivalPlayerController>();
        if(player&&player==ending.player)ending.Begin(endingScene,0);
    }
}
