Shader "Unlit/ColorDifference"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        // _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Fade"}
        
        // ZWrite Off
        // Lighting Off
        // Fog { Mode Off }


        Color [_Color]
        // Blend SrcAlpha OneMinusSrcAlpha 
        Blend One OneMinusSrcAlpha 
        // Blend One SrcAlpha 
        Pass {}
    }
}