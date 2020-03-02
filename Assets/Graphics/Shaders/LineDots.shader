// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/DottedLineShader"
{
    Properties
    {
        _RepeatCount("Repeat Count", float) = 5
        _Spacing("Spacing", float) = 0.5
        _Offset("Offset", float) = 0
        _Alpha("Alpha", float) = 1
    }
        SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // this is not possible because of different Properties per line
            // #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            float _RepeatCount;
            float _Spacing;
            float _Offset;
            float _Alpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;              
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uv.x = (o.uv.x + _Offset) * _RepeatCount * (1.0f + _Spacing);
                o.color = v.color;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // i.uv.x = fmod(i.uv.x, 1.0f + _Spacing);
                i.uv.x = frac(abs(i.uv.x/(1.0f+_Spacing)))*abs(1.0f+_Spacing);
                float r = length(i.uv - float2(1.0f + _Spacing, 1.0f) * 0.5f) * 2.0f;

                fixed4 color = i.color;
                // color.a *= saturate((0.99f - r) * 100.0f);
                color.a = r < 1? _Alpha : .2f*_Alpha;

                return color;
            }
            ENDCG
        }
    }
}