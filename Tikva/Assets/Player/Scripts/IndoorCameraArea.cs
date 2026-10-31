using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class IndoorCameraArea : MonoBehaviour
{
    [Tooltip("Used in rooms where a dedicated camera has not been added yet.")]
    public Camera fallbackCamera;
    public bool Contains(Vector3 feet)
    {
        var b=GetComponent<BoxCollider>();
        return new Bounds(b.center,b.size).Contains(transform.InverseTransformPoint(feet+Vector3.up*.9f));
    }
    void OnEnable()=>GetComponent<BoxCollider>().isTrigger=true;
    void OnDrawGizmosSelected(){var b=GetComponent<BoxCollider>();if(!b)return;Gizmos.matrix=transform.localToWorldMatrix;Gizmos.color=Color.cyan;Gizmos.DrawWireCube(b.center,b.size);}
}
