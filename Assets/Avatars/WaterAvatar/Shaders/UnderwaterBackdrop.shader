Shader "Ancient Future/Underwater Backdrop"
{
    Properties { _Color ("Tint", Color) = (0.01,0.12,0.19,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A v) { V o=(V)0; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv; return o; }
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv=i.uv;
                float t=_Time.y*0.05;
                float glow=exp(-length((uv-float2(0.48,0.82))*float2(2.8,1.5))*2.5);
                float rays=pow(saturate(sin((uv.x/(uv.y+0.45))*55+t+sin(uv.y*5+t))),16)*0.12;
                float caustic=pow(saturate(1-abs(sin(uv.x*44+sin(uv.y*29+t))*0.55+sin(uv.y*37+sin(uv.x*21-t))*0.45)),16);
                half3 c=lerp(half3(0.002,0.012,0.025),_Color.rgb,saturate(uv.y*0.7+glow));
                c+=half3(0.06,0.23,0.30)*(rays+glow*0.5+caustic*pow(uv.y,8)*0.65);
                return half4(c,1);
            }
            ENDHLSL
        }
    }
}
