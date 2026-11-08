Shader "Mach/FenceGlow" {
 Properties { _Color ("Glow",Color)=(2,.015,.02,.22) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   Blend SrcAlpha One
   ZWrite Off
   Cull Back
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Color;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;};
   Varyings Vert(Attributes v){Varyings o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(v.normalOS);return o;}
   half4 Frag(Varyings i):SV_Target {half facing=saturate(abs(dot(normalize(i.normalWS),GetWorldSpaceNormalizeViewDir(i.positionWS))));return half4(_Color.rgb,_Color.a*facing);}
   ENDHLSL
  }
 }
}
