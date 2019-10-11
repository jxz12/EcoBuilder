Shader "Mobile/Species"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _MainTex2 ("Base (RGB)", 2D) = "white" {}
        _MainTex3 ("Base (RGB)", 2D) = "white" {}
        _MainTex4 ("Base (RGB)", 2D) = "white" {}
    }
 
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf Lambert approxview halfasview noforwardadd alpha:fade
 
        sampler2D _MainTex;
        sampler2D _MainTex2;
        sampler2D _MainTex3;
        sampler2D _MainTex4;
 
        struct Input
        {
            float2 uv_MainTex;
        };
 
        void surf (Input IN, inout SurfaceOutput o)
        {
            half4 c = tex2D(_MainTex, IN.uv_MainTex);
            half4 c2 = tex2D(_MainTex2, IN.uv_MainTex);
            half4 c3 = tex2D(_MainTex3, IN.uv_MainTex);
            half4 c4 = tex2D(_MainTex4, IN.uv_MainTex);

            o.Albedo = c.rgb + c2.rgb + c3.rgb + c4.rgb;
            o.Alpha = c.a + c2.a + c3.a + c4.a;
        }
        ENDCG
    }
}