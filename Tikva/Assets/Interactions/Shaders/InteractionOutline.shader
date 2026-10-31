Shader "Interaction/Outline"
{
    Properties
    {
        _Color("Color",Color)=(1,1,0,1)
        _Pixels("Width",Float)=3
        _Cull("Cull",Float)=1
        _ColorMask("Color mask",Float)=15
        _StencilComp("Stencil comparison",Float)=6
        _StencilPass("Stencil operation",Float)=0
        _ZTest("Depth comparison",Float)=4
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+20"}
        Pass
        {
            Tags {"LightMode"="SRPDefaultUnlit"}
            Cull [_Cull] ZWrite Off ZTest [_ZTest] ColorMask [_ColorMask]
            Stencil {Ref 221 Comp [_StencilComp] Pass [_StencilPass]}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 vertex:POSITION;float3 normal:NORMAL;};
            struct V {float4 position:SV_POSITION;};
            CBUFFER_START(UnityPerMaterial)
            float4 _Color; float _Pixels;
            CBUFFER_END
            V vert(A i)
            {
                V o;float3 world=TransformObjectToWorld(i.vertex.xyz);
                float4 clip=TransformWorldToHClip(world);
                float4 tip=TransformWorldToHClip(world+TransformObjectToWorldNormal(i.normal));
                float2 dir=tip.xy/max(.001,tip.w)-clip.xy/max(.001,clip.w);
                dir/=max(length(dir),.00001);
                clip.xy+=dir*(_Pixels*2/_ScreenParams.xy)*clip.w;o.position=clip;return o;
            }
            half4 frag(V i):SV_Target{return _Color;}
            ENDHLSL
        }
    }
}
