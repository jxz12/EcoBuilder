Shader "Mobile/Diffuse Color"
{
    Properties
    {
        _Color("Color",COLOR)=(0.5,0.5,0.5,1.0)
    }
 
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150
        CGPROGRAM
        #pragma surface surf Lambert noforwardadd
 
        fixed4 _Color;
 
        struct Input
        {
            float foo; // optimised away?
        };
 
        void surf (Input IN, inout SurfaceOutput o)
        {
            o.Albedo = _Color.rgb;
            // o.Albedo = IN.color.rgb;
        }
        ENDCG
    }
}