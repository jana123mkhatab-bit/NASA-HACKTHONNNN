Shader "LastSignal/Earth"
{
    Properties
    {
        _DayMap ("Day map (NASA Blue Marble)", 2D) = "white" {}
        _NightMap ("Night map (city lights)", 2D) = "black" {}
        _NightIntensity ("Night lights intensity", Range(0, 6)) = 2.5
        _SpecIntensity ("Ocean glint", Range(0, 3)) = 1.2
        _Ambient ("Ambient (space)", Range(0, 0.3)) = 0.03
        _TerminatorTint ("Twilight tint", Color) = (1.0, 0.45, 0.2, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_DayMap);   SAMPLER(sampler_DayMap);
            TEXTURE2D(_NightMap); SAMPLER(sampler_NightMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _DayMap_ST;
                float4 _NightMap_ST;
                half _NightIntensity;
                half _SpecIntensity;
                half _Ambient;
                half4 _TerminatorTint;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; float2 uv : TEXCOORD2; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.viewWS = GetWorldSpaceViewDir(p.positionWS);
                o.uv = TRANSFORM_TEX(i.uv, _DayMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(i.viewWS);
                Light l = GetMainLight();
                float ndl = dot(n, l.direction);
                float dayAmt = smoothstep(-0.10, 0.20, ndl);

                half3 day = SAMPLE_TEXTURE2D(_DayMap, sampler_DayMap, i.uv).rgb;
                half3 night = SAMPLE_TEXTURE2D(_NightMap, sampler_NightMap, i.uv).rgb;

                half3 lit = day * (saturate(ndl) * l.color + _Ambient);
                // Warm band along the terminator
                float twilight = saturate(1.0 - abs(ndl) * 6.0) * 0.18;
                lit += day * _TerminatorTint.rgb * twilight;

                // Ocean glint: oceans are blue-dominant in the day map
                half ocean = saturate((day.b - max(day.r * 1.4, day.g * 0.9)) * 5.0);
                float3 h = normalize(l.direction + v);
                half spec = pow(saturate(dot(n, h)), 140.0) * ocean * _SpecIntensity * saturate(ndl * 4.0);

                half3 lights = night * _NightIntensity * (1.0 - dayAmt);
                return half4(lit + spec * l.color + lights, 1.0);
            }
            ENDHLSL
        }
    }
}
