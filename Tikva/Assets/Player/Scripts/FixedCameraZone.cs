using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera),typeof(BoxCollider))]
public sealed class FixedCameraZone : MonoBehaviour
{
    public static readonly HashSet<FixedCameraZone> Active = new();
    public int priority;
    public BoxCollider volume;
    public BoxCollider[] additionalVolumes;
    [Tooltip("Optional additional check of camera frustum. Zones alone are more reliable at doors.")]
    public bool requireVisibleInCamera;
    [Min(1)] public float maxViewDistance=30;
    public Camera Shot => GetComponent<Camera>();
    public bool Contains(Vector3 feet)
    {
        var box=volume?volume:GetComponent<BoxCollider>();
        bool Inside(BoxCollider b)=>b && new Bounds(b.center,b.size).Contains(b.transform.InverseTransformPoint(feet+Vector3.up*.9f));
        if(Inside(box))return true;
        if(additionalVolumes!=null)foreach(var b in additionalVolumes)if(Inside(b))return true;
        return false;
    }
    public bool Accepts(Vector3 feet)
    {
        if(!Contains(feet))return false;
        if(!requireVisibleInCamera)return true;
        Vector3 p=Shot.WorldToViewportPoint(feet+Vector3.up);
        return p.z>0 && p.z<maxViewDistance && p.x>=0 && p.x<=1 && p.y>=0 && p.y<=1;
    }
    void OnEnable(){Active.Add(this);Shot.enabled=false;GetComponent<BoxCollider>().isTrigger=true;}
    void OnDisable()=>Active.Remove(this);
    void OnDrawGizmosSelected()
    {
        var b=volume?volume:GetComponent<BoxCollider>();if(!b)return;
        Gizmos.color=new Color(.95f,.7f,.2f,.6f);Gizmos.matrix=b.transform.localToWorldMatrix;Gizmos.DrawWireCube(b.center,b.size);
    }
}
