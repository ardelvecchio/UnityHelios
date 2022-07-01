Shader "Unlit/GradientShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color1("Color", Color) = (0,0,1,1)
        _Color2("Color", Color) = (0,1,1,1)
        _Color3("Color", Color) = (0,1,0,1)
        _Color4("Color", Color) = (1, 0.92, 0.016, 1)
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            float4 _Color4;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 result = _Color1;
                if (i.uv.x < 0.33)
                {
                    result = lerp(_Color1, _Color2, i.uv.x*3);
                }
                if (i.uv.x > 0.33 && i.uv.x < 0.66)
                {
                    result = lerp(_Color2, _Color3, (i.uv.x-0.33)*3);
                }
                if (i.uv.x > 0.66)
                {
                    result = lerp(_Color3, _Color4, (i.uv.x-0.66)*3);
                }
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return result;
            }
            ENDCG
        }
    }
}
