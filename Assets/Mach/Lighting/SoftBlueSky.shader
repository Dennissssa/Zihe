Shader "Mach/Soft Blue Sky"
{
    Properties
    {
        _ZenithColor ("Upper sky", Color) = (0.57,0.77,0.89,1)
        _HorizonColor ("Horizon", Color) = (0.83,0.91,0.95,1)
        _NadirColor ("Lower sky", Color) = (0.76,0.85,0.90,1)
        _Exposure ("Exposure", Range(0,2)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Attributes { float4 position : POSITION; };
            struct Varyings { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
            half4 _ZenithColor, _HorizonColor, _NadirColor;
            half _Exposure;
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position=UnityObjectToClipPos(input.position);
                output.direction=input.position.xyz;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float height=normalize(input.direction).y;
                float blend=smoothstep(0,1,pow(abs(height),0.65));
                half3 color=lerp(_HorizonColor.rgb,height>=0?_ZenithColor.rgb:_NadirColor.rgb,blend);
                return half4(color*_Exposure,1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
