using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class CameraBlindZone : MonoBehaviour
{
    public static readonly HashSet<CameraBlindZone> Active=new();
    public bool Contains(Vector3 feet,float margin=0)
    {
        var box=GetComponent<BoxCollider>();
        return new Bounds(box.center,box.size+Vector3.one*margin*2).Contains(transform.InverseTransformPoint(feet+Vector3.up));
    }
    void OnEnable(){Active.Add(this);GetComponent<BoxCollider>().isTrigger=true;}
    void OnDisable()=>Active.Remove(this);
    void OnDrawGizmos()
    {
        var box=GetComponent<BoxCollider>();if(!box)return;
        Gizmos.matrix=transform.localToWorldMatrix;Gizmos.color=new Color(1,.2f,.3f,.13f);Gizmos.DrawCube(box.center,box.size);
        Gizmos.color=new Color(1,.2f,.3f,.8f);Gizmos.DrawWireCube(box.center,box.size);
    }
}
