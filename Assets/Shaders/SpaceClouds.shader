Shader "Space/Clouds"
{
    Properties
    {
        _MainTex ("Clouds", 2D) = "black" {}
        _SunDir ("Sun Dir", Vector) = (1,0,0,0)
        _Opacity ("Opacity", Float) = 0.85
        _Ambient ("Ambient", Float) = 0.01
        _SunIntensity ("Sun Intensity", Float) = 1.35
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; float4 _SunDir; float _Opacity; float _Ambient; float _SunIntensity;
            CBUFFER_END
            struct A { float4 pos : POSITION; float3 n : NORMAL; float2 uv : TEXCOORD0; };
            struct V { float4 cs : SV_POSITION; float3 n : TEXCOORD0; float2 uv : TEXCOORD1; };
            V vert(A i) { V o; o.cs = TransformObjectToHClip(i.pos.xyz); o.n = TransformObjectToWorldNormal(i.n); o.uv = i.uv; return o; }
            half4 frag(V i) : SV_Target
            {
                float c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).r;
                c = smoothstep(0.18, 0.95, c);
                float3 N = normalize(i.n); float3 L = normalize(_SunDir.xyz);
                float ndl = dot(N, L);
                float lit = saturate(ndl) * smoothstep(-0.08, 0.22, ndl);
                float3 col = float3(1,1,1) * (lit * _SunIntensity + _Ambient);
                col = lerp(col, col * float3(1.0, 0.72, 0.55), saturate(1.0 - abs(ndl) * 6.0) * 0.6); // sunset tint at the terminator
                return half4(col, saturate(c * _Opacity));
            }
            ENDHLSL
        }
    }
}
