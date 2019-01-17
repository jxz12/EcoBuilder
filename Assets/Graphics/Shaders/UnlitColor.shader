Shader "Unlit/Color"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        
        ZWrite Off
        Lighting Off
        Fog { Mode Off }

        Pass {
            Color [_Color]
            // SetTexture [_MainTex] { combine texture * primary } 
        }
    }
}