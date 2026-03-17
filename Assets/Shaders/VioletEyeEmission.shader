// Shadow Lab — Violet Eye Emission Shader
// URP Lit-based shader with dynamic emission controlled via C# (VioletEyeEmitter.cs)
// Assign to Nova's eye mesh material

Shader "ShadowLab/VioletEyeEmission"
{
    Properties
    {
        _BaseColor      ("Base Color",      Color)  = (0.05, 0, 0.1, 1)
        _EmissionColor  ("Emission Color",  Color)  = (0.54, 0, 1, 1)
        _EmissionIntensity ("Intensity",    Float)  = 1.5
        _IrisTex        ("Iris Texture",    2D)     = "white" {}
        _PupilSize      ("Pupil Size",      Range(0.1, 0.9)) = 0.35
        _FresnelPower   ("Fresnel Power",   Float)  = 2.0
        _Smoothness     ("Smoothness",      Range(0, 1)) = 0.95
        _Metallic       ("Metallic",        Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            TEXTURE2D(_IrisTex); SAMPLER(sampler_IrisTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float  _EmissionIntensity;
                float  _PupilSize;
                float  _FresnelPower;
                float  _Smoothness;
                float  _Metallic;
                float4 _IrisTex_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _IrisTex);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS   = GetWorldSpaceNormalizeViewDir(TransformObjectToWorld(IN.positionOS.xyz));
                OUT.fogFactor   = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv     = IN.uv - 0.5;
                float  dist   = length(uv);
                float  pupil  = step(dist, _PupilSize * 0.15);
                float  iris   = 1.0 - smoothstep(0.28, 0.5, dist);

                half4  irisTex  = SAMPLE_TEXTURE2D(_IrisTex, sampler_IrisTex, IN.uv);
                half3  baseCol  = _BaseColor.rgb * iris * irisTex.rgb;

                // Fresnel rim
                float fresnel = pow(1.0 - saturate(dot(IN.normalWS, IN.viewDirWS)), _FresnelPower);

                // Emission — driven by C# at runtime
                half3 emission = _EmissionColor.rgb * _EmissionIntensity * (iris + fresnel * 0.5);
                emission      += _EmissionColor.rgb * pupil * _EmissionIntensity * 2.0; // bright pupil

                half3 finalCol = baseCol + emission;
                finalCol = MixFog(finalCol, IN.fogFactor);

                return half4(finalCol, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
