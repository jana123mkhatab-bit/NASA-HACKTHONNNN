// Fresnel shell: atmosphere, sun corona, canopy glass, shields. Blend set from script via _Src/_Dst.
Shader "Space/Glow"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.3,0.6,1,1)
        _Power ("Fresnel Power", Float) = 3
        _Intensity ("Intensity", Float) = 1
        _Base ("Base Alpha", Float) = 0
        _SunDir ("Sun Dir", Vector) = (1,0,0,0)
        _SunMask ("Sun Mask (0..1)", Range(0,1)) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _Src ("Src", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _Dst ("Dst", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend [_Src] [_Dst]
            ZWrite Off
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Color; float _Power; float _Intensity; float _Base; float4 _SunDir; float _SunMask;
            CBUFFER_END
            struct A { float4 pos : POSITION; float3 n : NORMAL; };
            struct V { float4 cs : SV_POSITION; float3 n : TEXCOORD0; float3 ws : TEXCOORD1; };
            V vert(A i) { V o; o.cs = TransformObjectToHClip(i.pos.xyz); o.n = TransformObjectToWorldNormal(i.n); o.ws = TransformObjectToWorld(i.pos.xyz); return o; }
            half4 frag(V i) : SV_Target
            {
                float3 N = normalize(i.n);
                float3 Vd = normalize(_WorldSpaceCameraPos - i.ws);
                float fres = pow(1.0 - saturate(abs(dot(N, Vd))), _Power);
                float sun = lerp(1.0, saturate(dot(N, normalize(_SunDir.xyz)) * 1.4 + 0.3), _SunMask);
                float a = saturate((_Base + fres) * sun);
                return half4(_Color.rgb * (_Base + fres) * _Intensity * sun, a * _Color.a);
            }
            ENDHLSL
        }
    }
}
