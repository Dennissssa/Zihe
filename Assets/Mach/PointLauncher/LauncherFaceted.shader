Shader "Mach/Launcher Faceted"
{
 Properties { _BaseColor("Color",Color)=(1,1,1,1) }
 SubShader
 {
  Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
  Pass
  {
   Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;};
   Varyings vert(Attributes input){Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(input.normalOS);return o;}
   half4 frag(Varyings i):SV_Target {half light=.48+.52*saturate(dot(normalize(i.normalWS),normalize(float3(-.4,.8,-.6))));return half4(_BaseColor.rgb*light,1);}
   ENDHLSL
  }
 }
}
