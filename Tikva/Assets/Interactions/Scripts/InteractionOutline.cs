using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class InteractionOutline : MonoBehaviour
{
    [Range(1,8)] public float width=3;
    public Shader outlineShader;
    readonly List<Renderer> shells=new();
    Material mask,line;
    public bool IsShowing {get;private set;}
    public Color CurrentColor {get;private set;}
    void Build()
    {
        if(line)return;
        var shader=outlineShader?outlineShader:Shader.Find("Interaction/Outline");if(!shader)return;
        mask=new Material(shader){hideFlags=HideFlags.HideAndDontSave,renderQueue=3020};
        mask.SetFloat("_Pixels",0);mask.SetFloat("_Cull",0);mask.SetFloat("_ColorMask",0);mask.SetFloat("_StencilComp",8);mask.SetFloat("_StencilPass",2);
        line=new Material(shader){hideFlags=HideFlags.HideAndDontSave,renderQueue=3021};line.SetFloat("_Pixels",width);
        var originals=GetComponentsInChildren<Renderer>();
        foreach(var original in originals)
        {
            Mesh mesh=null;
            if(original is SkinnedMeshRenderer skin)mesh=skin.sharedMesh;
            else {var f=original.GetComponent<MeshFilter>();if(f)mesh=f.sharedMesh;}
            if(!mesh)continue;
            for(int pass=0;pass<2;pass++)
            {
            var g=new GameObject("Interaction outline");g.layer=2;g.transform.SetParent(original.transform,false);
            Renderer shell;
            if(original is SkinnedMeshRenderer source)
            {
                var s=g.AddComponent<SkinnedMeshRenderer>();s.sharedMesh=mesh;s.bones=source.bones;s.rootBone=source.rootBone;s.localBounds=source.localBounds;shell=s;
            }
            else {g.AddComponent<MeshFilter>().sharedMesh=mesh;shell=g.AddComponent<MeshRenderer>();}
            // Render one stencil mask per submesh before the expanded silhouette.
            var materials=new Material[mesh.subMeshCount];for(int i=0;i<mesh.subMeshCount;i++)materials[i]=pass==0?mask:line;
            shell.sharedMaterials=materials;shell.shadowCastingMode=ShadowCastingMode.Off;shell.receiveShadows=false;shell.enabled=false;shells.Add(shell);
            }
        }
    }
    public void Show(bool visible,Color color)
    {
        if(visible)Build();IsShowing=visible;CurrentColor=color;
        if(line){line.SetColor("_Color",color);line.SetFloat("_Pixels",width);}
        foreach(var shell in shells)if(shell)shell.enabled=visible;
    }
    void OnDisable()=>Show(false,Color.green);
    void OnDestroy(){if(mask)Destroy(mask);if(line)Destroy(line);}
}
