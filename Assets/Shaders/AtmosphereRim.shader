Shader "SolarSystem/AtmosphereRim"
{
    Properties
    {
        _AtmosphereColor ("Atmosphere Color", Color) = (0.2, 0.6, 1.0, 1.0)
        _RimPower ("Rim Power", Range(0.5, 10.0)) = 3.0
        _SunIntensity ("Sun Scattering Intensity", Range(0.0, 5.0)) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha One // Additive atmosphere glow

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
            };

            float4 _AtmosphereColor;
            float _RimPower;
            float _SunIntensity;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewWS = normalize(_WorldSpaceCameraPos - worldPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float3 N = normalize(i.normalWS);
                float3 V = normalize(i.viewWS);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);

                // Fresnel Rim computation
                float fresnel = 1.0 - saturate(dot(N, V));
                float rim = pow(fresnel, _RimPower);

                // Sun illumination masking (Day vs Night side atmosphere glow)
                float sunFactor = saturate(dot(N, L));

                float3 color = _AtmosphereColor.rgb * rim * (sunFactor * _SunIntensity + 0.1);
                float alpha = rim * (sunFactor * 0.8 + 0.2);

                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
}
