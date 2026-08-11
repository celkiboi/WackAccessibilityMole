Shader "Custom/ColorblindFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Mode ("Colorblind Mode (0=Off, 1=Protanopia, 2=Deuteranopia, 3=Tritanopia)", Int) = 0
        _Intensity ("Intensity", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            int _Mode;
            float _Intensity;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 original = tex2D(_MainTex, i.uv);
                if (_Mode <= 0 || _Intensity <= 0.0)
                {
                    return original;
                }

                float3 col = original.rgb;

                // Mode 1: Protanopia Correction (Red-Deficiency)
                // Shifts problematic red hues towards high-contrast cyan/blue & bright yellow spectrums
                if (_Mode == 1)
                {
                    float3 protanCorrected;
                    protanCorrected.r = 0.0 * col.r + 2.02344 * col.g + -2.52581 * col.b;
                    protanCorrected.g = 0.0 * col.r + 1.0 * col.g + 0.0 * col.b;
                    protanCorrected.b = 0.0 * col.r + 0.0 * col.g + 1.0 * col.b;

                    // Preserve brightness and blend with blue boost for red objects
                    float redDominance = saturate(col.r - max(col.g, col.b));
                    float3 contrastBoost = lerp(col, float3(0.1, 0.6, 1.0), redDominance * 0.85);

                    col = lerp(col, contrastBoost, _Intensity);
                }
                // Mode 2: Deuteranopia Correction (Green-Deficiency)
                // Shifts problematic green hues towards distinct blue/magenta & gold spectrums
                else if (_Mode == 2)
                {
                    float greenDominance = saturate(col.g - max(col.r, col.b));
                    float3 contrastBoost = lerp(col, float3(1.0, 0.2, 0.8), greenDominance * 0.85);

                    col = lerp(col, contrastBoost, _Intensity);
                }
                // Mode 3: Tritanopia Correction (Blue-Yellow Deficiency)
                // Shifts blue/yellow hues towards high-contrast red/teal spectrums
                else if (_Mode == 3)
                {
                    float blueDominance = saturate(col.b - max(col.r, col.g));
                    float3 contrastBoost = lerp(col, float3(1.0, 0.4, 0.1), blueDominance * 0.85);

                    col = lerp(col, contrastBoost, _Intensity);
                }

                return fixed4(saturate(col), original.a);
            }
            ENDCG
        }
    }
}
