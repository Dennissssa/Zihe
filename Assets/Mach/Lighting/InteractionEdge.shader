Shader "Mach/InteractionEdge"
{
 SubShader {Tags {"RenderPipeline"="UniversalPipeline"} Pass {ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
 float4 _OutlineSettings;half4 _OutlineColor;
 half4 Frag(Varyings i):SV_Target
 {
  float2 uv=i.texcoord;float center=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv).r;float outer=center;
  [unroll] for(int n=0;n<16;n++){float a=n*0.39269908;float2 delta=float2(cos(a),sin(a))*_OutlineSettings.xy*_OutlineSettings.z;outer=max(outer,SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv+delta).r);}
  return half4(_OutlineColor.rgb,saturate(outer-center)*_OutlineColor.a);
 }
 ENDHLSL
 }}
}
