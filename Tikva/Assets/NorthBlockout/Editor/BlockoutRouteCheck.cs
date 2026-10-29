using System.Collections.Generic;
using UnityEngine;
using NorthBlockout;

public static class BlockoutRouteCheck
{
    public static object Run()
    {
        var w=Object.FindAnyObjectByType<BlockoutWalker>();
        var cc=w.GetComponent<CharacterController>();
        Vector3 original=w.transform.position;
        var reports=new List<string>();
        w.Teleport(new Vector3(5.3f,3.06f,15));Physics.SyncTransforms();
        var route=new[]{
            new Vector3(3.75f,3,14.8f),new Vector3(3.75f,3,13.7f),new Vector3(1.35f,3,13.7f),
            new Vector3(1.35f,1.5f,18.1f),new Vector3(-1.35f,1.5f,18.1f),new Vector3(-1.35f,0,13.3f),
            new Vector3(0,0,13.3f),new Vector3(0,0,10),new Vector3(0,0,5.2f),new Vector3(-8,0,5.2f),
            new Vector3(-8,0,1.8f),new Vector3(-3.5f,0,1.8f),new Vector3(-3.5f,0,-9.5f),
            new Vector3(-17.5f,0,-9.5f),new Vector3(-17.5f,0,-14),new Vector3(-17.5f,0,-19),
            new Vector3(-17.5f,0,-14),new Vector3(-17.5f,0,-9.5f),new Vector3(12.5f,0,-9.5f),
            new Vector3(12.5f,0,-16),new Vector3(12.5f,0,-20.5f),new Vector3(20,0,-20.5f),
            new Vector3(12.5f,0,-20.5f),new Vector3(12.5f,0,-9.5f),new Vector3(-3.5f,0,-9.5f),
            new Vector3(-3.5f,0,1.8f),new Vector3(0,0,1.8f),new Vector3(0,0,13.3f),
            new Vector3(-1.35f,0,13.3f),new Vector3(-1.35f,1.5f,18.1f),new Vector3(1.35f,1.5f,18.1f),
            new Vector3(1.35f,3,13.7f),new Vector3(3.75f,3,13.7f),new Vector3(3.75f,3,14.8f),new Vector3(5.3f,3,14.8f),
            new Vector3(3.75f,3,14.8f),new Vector3(3.75f,3,17.6f),new Vector3(8.4f,3,17.6f),new Vector3(8.4f,3,15.8f),new Vector3(8.4f,3,17.6f),new Vector3(12.4f,3,17.6f),new Vector3(12.4f,3,15.8f),new Vector3(12.4f,3,17.6f),new Vector3(9.3f,3,17.6f),new Vector3(9.3f,3,19.1f)
        };
        int passed=0;
        for(int n=0;n<route.Length;n++)
        {
            Vector3 goal=route[n];
            for(int i=0;i<1600;i++)
            {
                Vector3 delta=goal-w.transform.position;delta.y=0;
                if(delta.magnitude<.055f)break;
                cc.Move(Vector3.ClampMagnitude(delta,.04f)+Vector3.down*.055f);
            }
            for(int i=0;i<8;i++)cc.Move(Vector3.down*.055f);
            Vector3 current=w.transform.position;
            if(Vector2.Distance(new Vector2(current.x,current.z),new Vector2(goal.x,goal.z))>.12f || Mathf.Abs(current.y-goal.y)>.3f)
            {
                reports.Add("FAIL waypoint "+n+" goal="+goal+" actual="+current);break;
            }
            passed++;
        }
        w.Teleport(original);w.RefreshView();
        return new{passed,total=route.Length,reports};
    }
}

