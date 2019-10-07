Shader "Mobile/Diffuse Color"
{
    Properties
    {
        _Color("Color",COLOR)=(0.5,0.5,0.5,1.0)
        // _MainTex ("Base (RGB)", 2D) = "white" {}
    }
 
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150
        CGPROGRAM
        #pragma surface surf Lambert noforwardadd
 
        sampler2D _MainTex;
        fixed4 _Color;
 
        struct Input
        {
            float2 uv_MainTex;
        };
 
        void surf (Input IN, inout SurfaceOutput o)
        {
            // fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = _Color.rgb;
            // o.Alpha = _Color.a;
        }
        ENDCG
    }
    Fallback "Mobile/VertexLit"
}