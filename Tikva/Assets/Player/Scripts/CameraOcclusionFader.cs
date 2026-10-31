using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class CameraOcclusionFader : MonoBehaviour
{
    [Range(.03f,.6f)] public float occludedOpacity=.12f;
    [Min(.01f)] public float fadeDuration=.18f;
    public float sightRadius=.25f;
    public LayerMask obstacleLayers=~0;
    readonly HashSet<Renderer> wanted=new();
    readonly Dictionary<Renderer,State> states=new();
    readonly Dictionary<Material,Material> transparent=new();
    static readonly int BaseColor=Shader.PropertyToID("_BaseColor");
    static readonly int ColorId=Shader.PropertyToID("_Color");
    public int FadedRendererCount {get;private set;}
    sealed class State
    {
        public Renderer renderer;
        public Material[] originals,faded;
        public MaterialPropertyBlock[] originalBlocks,blocks;
        public float alpha=1;
        public bool assigned;
    }
    Material Transparent(Material original)
    {
        if(!original || !original.HasProperty(BaseColor))return original;
        if(transparent.TryGetValue(original,out var cached))return cached;
        var m=new Material(original){name=original.name+" (camera fade)",hideFlags=HideFlags.HideAndDontSave};
        m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_ZWrite",0);m.SetFloat("_AlphaClip",0);
        m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);m.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
        m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)RenderQueue.Transparent;
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.DisableKeyword("_ALPHATEST_ON");m.DisableKeyword("_ALPHAPREMULTIPLY_ON");m.DisableKeyword("_ALPHAMODULATE_ON");
        // Fade only the camera view. Keep the original solid silhouette in the shadow map.
        m.SetShaderPassEnabled("ShadowCaster",original.GetShaderPassEnabled("ShadowCaster"));
        m.SetShaderPassEnabled("DepthOnly",false);m.SetShaderPassEnabled("DepthNormals",false);
        transparent.Add(original,m);return m;
    }
    State Capture(Renderer renderer)
    {
        var state=new State{renderer=renderer,originals=renderer.sharedMaterials};int n=state.originals.Length;
        state.faded=new Material[n];state.originalBlocks=new MaterialPropertyBlock[n];state.blocks=new MaterialPropertyBlock[n];
        for(int i=0;i<n;i++)
        {
            state.faded[i]=Transparent(state.originals[i]);state.originalBlocks[i]=new MaterialPropertyBlock();state.blocks[i]=new MaterialPropertyBlock();
            renderer.GetPropertyBlock(state.originalBlocks[i],i);renderer.GetPropertyBlock(state.blocks[i],i);
        }
        states.Add(renderer,state);return state;
    }
    void Add(Renderer r,Transform player)
    {
        if(!r||!r.enabled||r.transform.IsChildOf(player))return;
        var group=r.GetComponentInParent<CameraOcclusionGroup>();
        if(group)foreach(var member in group.Renderers){if(member && member.enabled && !member.transform.IsChildOf(player))wanted.Add(member);}
        else wanted.Add(r);
    }
    public void Tick(Transform player,Camera camera,bool active,float dt)
    {
        if(!Application.isPlaying)return;
        wanted.Clear();
        if(active && player && camera)
        {
            foreach(var group in CameraOcclusionGroup.Active)
                if(group && group.envelope && group.envelope.bounds.Contains(camera.transform.position))
                    foreach(var r in group.Renderers)if(r && r.enabled && !r.transform.IsChildOf(player))wanted.Add(r);
            foreach(var collider in Physics.OverlapSphere(camera.transform.position,.3f,obstacleLayers,QueryTriggerInteraction.Ignore))Add(collider.GetComponent<Renderer>(),player);
            for(int sample=0;sample<2;sample++)
            {
                var delta=player.position+Vector3.up*(sample==0?.85f:1.55f)-camera.transform.position;
                foreach(var hit in Physics.SphereCastAll(camera.transform.position,sightRadius,delta.normalized,Mathf.Max(0,delta.magnitude-.35f),obstacleLayers,QueryTriggerInteraction.Ignore))
                    Add(hit.collider.GetComponent<Renderer>(),player);
            }
        }
        foreach(var r in wanted)if(r && !states.ContainsKey(r))Capture(r);
        FadedRendererCount=0;
        foreach(var state in states.Values)
        {
            var r=state.renderer;if(!r)continue;
            state.alpha=Mathf.MoveTowards(state.alpha,wanted.Contains(r)?occludedOpacity:1,dt/Mathf.Max(.01f,fadeDuration));
            if(state.alpha<.999f)
            {
                if(!state.assigned){r.sharedMaterials=state.faded;state.assigned=true;}
                for(int i=0;i<state.originals.Length;i++)
                {
                    var m=state.originals[i];if(!m||!m.HasProperty(BaseColor))continue;
                    Color c=m.GetColor(BaseColor);c.a*=state.alpha;state.blocks[i].SetColor(BaseColor,c);
                    if(m.HasProperty(ColorId))state.blocks[i].SetColor(ColorId,c);
                    r.SetPropertyBlock(state.blocks[i],i);
                }
                FadedRendererCount++;
            }
            else if(state.assigned)Restore(state);
        }
    }
    static void Restore(State state)
    {
        if(state.renderer)
        {
            state.renderer.sharedMaterials=state.originals;
            for(int i=0;i<state.originalBlocks.Length;i++)state.renderer.SetPropertyBlock(state.originalBlocks[i],i);
        }
        state.assigned=false;
    }
    public void RestoreAll()
    {
        foreach(var state in states.Values)Restore(state);
        states.Clear();wanted.Clear();FadedRendererCount=0;
        foreach(var material in transparent.Values)if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
        transparent.Clear();
    }
    void OnDisable()=>RestoreAll();
}
