Shader "Unlit/PackRGB"
{
    Properties
    {
        _TexR ("Texture", 2D) = "red" {}
        _TexG ("Texture", 2D) = "green" {}
        _TexB ("Texture", 2D) = "blue" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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

            sampler2D _TexR;
            sampler2D _TexG;
            sampler2D _TexB;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = fixed4(1, 1, 1, 1);
                col.r = tex2D(_TexR, i.uv).r;
                col.g = tex2D(_TexG, i.uv).g;
                col.b = tex2D(_TexB, i.uv).b;
                return col;
            }
            ENDCG
        }
    }
}
