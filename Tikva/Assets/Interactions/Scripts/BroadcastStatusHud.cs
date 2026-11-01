using UnityEngine;

public sealed class BroadcastStatusHud : MonoBehaviour
{
    public SurvivalPlayerController player;
    public BroadcastCycle broadcast;
    public GameObject liveBadge;
    public bool IsVisible=>liveBadge&&liveBadge.activeSelf;

    void LateUpdate()
    {
        if(!liveBadge)return;
        bool visible=player&&broadcast&&broadcast.IsOnAir&&!player.EffectiveFirstPerson;
        if(liveBadge.activeSelf!=visible)liveBadge.SetActive(visible);
    }
    void OnDisable(){if(liveBadge)liveBadge.SetActive(false);}
}
