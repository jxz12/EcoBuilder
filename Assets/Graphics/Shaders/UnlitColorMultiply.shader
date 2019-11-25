Shader "Unlit/ColorMultiply"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Multiply"}
        
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