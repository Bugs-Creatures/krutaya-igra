using UnityEngine;

// Vector uGUI geometry: an antialiased ring or disc without a raster texture.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class InteractionReticle : UnityEngine.UI.MaskableGraphic
{
    bool filled;
    float thickness=1;
    public bool Filled
    {
        get=>filled;
        set{if(filled==value)return;filled=value;SetVerticesDirty();}
    }
    public float Thickness
    {
        get=>thickness;
        set{if(Mathf.Approximately(thickness,value))return;thickness=value;SetVerticesDirty();}
    }
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();var rect=GetPixelAdjustedRect();var centre=rect.center;
        float radius=Mathf.Min(rect.width,rect.height)*.5f-.4f;if(radius<=0)return;
        const int segments=64;
        float edge=Mathf.Min(.4f,thickness*.4f);
        var rings=filled?new[]{radius-edge,radius+edge}:new[]{radius+edge,radius-edge,Mathf.Max(0,radius-thickness+edge),Mathf.Max(0,radius-thickness-edge)};
        for(int ring=0;ring<rings.Length;ring++)
        {
            var tint=color;
            if(filled?ring==1:ring==0||ring==3)tint.a=0;
            for(int n=0;n<segments;n++)
            {
                float angle=n*Mathf.PI*2/segments;
                mesh.AddVert(centre+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*rings[ring],tint,Vector2.zero);
            }
        }
        for(int ring=0;ring<rings.Length-1;ring++)for(int n=0;n<segments;n++)
        {
            int a=ring*segments+n,b=ring*segments+(n+1)%segments,c=a+segments,d=b+segments;
            mesh.AddTriangle(a,b,c);mesh.AddTriangle(b,d,c);
        }
        if(filled)
        {
            int middle=mesh.currentVertCount;mesh.AddVert(centre,color,Vector2.zero);
            for(int n=0;n<segments;n++)mesh.AddTriangle(middle,n,(n+1)%segments);
        }
    }
}
