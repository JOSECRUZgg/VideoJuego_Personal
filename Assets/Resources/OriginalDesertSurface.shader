Shader "Game/OriginalDesertSurface"
{
    Properties
    {
        _MainTex("Arena original", 2D) = "white" {}
        _MainTexSide("Roca original", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _BaseScale("Escala arena", Float) = 0.04
        _SideScale("Escala roca", Float) = 0.04
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_MainTexSide); SAMPLER(sampler_MainTexSide);
            CBUFFER_START(UnityPerMaterial)
            float4 _Color; float _BaseScale; float _SideScale;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; };
            V vert(A v) { V o; o.world=TransformObjectToWorld(v.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world); o.normal=TransformObjectToWorldNormal(v.normalOS); return o; }
            half4 frag(V i):SV_Target
            {
                float3 n=normalize(i.normal), weights=pow(abs(n),4); weights/=max(dot(weights,1),.001);
                half3 top=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.world.xz*_BaseScale).rgb;
                half3 sideX=SAMPLE_TEXTURE2D(_MainTexSide,sampler_MainTexSide,i.world.zy*_SideScale).rgb;
                half3 sideZ=SAMPLE_TEXTURE2D(_MainTexSide,sampler_MainTexSide,i.world.xy*_SideScale).rgb;
                Light light=GetMainLight(TransformWorldToShadowCoord(i.world));
                half3 diffuse=SampleSH(n)+light.color*saturate(dot(n,light.direction))*light.shadowAttenuation;
                return half4((top*weights.y+sideX*weights.x+sideZ*weights.z)*_Color.rgb*diffuse,1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
