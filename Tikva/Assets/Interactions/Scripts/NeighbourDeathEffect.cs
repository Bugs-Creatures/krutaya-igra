using UnityEngine;

public sealed class NeighbourDeathEffect : MonoBehaviour
{
    public Rigidbody[] pieces;
    public ParticleSystem blood;
    public bool HasBurst {get;private set;}
    public void Burst(Vector3 attackDirection)
    {
        if(HasBurst)return;HasBurst=true;
        // Initially overlapping fragments must not explode from mutual depenetration.
        for(int a=0;a<pieces.Length;a++)for(int b=a+1;b<pieces.Length;b++)
            Physics.IgnoreCollision(pieces[a].GetComponent<Collider>(),pieces[b].GetComponent<Collider>());
        foreach(var piece in pieces)
        {
            var direction=Random.onUnitSphere;direction.y=Mathf.Abs(direction.y)+.4f;
            piece.isKinematic=false;piece.useGravity=true;
            piece.linearVelocity=direction.normalized*Random.Range(1.7f,3.5f)+attackDirection*.9f;
            piece.angularVelocity=Random.insideUnitSphere*12;
        }
        if(blood)blood.Play(true);
        Destroy(gameObject,8);
    }
}
