Shader "Unlit/ColourAlpha"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Fade"}
        
        Color [_Color]
        Blend SrcAlpha OneMinusSrcAlpha 
        // Blend One OneMinusSrcAlpha 
        // Blend One SrcAlpha 
        Pass {}
    }
}