Shader "Unlit/TexBehind"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
 
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Offset 0, 100

        Pass {
            Lighting Off
            SetTexture [_MainTex]
        }
    }
}