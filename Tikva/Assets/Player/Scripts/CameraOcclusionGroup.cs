using System.Collections.Generic;
using UnityEngine;

public sealed class CameraOcclusionGroup : MonoBehaviour
{
    public static readonly HashSet<CameraOcclusionGroup> Active=new();
    [Tooltip("Optional envelope used when the camera is inside this building.")]
    public BoxCollider envelope;
    Renderer[] renderers;
    public Renderer[] Renderers => renderers ??= GetComponentsInChildren<Renderer>(true);
    void OnEnable(){Active.Add(this);renderers=null;}
    void OnDisable()=>Active.Remove(this);
}
