Shader "Feu Bleu/Ghost Silhouette"
{
    Properties
    {
        _Tint ("Ghost tint", Color) = (0.58, 0.82, 0.94, 1)
        _FarOpacity ("Distant core opacity", Range(0, 1)) = 0.07
        _NearOpacity ("Nearby core opacity", Range(0, 1)) = 0.22
        _RimOpacity ("Silhouette opacity", Range(0, 1)) = 0.16
        _RimPower ("Silhouette softness", Range(1, 8)) = 2.5
        _NearDistance ("Nearby distance (metres)", Float) = 0.75
        _FarDistance ("Distant distance (metres)", Float) = 2.5
        _Visibility ("Tracking visibility", Range(0, 1)) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth test", Float) = 4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "GhostSilhouette"
            Tags { "LightMode"="SRPDefaultUnlit" }
            // Premultiplied RGB and coverage preserve passthrough compositor alpha.
            Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            ZTest [_ZTest]
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _FarOpacity, _NearOpacity, _RimOpacity, _RimPower;
                float _NearDistance, _FarDistance, _Visibility, _ZTest;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half rim = pow(1 - saturate(dot(normalize(input.normalWS), view)), _RimPower);
                // Horizontal distance keeps crouching users visible near standing users.
                float distanceXZ = distance(GetCameraPositionWS().xz, input.positionWS.xz);
                float farDistance = max(_FarDistance, _NearDistance + 0.01);
                half nearby = 1 - smoothstep(_NearDistance, farDistance, distanceXZ);
                half alpha = saturate(lerp(_FarOpacity, _NearOpacity, nearby) + rim * _RimOpacity);
                alpha *= saturate(input.color.a) * saturate(_Visibility) * _Tint.a;
                half3 color = _Tint.rgb * lerp(0.8, 1.0, rim);
                return half4(color * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
