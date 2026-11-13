// Vertex-coloured terrain with procedural micro detail, main-light shadows and fog (URP).
Shader "Space/Terrain"
{
    Properties
    {
        _Detail ("Detail", Float) = 0.5
        _Bump ("Bump", Float) = 0.6
        _Scale ("Detail Scale", Float) = 0.7
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Detail; float _Bump; float _Scale;
            CBUFFER_END
            struct A { float4 pos : POSITION; float3 n : NORMAL; float4 col : COLOR; };
            struct V { float4 cs : SV_POSITION; float3 ws : TEXCOORD0; float3 n : TEXCOORD1; float4 col : COLOR; float fog : TEXCOORD2; };
            V vert(A i)
            {
                V o; o.cs = TransformObjectToHClip(i.pos.xyz); o.ws = TransformObjectToWorld(i.pos.xyz);
                o.n = TransformObjectToWorldNormal(i.n); o.col = i.col; o.fog = ComputeFogFactor(o.cs.z); return o;
            }
            float h3(float3 p) { p = frac(p * 0.1031); p += dot(p, p.zyx + 31.32); return frac((p.x + p.y) * p.z); }
            float vn(float3 p)
            {
                float3 i = floor(p); float3 f = frac(p); f = f * f * (3.0 - 2.0 * f);
                float a = lerp(lerp(h3(i), h3(i + float3(1,0,0)), f.x), lerp(h3(i + float3(0,1,0)), h3(i + float3(1,1,0)), f.x), f.y);
                float b = lerp(lerp(h3(i + float3(0,0,1)), h3(i + float3(1,0,1)), f.x), lerp(h3(i + float3(0,1,1)), h3(i + float3(1,1,1)), f.x), f.y);
                return lerp(a, b, f.z);
            }
            float fbm(float3 p) { return vn(p) * 0.55 + vn(p * 2.3) * 0.3 + vn(p * 5.7) * 0.15; }
            half4 frag(V i) : SV_Target
            {
                float3 p = i.ws * _Scale;
                float e = 0.12;
                float c0 = fbm(p);
                float cx = fbm(p + float3(e, 0, 0)); float cz = fbm(p + float3(0, 0, e));
                float3 N = normalize(i.n);
                N = normalize(N + float3(c0 - cx, 0, c0 - cz) * _Bump * 6.0);
                float3 albedo = i.col.rgb * (1.0 - _Detail * 0.5 + _Detail * c0 * 1.1);
                albedo *= 0.92 + 0.16 * vn(i.ws * 9.0);
                float4 sc = TransformWorldToShadowCoord(i.ws);
                Light L = GetMainLight(sc);
                float3 col = albedo * (L.color * L.distanceAttenuation * L.shadowAttenuation * saturate(dot(N, L.direction)) + SampleSH(N));
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
