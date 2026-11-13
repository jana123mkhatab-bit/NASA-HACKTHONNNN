Shader "Space/SunSurface"
{
    Properties
    {
        _MainTex ("Sun", 2D) = "white" {}
        [HDR] _Color ("HDR Color", Color) = (2.6,1.7,0.8,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; float4 _Color;
            CBUFFER_END
            struct A { float4 pos : POSITION; float3 n : NORMAL; float2 uv : TEXCOORD0; };
            struct V { float4 cs : SV_POSITION; float3 n : TEXCOORD0; float3 ws : TEXCOORD1; float2 uv : TEXCOORD2; };
            V vert(A i) { V o; o.cs = TransformObjectToHClip(i.pos.xyz); o.n = TransformObjectToWorldNormal(i.n); o.ws = TransformObjectToWorld(i.pos.xyz); o.uv = i.uv; return o; }
            half4 frag(V i) : SV_Target
            {
                float t = _Time.y * 0.012;
                float3 a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(t, 0)).rgb;
                float3 b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv * 1.7 + float2(-t * 1.3, t * 0.7)).rgb;
                float3 tex = lerp(a, b, 0.4);
                float3 N = normalize(i.n); float3 Vd = normalize(_WorldSpaceCameraPos - i.ws);
                float mu = saturate(dot(N, Vd));
                float limb = 0.55 + 0.45 * pow(mu, 0.45);
                float3 c = (tex * 0.9 + 0.25) * _Color.rgb * limb;
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
