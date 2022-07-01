Shader "Unlit/PlantShader"
{
   Properties
    {
        _MainTex ("Base",   2D)    = ""{}
        _Color   ("Color",  Color) = (1, 1, 1, 1)
        _Cutoff  ("Cutoff", Float) = 0.5
        _TopColor("Color", Color) = (0,1,0,1)
        _BottomColor("Color", Color) = (1,0,1,0)
        _Blend ("Blend Value", Range(0,1))= 0
    }
    
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 100
        Cull Off
        
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
            
            float4 _Color;
            float4 _BottomColor;
            float4 _TopColor;
            
            float _Cutoff;
            
            float _Blend;


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
                fixed4 grad = lerp(_BottomColor, _TopColor, _Blend);
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                col = lerp(col, grad, _Blend);
                clip(col.a - _Cutoff);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
