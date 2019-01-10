Shader "Unlit/Color Alpha"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        // _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _Alpha ("Alpha Multiplier", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        
        ZWrite Off
        Lighting Off
        Fog { Mode Off }
        Blend SrcAlpha OneMinusSrcAlpha 

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };
			
			struct appdata {
				float4 vertex : POSITION;
			};

            v2f vert(appdata v)
            {
                v2f o;
			    o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 _Color;
            fixed _Alpha;

            fixed4 frag(v2f i) : SV_TARGET
            {
                fixed4 c = fixed4(_Color.r, _Color.g, _Color.b, _Alpha);
                return c;
            }
            ENDCG
        }
    }
}