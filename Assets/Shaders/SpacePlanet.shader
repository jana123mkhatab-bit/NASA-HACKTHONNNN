// Planet surface: day/night terminator, city lights, ocean specular, limb haze and close-range fractal detail.
Shader "Space/Planet"
{
    Properties
    {
        _MainTex ("Day", 2D) = "white" {}
        _NightTex ("Night Lights", 2D) = "black" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _SunDir ("Sun Dir (toward sun)", Vector) = (1,0,0,0)
        _SunColor ("Sun Color", Color) = (1,0.97,0.92,1)
        _SunIntensity ("Sun Intensity", Float) = 1.35
        _NightStrength ("Night Lights", Float) = 1.8
        _Ambient ("Ambient", Float) = 0.012
        _Spec ("Ocean Specular", Float) = 0.9
        _Detail ("Detail Strength", Float) = 0.35
        _DetailScale ("Detail Scale", Float) = 700
        _DetailFar ("Detail Fade Distance", Float) = 450
        _Haze ("Limb Haze", Float) = 0
        _Contrast ("Contrast", Float) = 1.08
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NightTex); SAMPLER(sampler_NightTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; float4 _Tint; float4 _SunDir; float4 _SunColor;
                float _SunIntensity, _NightStrength, _Ambient, _Spec, _Detail, _DetailScale, _DetailFar, _Haze, _Contrast;
            CBUFFER_END

            struct A { float4 pos : POSITION; float3 n : NORMAL; float2 uv : TEXCOORD0; };
            struct V { float4 cs : SV_POSITION; float3 ws : TEXCOORD0; float3 n : TEXCOORD1; float2 uv : TEXCOORD2; float3 os : TEXCOORD3; };

            V vert(A i)
            {
                V o;
                o.cs = TransformObjectToHClip(i.pos.xyz);
                o.ws = TransformObjectToWorld(i.pos.xyz);
                o.n = TransformObjectToWorldNormal(i.n);
                o.uv = i.uv;
                o.os = normalize(i.pos.xyz);
                return o;
            }

            float hash13(float3 p) { p = frac(p * 0.1031); p += dot(p, p.zyx + 31.32); return frac((p.x + p.y) * p.z); }
            float vnoise(float3 p)
            {
                float3 i = floor(p); float3 f = frac(p); f = f * f * (3.0 - 2.0 * f);
                float a = lerp(lerp(hash13(i), hash13(i + float3(1,0,0)), f.x), lerp(hash13(i + float3(0,1,0)), hash13(i + float3(1,1,0)), f.x), f.y);
                float b = lerp(lerp(hash13(i + float3(0,0,1)), hash13(i + float3(1,0,1)), f.x), lerp(hash13(i + float3(0,1,1)), hash13(i + float3(1,1,1)), f.x), f.y);
                return lerp(a, b, f.z);
            }

            half4 frag(V i) : SV_Target
            {
                float3 N = normalize(i.n);
                float3 L = normalize(_SunDir.xyz);
                float3 Vd = normalize(_WorldSpaceCameraPos - i.ws);
                float dist = distance(_WorldSpaceCameraPos, i.ws);

                float3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb * _Tint.rgb;
                float k = saturate(1.0 - dist / _DetailFar);
                float n = vnoise(i.os * _DetailScale) * 0.5 + vnoise(i.os * _DetailScale * 2.7) * 0.3 + vnoise(i.os * _DetailScale * 7.3) * 0.2;
                albedo *= 1.0 + (n - 0.5) * 2.0 * _Detail * k;
                albedo = saturate(lerp(0.5, albedo, _Contrast));

                float ndl = dot(N, L);
                float lit = saturate(ndl);
                float soft = smoothstep(-0.06, 0.2, ndl);
                float3 col = albedo * (_SunColor.rgb * _SunIntensity * lit * soft + _Ambient);

                // city lights on the dark side
                float nightMask = 1.0 - smoothstep(-0.12, 0.08, ndl);
                float3 nl = SAMPLE_TEXTURE2D(_NightTex, sampler_NightTex, i.uv).rgb;
                col += nl * nl * _NightStrength * nightMask * float3(1.0, 0.82, 0.55);

                // ocean glint (blue dominant pixels)
                float ocean = saturate((albedo.b - max(albedo.r, albedo.g) * 1.15) * 7.0);
                float3 H = normalize(L + Vd);
                float spec = pow(saturate(dot(N, H)), 140.0) * ocean * saturate(ndl * 4.0);
                col += _SunColor.rgb * spec * _Spec * 2.0;

                // thin-atmosphere limb haze
                float fres = pow(1.0 - saturate(dot(N, Vd)), 3.0);
                col += float3(0.25, 0.5, 1.0) * fres * _Haze * saturate(ndl * 1.4 + 0.25);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
