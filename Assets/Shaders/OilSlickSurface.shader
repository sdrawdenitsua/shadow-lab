// Shadow Lab — Oil Slick / Mineral Oil Surface Shader
// Used on the PC tank floor and oil puddles on the workbench

Shader "ShadowLab/OilSlickSurface"
{
    Properties
    {
        _BaseColor      ("Base Color",       Color)   = (0.02, 0.01, 0.03, 1)
        _OilTex         ("Oil Noise Texture",2D)      = "white" {}
        _ReflectionCube ("Reflection Cubemap",Cube)   = "_Skybox" {}
        _Smoothness     ("Smoothness",       Range(0,1)) = 0.98
        _IridescenceStr ("Iridescence",      Range(0,1)) = 0.6
        _ScrollSpeed    ("Oil Scroll Speed", Float)   = 0.02
        _Depth          ("Oil Depth Tint",   Color)   = (0.04, 0, 0.08, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }

        Pass
        {
            Name "OilSurface"
            Blend SrcAlpha OneMinusSrcAlpha
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 pos : POSITION; float2 uv : TEXCOORD0; float3 normal : NORMAL; };
            struct Varyings   { float4 hcs : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 viewDir : TEXCOORD2; };

            TEXTURE2D(_OilTex); SAMPLER(sampler_OilTex);
            TEXTURECUBE(_ReflectionCube); SAMPLER(sampler_ReflectionCube);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _Depth;
                float  _Smoothness, _IridescenceStr, _ScrollSpeed;
                float4 _OilTex_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.hcs      = TransformObjectToHClip(IN.pos.xyz);
                OUT.uv       = TRANSFORM_TEX(IN.uv, _OilTex);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normal);
                OUT.viewDir  = GetWorldSpaceNormalizeViewDir(TransformObjectToWorld(IN.pos.xyz));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 scrolledUV = IN.uv + float2(_ScrollSpeed, _ScrollSpeed * 0.7) * _Time.y;
                half   noise      = SAMPLE_TEXTURE2D(_OilTex, sampler_OilTex, scrolledUV).r;

                // Iridescent rainbow from view angle
                float  ndotv   = dot(IN.normalWS, IN.viewDir);
                float3 iridescent = float3(
                    sin(ndotv * 6.28 + noise * 4.0 + 0.0) * 0.5 + 0.5,
                    sin(ndotv * 6.28 + noise * 4.0 + 2.1) * 0.5 + 0.5,
                    sin(ndotv * 6.28 + noise * 4.0 + 4.2) * 0.5 + 0.5
                );

                // Cubemap reflection
                float3 reflDir = reflect(-IN.viewDir, IN.normalWS);
                half3  refl    = SAMPLE_TEXTURECUBE(_ReflectionCube, sampler_ReflectionCube, reflDir).rgb;

                half3 col = lerp(_BaseColor.rgb, _Depth.rgb, noise * 0.5);
                col = lerp(col, iridescent, _IridescenceStr * (1.0 - ndotv));
                col = lerp(col, refl, _Smoothness * 0.7);

                return half4(col, 0.92);
            }
            ENDHLSL
        }
    }
}
