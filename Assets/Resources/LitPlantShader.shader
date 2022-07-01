Shader "Custom/LitPlantShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Cutoff  ("Cutoff", Float) = 0.5
        _Gradient ("Gradient", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off
        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float4 vertColor : COLOR;
        };
        
        fixed4 _Color;
        float _Cutoff;
        float _Gradient;
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            clip(c.a- _Cutoff);
            
            o.Albedo = lerp(c.rgb, IN.vertColor.rgb, _Gradient);
            o.Alpha = c.a;
            
        }
        ENDCG
    }
    FallBack "Diffuse"
}
