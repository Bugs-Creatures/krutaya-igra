using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(1200)]
public sealed class FirstPersonHeldItemRenderer : MonoBehaviour
{
    public const int ItemLayer=31;
    public const string LayerName="FirstPersonHeldItem";
    public PlayerInteraction interaction;
    static readonly HashSet<FirstPersonHeldItemRenderer> active=new();
    readonly Dictionary<Transform,int> originalLayers=new();
    readonly Dictionary<Renderer,ShadowCastingMode> originalShadows=new();
    readonly Dictionary<Renderer,bool> originalOcclusion=new();
    InteractionObject renderedItem;
    public bool HasItem=>isActiveAndEnabled&&interaction&&interaction.player&&interaction.player.EffectiveFirstPerson&&interaction.heldItem;
    public static bool Renders(Camera camera)
    {
        foreach(var renderer in active)
            if(renderer&&renderer.HasItem&&renderer.interaction.player.cameraRig.output==camera)return true;
        return false;
    }
    void Awake(){if(!interaction)interaction=GetComponent<PlayerInteraction>();}
    void OnEnable()=>active.Add(this);
    void Restore()
    {
        foreach(var pair in originalLayers)if(pair.Key)pair.Key.gameObject.layer=pair.Value;
        foreach(var pair in originalShadows)if(pair.Key)pair.Key.shadowCastingMode=pair.Value;
        foreach(var pair in originalOcclusion)if(pair.Key)pair.Key.allowOcclusionWhenDynamic=pair.Value;
        originalLayers.Clear();originalShadows.Clear();originalOcclusion.Clear();renderedItem=null;
    }
    void LateUpdate()
    {
        var next=HasItem?interaction.heldItem:null;
        if(next==renderedItem)return;
        Restore();renderedItem=next;if(!next)return;
        foreach(var child in next.GetComponentsInChildren<Transform>(true))
        {originalLayers.Add(child,child.gameObject.layer);child.gameObject.layer=ItemLayer;}
        foreach(var renderer in next.GetComponentsInChildren<Renderer>(true))
        {
            originalShadows.Add(renderer,renderer.shadowCastingMode);renderer.shadowCastingMode=ShadowCastingMode.Off;
            originalOcclusion.Add(renderer,renderer.allowOcclusionWhenDynamic);renderer.allowOcclusionWhenDynamic=false;
        }
    }
    void OnDisable(){active.Remove(this);Restore();}
}
