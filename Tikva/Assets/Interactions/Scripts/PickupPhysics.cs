using System.Collections.Generic;
using UnityEngine;

public static class PickupPhysics
{
    const float MinimumWorldThickness=.035f;

    // Swept CCD supports primitive colliders. Thin convex render meshes are poor
    // collision shapes for small dropped tools, especially against a heightfield.
    public static Collider[] PrepareColliders(GameObject item,bool immediate=false)
    {
        RecenterFarPivot(item);
        var result=new List<Collider>();
        foreach(var collider in item.GetComponentsInChildren<Collider>(true))
        {
            if(collider is not MeshCollider mesh){result.Add(collider);continue;}
            var bounds=mesh.sharedMesh?mesh.sharedMesh.bounds:new Bounds(Vector3.zero,Vector3.one*.1f);
            var scale=mesh.transform.lossyScale;
            var minimum=new Vector3(MinimumWorldThickness/Mathf.Max(.0001f,Mathf.Abs(scale.x)),MinimumWorldThickness/Mathf.Max(.0001f,Mathf.Abs(scale.y)),MinimumWorldThickness/Mathf.Max(.0001f,Mathf.Abs(scale.z)));
            var box=mesh.gameObject.AddComponent<BoxCollider>();
            box.center=bounds.center;box.size=Vector3.Max(bounds.size,minimum);
            box.enabled=mesh.enabled;box.isTrigger=mesh.isTrigger;box.sharedMaterial=mesh.sharedMaterial;
            box.contactOffset=Mathf.Max(.005f,mesh.contactOffset);
            box.includeLayers=mesh.includeLayers;box.excludeLayers=mesh.excludeLayers;box.layerOverridePriority=mesh.layerOverridePriority;
            result.Add(box);mesh.enabled=false;
            if(immediate)Object.DestroyImmediate(mesh);else Object.Destroy(mesh);
        }
        return result.ToArray();
    }

    public static void ConfigureBody(Rigidbody body)
    {
        body.collisionDetectionMode=body.isKinematic?CollisionDetectionMode.ContinuousSpeculative:CollisionDetectionMode.ContinuousDynamic;
    }

    public static void RecenterFarPivot(GameObject item)
    {
        bool found=false;Bounds bounds=default;
        foreach(var filter in item.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!filter.sharedMesh||filter.sharedMesh.vertexCount==0||filter.name=="Interaction outline")continue;
            var b=filter.sharedMesh.bounds;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {
                var point=filter.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));
                if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);
            }
        }
        if(!found||(bounds.center-item.transform.position).sqrMagnitude<4)return;
        var root=item.transform;var positions=new List<Vector3>();var children=new List<Transform>();
        foreach(Transform child in root){children.Add(child);positions.Add(child.position);}
        var rootBoxes=root.GetComponents<BoxCollider>();var boxCenters=new Vector3[rootBoxes.Length];
        for(int n=0;n<rootBoxes.Length;n++)boxCenters[n]=root.TransformPoint(rootBoxes[n].center);
        root.position=bounds.center;
        for(int n=0;n<children.Count;n++)children[n].position=positions[n];
        for(int n=0;n<rootBoxes.Length;n++)rootBoxes[n].center=root.InverseTransformPoint(boxCenters[n]);
        var body=item.GetComponent<Rigidbody>();
        if(body)
        {
            body.position=root.position;
            // Only a rare pivot correction needs an immediate transform sync.
            Physics.SyncTransforms();body.ResetCenterOfMass();body.ResetInertiaTensor();
        }
    }

    public static void GuardTerrain(Rigidbody body,Collider[] colliders,ref Terrain lastTerrain)
    {
        bool found=false;Bounds bounds=default;
        foreach(var collider in colliders)
        {
            if(!collider||!collider.enabled||collider.isTrigger||!collider.gameObject.activeInHierarchy)continue;
            if(!found){bounds=collider.bounds;found=true;}else bounds.Encapsulate(collider.bounds);
        }
        if(!found){lastTerrain=null;return;}
        foreach(var terrain in Terrain.activeTerrains)
        {
            var data=terrain.terrainData;var local=bounds.center-terrain.transform.position;
            if(local.x<0||local.z<0||local.x>data.size.x||local.z>data.size.z)continue;
            var surface=terrain.GetComponent<TerrainCollider>();
            if(!surface||!surface.enabled||Physics.GetIgnoreLayerCollision(body.gameObject.layer,surface.gameObject.layer))continue;
            int x=Mathf.Clamp((int)(local.x/data.size.x*data.holesResolution),0,data.holesResolution-1);
            int z=Mathf.Clamp((int)(local.z/data.size.z*data.holesResolution),0,data.holesResolution-1);
            if(data.IsHole(x,z)){lastTerrain=null;return;}
            float height=terrain.SampleHeight(bounds.center)+terrain.transform.position.y;
            // Only recover a crossing from above. Deliberately underground objects
            // and real terrain holes are left alone.
            if(lastTerrain==terrain&&bounds.max.y<height-.005f)
            {
                body.position+=Vector3.up*(height+.005f-bounds.min.y);
                var velocity=body.linearVelocity;if(velocity.y<0)velocity.y=0;body.linearVelocity=velocity;
                body.WakeUp();
                return;
            }
            lastTerrain=bounds.max.y>=height-.005f?terrain:null;
            return;
        }
        lastTerrain=null;
    }
}
