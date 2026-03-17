// ============================================================
// Shadow Lab — Violet Eye Emission Shader (URP Lit-based)
// ============================================================
// Features:
//   • Custom Fresnel rim driven by _FresnelPower + _FresnelColor
//   • _EmissionColor controlled at runtime by VioletEyeEmitter.cs
//   • Iris radial mask with adjustable pupil size
//   • Subsurface scattering approx (inner glow)
//   • Compatible with URP 14 (Unity 2022.3 LTS)
// ============================================================

Shader "ShadowLab/VioletEyeEmission"
{
    Properties
    {
        // ── Surface ──────────────────────────────────────────────────
        _BaseColor          ("Base (Iris) Color",       Color)       = (0.04, 0.00, 0.10, 1)
        _BaseMap            ("Iris Texture",             2D)          = "white" {}
        _Smoothness         ("Smoothness",               Range(0,1))  = 0.96
        _Metallic           ("Metallic",                 Range(0,1))  = 0.05

        // ── Emission (set by VioletEyeEmitter.cs) ────────────────────
        [HDR]
        _EmissionColor      ("Emission Color",           Color)       = (0.54, 0.0, 1.0, 1)

        // ── Iris / Pupil ──────────────────────────────────────────────
        _IrisRadius         ("Iris Radius",              Range(0.1, 0.5))  = 0.42
        _PupilRadius        ("Pupil Radius",             Range(0.0, 0.4))  = 0.14
        _PupilDarkness      ("Pupil Darkness",           Range(0,1))       = 0.92
        _IrisEdgeSoftness   ("Iris Edge Softness",       Range(0.001,0.1)) = 0.015

        // ── Fresnel ───────────────────────────────────────────────────
        [HDR]
        _FresnelColor       ("Fresnel Rim Color",        Color)       = (0.60, 0.10, 1.0, 1)
        _FresnelPower       ("Fresnel Power",            Range(0.5, 8))   = 2.8
        _FresnelIntensity   ("Fresnel Intensity",        Range(0,3))      = 1.2

        // ── Inner Glow (SSS approx) ───────────────────────────────────
        _InnerGlowRadius    ("Inner Glow Radius",        Range(0,0.5))    = 0.22
        _InnerGlowIntensity ("Inner Glow Intensity",     Range(0,2))      = 0.6
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ── Vertex input / output ─────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
                float3 positionWS  : TEXCOORD3;
                float  fogFactor   : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO          // Required for Quest stereo rendering
            };

            // ── Textures & Samplers ───────────────────────────────────
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            // ── CBUFFER ───────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float4 _FresnelColor;
                float4 _BaseMap_ST;
                float  _Smoothness;
                float  _Metallic;
                float  _IrisRadius;
                float  _PupilRadius;
                float  _PupilDarkness;
                float  _IrisEdgeSoftness;
                float  _FresnelPower;
                float  _FresnelIntensity;
                float  _InnerGlowRadius;
                float  _InnerGlowIntensity;
            CBUFFER_END

            // ── Vertex shader ─────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vni = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = vpi.positionCS;
                OUT.positionWS  = vpi.positionWS;
                OUT.normalWS    = vni.normalWS;
                OUT.viewDirWS   = GetWorldSpaceNormalizeViewDir(vpi.positionWS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor   = ComputeFogFactor(vpi.positionCS.z);
                return OUT;
            }

            // ── Fragment shader ───────────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                // ── Iris/Pupil mask ───────────────────────────────────
                float2 centeredUV = IN.uv - 0.5;
                float  dist       = length(centeredUV);

                // Soft iris boundary
                float irisMask = 1.0 - smoothstep(
                    _IrisRadius - _IrisEdgeSoftness,
                    _IrisRadius + _IrisEdgeSoftness,
                    dist);

                // Sharp pupil
                float pupilMask = smoothstep(_PupilRadius - 0.005, _PupilRadius + 0.005, dist);

                // ── Base colour ───────────────────────────────────────
                half4  texSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half3  baseCol   = _BaseColor.rgb * texSample.rgb * irisMask;

                // Darken pupil
                baseCol *= lerp(1.0 - _PupilDarkness, 1.0, pupilMask);

                // ── Lighting ──────────────────────────────────────────
                InputData lightInput;
                ZERO_INITIALIZE(InputData, lightInput);
                lightInput.positionWS     = IN.positionWS;
                lightInput.normalWS       = normalize(IN.normalWS);
                lightInput.viewDirectionWS= IN.viewDirWS;
                lightInput.shadowCoord    = TransformWorldToShadowCoord(IN.positionWS);

                SurfaceData surface;
                ZERO_INITIALIZE(SurfaceData, surface);
                surface.albedo       = baseCol;
                surface.metallic     = _Metallic;
                surface.smoothness   = _Smoothness;
                surface.occlusion    = 1.0;
                surface.alpha        = 1.0;
                // Emission injected below after PBR

                half4 pbr = UniversalFragmentPBR(lightInput, surface);

                // ── Fresnel rim ───────────────────────────────────────
                float  ndotv    = saturate(dot(normalize(IN.normalWS), IN.viewDirWS));
                float  fresnel  = pow(1.0 - ndotv, _FresnelPower);
                half3  rimGlow  = _FresnelColor.rgb * fresnel * _FresnelIntensity;

                // ── Emission (driven by VioletEyeEmitter) ────────────
                // Full on iris, extra-bright on pupil (retinal depth)
                half3 emission = _EmissionColor.rgb * irisMask;
                emission      += _EmissionColor.rgb * (1.0 - pupilMask) * 1.8; // pupil hotspot

                // ── Inner glow (SSS approximation) ───────────────────
                float innerMask  = 1.0 - smoothstep(0.0, _InnerGlowRadius, dist);
                half3 innerGlow  = _EmissionColor.rgb * innerMask * _InnerGlowIntensity;

                // ── Combine ───────────────────────────────────────────
                half3 finalCol = pbr.rgb + emission + rimGlow + innerGlow;
                finalCol = MixFog(finalCol, IN.fogFactor);

                return half4(finalCol, 1.0);
            }
            ENDHLSL
        }

        // Shadow caster pass (so Nova's eyes cast shadows)
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }

    FallBack "Universal Render Pipeline/Lit"
}
