Shader "SecretRoom/MonitorScreen"
{
    Properties
    {
        _BaseMap("Screenshot (16:9)",2D)="white"{}
        [HDR] _BaseColor("Screen tint",Color)=(.86,.92,1,1)
        _EmissionStrength("Brightness",Float)=1.5
        _HasImage("Image assigned",Float)=0
    }
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Tags{"LightMode"="SRPDefaultUnlit"}
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;float4 _BaseColor;float _EmissionStrength;float _HasImage;
            CBUFFER_END
            struct Input{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct Output{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            Output Vert(Input input){Output o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.uv=TRANSFORM_TEX(input.uv,_BaseMap);return o;}
            half4 Frag(Output input):SV_Target
            {
                half3 image=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb;
                float scan=.96+.04*cos(input.uv.y*270*6.2831853);
                float gridX=step(.985,frac(input.uv.x*16)),gridY=step(.98,frac(input.uv.y*9));
                half3 placeholder=(.66+.1*input.uv.y)*(1-.07*max(gridX,gridY));
                return half4(lerp(placeholder,image,saturate(_HasImage))*scan*_BaseColor.rgb*_EmissionStrength,1);
            }
            ENDHLSL
        }
    }
}
