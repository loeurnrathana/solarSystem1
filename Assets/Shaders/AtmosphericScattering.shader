Shader "SolarSystem/AtmosphericScattering"
{
    Properties
    {
        _AtmosphereColor ("Atmosphere Base Color", Color) = (0.3, 0.6, 1.0, 1.0)
        _RayleighCoeff ("Rayleigh RGB Coefficients", Vector) = (0.15, 0.45, 1.0, 1.0)
        _SunDir ("Sun Light Direction", Vector) = (1.0, 0.2, 0.5, 0.0)
        _AtmosphereThickness ("Atmosphere Thickness Multiplier", Range(0.1, 5.0)) = 1.2
        _DensityFalloff ("Density Falloff Power", Range(0.5, 10.0)) = 3.5
        _MieG ("Mie Forward Scattering (G)", Range(0.0, 0.99)) = 0.76
        _GlowIntensity ("Atmosphere Glow Intensity", Range(0.1, 10.0)) = 2.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "IgnoreProjector"="True" }
        LOD 100

        Blend SrcAlpha One
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            fixed4 _AtmosphereColor;
            float4 _RayleighCoeff;
            float4 _SunDir;
            float _AtmosphereThickness;
            float _DensityFalloff;
            float _MieG;
            float _GlowIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 N = normalize(i.worldNormal);
                float3 lightDir = normalize(_SunDir.xyz);

                // 1. Rayleigh Limb Scattering (Fresnel edge glow based on view angle)
                float NdotV = saturate(dot(N, viewDir));
                float limbGlow = pow(1.0 - NdotV, _DensityFalloff);

                // 2. Solar Illumination / Day-Night Phase (Sun direction facing factor)
                float NdotL = dot(N, lightDir);
                float dayFactor = saturate(NdotL * 0.5 + 0.5); // Soft terminator transition

                // 3. Mie Forward Scattering (Solar halo brightness when looking towards Sun)
                float cosTheta = dot(-viewDir, lightDir);
                float g = _MieG;
                float miePhase = (1.0 - g * g) / (4.0 * 3.14159265 * pow(abs(1.0 + g * g - 2.0 * g * cosTheta), 1.5));

                // 4. Combine Rayleigh dispersion colors with Mie solar highlight
                float3 rayleighColor = _AtmosphereColor.rgb * _RayleighCoeff.rgb * limbGlow * _GlowIntensity;
                float3 mieColor = _AtmosphereColor.rgb * miePhase * 0.15;

                float3 finalColor = (rayleighColor * dayFactor + mieColor * saturate(NdotL + 0.2));

                // Alpha falloff towards center sphere so planet surface remains clearly visible
                float alpha = saturate(limbGlow * _GlowIntensity * dayFactor);

                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}
