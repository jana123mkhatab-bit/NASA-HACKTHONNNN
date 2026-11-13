Shader "LastSignal/Clouds"
{
    Properties
    {
        _CloudMap ("Cloud map", 2D) = "white" {}
        _Opacity ("Opacity", Range(0, 1)) = 0.85
        _Ambient ("Ambient (space)", Range(0, 0.3)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_CloudMap); SAMPLER(sampler_CloudMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _CloudMap_ST;
                half _Opacity;
                half _Ambient;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = TRANSFORM_TEX(i.uv, _CloudMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                Light l = GetMainLight();
                float ndl = dot(normalize(i.normalWS), l.direction);
                half3 c = SAMPLE_TEXTURE2D(_CloudMap, sampler_CloudMap, i.uv).rgb;
                half density = saturate(max(c.r, max(c.g, c.b)));
                half3 col = half3(1, 1, 1) * (saturate(ndl) * l.color + _Ambient);
                return half4(col, density * _Opacity);
            }
            ENDHLSL
        }
    }
}
