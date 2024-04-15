Shader "Unlit/TextureMix" {
    Properties {
        _MainTex ("Texture 1", 2D) = "white" {}
        // The second texture will appear over the first, as in the Porter and Duff compositing operation.
        _SecondTex ("Texture 2", 2D) = "black" {}
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _SecondTex;
            float4 _MainTex_ST;
            float4 _SecondTex_ST;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 color1 = tex2D(_MainTex, i.uv + _MainTex_ST.xy);
                fixed4 color2 = tex2D(_SecondTex, i.uv + _SecondTex_ST.xy);

                // Porter and Duff, color2 "over" color1.
                float finalAlpha = color2.a + color1.a * (1.0 - color2.a);
                fixed3 finalColor = (color2.rgb * color2.a + color1.rgb * color1.a * (1.0 - color2.a)) / finalAlpha;
                
                return fixed4(finalColor, finalAlpha);
            }
            ENDCG
        }
    }
}
