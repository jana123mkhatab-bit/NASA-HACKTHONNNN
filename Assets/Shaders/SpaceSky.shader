// Additive star dome: procedural crisp stars + milky-way texture. Attach a sphere to the camera.
Shader "Space/Sky"
{
    Properties
    {
        _MainTex ("Milky Way", 2D) = "black" {}
        _Milky ("Milky Way Intensity", Float) = 0.55
        _Stars ("Star Intensity", Float) = 2.2
        _Dim ("Dim (0=hidden)", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-50" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; float _Milky; float _Stars; float _Dim;
            CBUFFER_END

            struct A { float4 pos : POSITION; };
            struct V { float4 cs : SV_POSITION; float3 d : TEXCOORD0; };

            V vert(A i) { V o; o.cs = TransformObjectToHClip(i.pos.xyz); o.d = i.pos.xyz; return o; }

            float3 hash33(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            float3 starLayer(float3 d, float scale, float density, float size)
            {
                float3 p = d * scale;
                float3 id = floor(p);
                float3 f = frac(p) - 0.5;
                float3 h = hash33(id);
                float3 off = (h - 0.5) * 0.7;
                float dist = length(f - off);
                float present = step(1.0 - density, hash33(id + 17.0).x);
                float bright = pow(hash33(id + 5.0).y, 3.0) * 2.5 + 0.35;
                float s = smoothstep(size * (0.6 + bright * 0.25), 0.0, dist) * present * bright;
                float t = hash33(id + 9.0).z;
                float3 tint = lerp(float3(0.65, 0.78, 1.0), float3(1.0, 0.82, 0.62), t);
                return tint * s;
            }

            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.d);
                float2 uv = float2(atan2(d.z, d.x) * 0.15915494 + 0.5, asin(clamp(d.y, -1, 1)) * 0.31830989 + 0.5);
                float3 mw = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
                mw = pow(mw, 1.6) * _Milky;
                float3 s = starLayer(d, 70.0, 0.55, 0.075) + starLayer(d, 150.0, 0.5, 0.09) + starLayer(d, 330.0, 0.45, 0.12);
                return half4((mw + s * _Stars) * _Dim, 1);
            }
            ENDHLSL
        }
    }
}
