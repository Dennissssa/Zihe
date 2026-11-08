Shader "Mach/DroneMissionMask" {
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" }
  ZWrite Off ZTest Always Cull Off
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  float4 Vert(float3 position:POSITION):SV_POSITION {return TransformObjectToHClip(position);}
  half4 Frag():SV_Target{return 1;}
  ENDHLSL
  Pass { ColorMask R
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   ENDHLSL
  }
  Pass { ColorMask G
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   ENDHLSL
  }
  Pass { ColorMask B
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   ENDHLSL
  }
 }
}
