Shader "Ancient Future/Remote Living Water"
{
    Properties
    {
        _Visibility ("Tracking visibility", Range(0,1)) = 1
        _DeepColor ("Water tint", Color) = (0.025, 0.22, 0.3, 1)
        _RimColor ("Silver blue reflection", Color) = (0.55, 0.9, 1, 1)
        _Opacity ("Core opacity", Range(0.02,0.7)) = 0.14
        _RimStrength ("Reflection strength", Range(0,2)) = 0.85
        _FlowScale ("Flow detail", Range(2,40)) = 18
        _FlowSpeed ("Flow speed", Range(0,2)) = 0.32
        _FlowBrightness ("Flow highlights", Range(0,3)) = 1.2
        _Displacement ("Surface ripple (metres)", Range(0,0.02)) = 0.002
        _EdgePower ("Fresnel power", Range(1,8)) = 3.8
        _FlowTex ("Seamless flow field", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "LivingWater"
            Tags { "LightMode"="UniversalForward" }
            // Premultiplied color AND coverage preserve compositor alpha for MR underlays.
            Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _DeepColor;
            half4 _RimColor;
            float _Opacity, _RimStrength, _FlowScale, _FlowSpeed;
            float _FlowBrightness, _Displacement, _EdgePower, _Visibility;
            CBUFFER_END
            TEXTURE2D(_FlowTex);
            SAMPLER(sampler_FlowTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float3 flowPosition : TEXCOORD2;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 p = v.positionOS.xyz;
                float t = _Time.y * _FlowSpeed;
                float wave = sin(p.y * 34 + p.x * 17 + t * 3) * sin(p.z * 27 - p.y * 18 - t * 2);
                p += v.normalOS * wave * _Displacement;
                VertexPositionInputs position = GetVertexPositionInputs(p);
                o.positionCS = position.positionCS;
                o.positionWS = position.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.flowPosition = v.positionOS.xyz;
                o.color = v.color;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float t = _Time.y * _FlowSpeed;
                float3 p = i.flowPosition * _FlowScale;
                p.x += 0.8 * sin(p.y * 0.65 - t * 1.7) + 0.5 * sin(p.z * 1.3 + t);
                p.z += 0.65 * sin(p.y * 0.8 + p.x * 0.4 - t);
                half3 normal = normalize(i.normalWS);
                half4 flowXY = SAMPLE_TEXTURE2D(_FlowTex, sampler_FlowTex, p.xy * float2(0.13,0.055) + float2(t * 0.02,-t * 0.06));
                half4 flowYZ = SAMPLE_TEXTURE2D(_FlowTex, sampler_FlowTex, p.yz * float2(0.065,0.13) + float2(-t * 0.05,t * 0.025) + 0.31);
                half4 flowZX = SAMPLE_TEXTURE2D(_FlowTex, sampler_FlowTex, p.zx * 0.11 + float2(t * 0.025,-t * 0.035) + 0.67);
                half field = flowXY.b * 0.55 + flowYZ.b * 0.30 + flowZX.b * 0.15;
                half fine = pow(saturate(1 - abs(field - 0.50) * 14), 10);
                half secondary = pow(saturate(1 - abs(field - 0.62) * 16), 14);
                half3 ripple = half3(flowXY.r + flowZX.g - 1, flowXY.g + flowYZ.r - 1, flowYZ.g + flowZX.r - 1);
                normal = normalize(normal + ripple * 0.68);
                half3 view = GetWorldSpaceNormalizeViewDir(i.positionWS);
                half fresnel = pow(1.0 - saturate(dot(normal, view)), _EdgePower);
                half3 reflected = reflect(-view, normal);
                // Broad studio-like reflections stay visible without a scene-color or passthrough texture.
                half sky = smoothstep(-0.35, 0.8, reflected.y);
                half strip = pow(saturate(dot(reflected, normalize(half3(-0.5, 0.65, -0.4)))), 28.0);
                half strip2 = pow(saturate(dot(reflected, normalize(half3(0.8, 0.25, 0.35)))), 45.0);
                Light light = GetMainLight();
                half specular = pow(saturate(dot(normal, normalize(light.direction + view))), 100.0);
                half flow = (fine * 0.85 + secondary * 0.4) * _FlowBrightness;
                half alpha = saturate(_Opacity + fresnel * 0.65 + flow * 0.38 + (strip + specular) * 0.22);
                alpha *= i.color.a * _Visibility;
                half3 color = _DeepColor.rgb * (0.65 + sky * 0.6);
                color += _RimColor.rgb * (fresnel * _RimStrength + flow * 1.1 + strip * 1.8 + strip2 * 0.8);
                color += light.color * specular * 1.2;
                return half4(color * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
