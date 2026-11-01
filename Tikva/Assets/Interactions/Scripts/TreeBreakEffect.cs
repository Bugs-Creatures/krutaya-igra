using UnityEngine;

public sealed class TreeBreakEffect : MonoBehaviour
{
    public Rigidbody[] halves;
    public ParticleSystem chips;
    [Min(.1f)] public float lifetime=1.1f;
    public void Burst(Vector3 direction)
    {
        var sideways=Vector3.Cross(Vector3.up,direction).normalized;
        if(sideways.sqrMagnitude<.01f)sideways=Vector3.forward;
        for(int n=0;n<halves.Length;n++)
        {
            var body=halves[n];body.isKinematic=false;body.useGravity=true;
            body.linearVelocity=sideways*(n==0?-2.4f:2.4f)+Vector3.up*.8f;
            body.angularVelocity=direction*(n==0?-1.3f:1.3f);
        }
        if(chips)chips.Play(true);
        Destroy(gameObject,lifetime);
    }
}
