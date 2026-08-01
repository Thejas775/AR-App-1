// URP Lit + Meta Environment Depth occlusion.
//
// Meta only ships an *Unlit* URP occlusion shader plus an HLSL include; their
// LitOccluded shadergraph exposes nothing but a base colour. This wires the
// include into a real URP PBR pass so the Omnitrix keeps its metal/smoothness
// and emission while still being occluded by the player's actual arm.

Shader "Omnitrix/OccludedLit"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,0)
        _EmissionMap("Emission Map", 2D) = "white" {}
        _EnvironmentDepthBias("Environment Depth Bias", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // Must blend this way so occluded pixels composite correctly against passthrough.
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZWrite On
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // Enables hard/soft occlusion; EnvironmentDepthManager sets these.
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/URP/EnvironmentOcclusionURP.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                META_DEPTH_VERTEX_OUTPUT(3)
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                float4 _BaseMap_ST;
                float4 _EmissionMap_ST;
                half4  _EmissionColor;
                half   _Metallic;
                half   _Smoothness;
                float  _EnvironmentDepthBias;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   nrm = GetVertexNormalInputs(input.normalOS);

                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS   = nrm.normalWS;
                output.uv         = input.uv;   // raw; each map applies its own ST in frag

                META_DEPTH_INITIALIZE_VERTEX_OUTPUT(output, input.positionOS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uvBase = TRANSFORM_TEX(input.uv, _BaseMap);
                float2 uvEmis = TRANSFORM_TEX(input.uv, _EmissionMap);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvBase) * _BaseColor;

                SurfaceData surface = (SurfaceData)0;
                surface.albedo     = albedo.rgb;
                surface.alpha      = albedo.a;
                surface.metallic   = _Metallic;
                surface.smoothness = _Smoothness;
                surface.occlusion  = 1.0;
                surface.normalTS   = half3(0, 0, 1);
                surface.emission   = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uvEmis).rgb * _EmissionColor.rgb;

                InputData data = (InputData)0;
                data.positionWS               = input.positionWS;
                data.normalWS                 = normalize(input.normalWS);
                data.viewDirectionWS          = GetWorldSpaceNormalizeViewDir(input.positionWS);
                data.shadowCoord              = TransformWorldToShadowCoord(input.positionWS);
                data.bakedGI                  = SampleSH(data.normalWS);
                data.normalizedScreenSpaceUV  = GetNormalizedScreenSpaceUV(input.positionCS);
                data.shadowMask               = half4(1, 1, 1, 1);

                half4 color = UniversalFragmentPBR(data, surface);

                // Multiplies colour by environment visibility; fully hidden pixels are discarded.
                META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY(input, color, _EnvironmentDepthBias);

                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
