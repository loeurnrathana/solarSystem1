Shader "SolarSystem/SolarSystemCollage"
{
    Properties
    {
        _TexMoon ("Moon / Mercury Texture", 2D) = "white" {}
        _TexVenus ("Venus Texture", 2D) = "white" {}
        _TexEarth ("Earth Texture", 2D) = "white" {}
        _TexMars ("Mars Texture", 2D) = "white" {}
        _TexJupiter ("Jupiter Texture", 2D) = "white" {}
        _TexSaturn ("Saturn Texture", 2D) = "white" {}
        _TexUranus ("Uranus Texture", 2D) = "white" {}
        _TexNeptune ("Neptune Texture", 2D) = "white" {}
        _TexPluto ("Pluto Texture", 2D) = "white" {}
        
        _LineColor ("Slice Line Color", Color) = (0.05, 0.05, 0.05, 1.0)
        _LineWidth ("Slice Line Thickness", Range(0.001, 0.05)) = 0.008
        _NumSlices ("Number of Slices", Float) = 9.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            sampler2D _TexMoon;
            sampler2D _TexVenus;
            sampler2D _TexEarth;
            sampler2D _TexMars;
            sampler2D _TexJupiter;
            sampler2D _TexSaturn;
            sampler2D _TexUranus;
            sampler2D _TexNeptune;
            sampler2D _TexPluto;

            fixed4 _LineColor;
            float _LineWidth;
            float _NumSlices;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float vNorm = saturate(i.uv.y);
                float sliceIdx = floor(vNorm * _NumSlices);
                
                // Check if UV is close to a horizontal slice dividing line
                float fracSlice = frac(vNorm * _NumSlices);
                bool isLine = (fracSlice < _LineWidth) || (fracSlice > (1.0 - _LineWidth));

                if (isLine && vNorm > 0.02 && vNorm < 0.98)
                {
                    return _LineColor;
                }

                // Sample texture based on slice index (Top to Bottom: 8 down to 0)
                int idx = (int)sliceIdx;
                fixed4 col = fixed4(1, 1, 1, 1);

                if (idx == 8)      col = tex2D(_TexMoon, i.uv);
                else if (idx == 7) col = tex2D(_TexVenus, i.uv);
                else if (idx == 6) col = tex2D(_TexEarth, i.uv);
                else if (idx == 5) col = tex2D(_TexMars, i.uv);
                else if (idx == 4) col = tex2D(_TexJupiter, i.uv);
                else if (idx == 3) col = tex2D(_TexSaturn, i.uv);
                else if (idx == 2) col = tex2D(_TexUranus, i.uv);
                else if (idx == 1) col = tex2D(_TexNeptune, i.uv);
                else              col = tex2D(_TexPluto, i.uv);

                // Subtle directional lighting shading
                float3 N = normalize(i.worldNormal);
                float3 lightDir = normalize(float3(0.5, 0.3, 0.8));
                float diff = saturate(dot(N, lightDir)) * 0.7 + 0.35;

                return fixed4(col.rgb * diff, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
