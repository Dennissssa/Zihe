Shader "Mach/InteractionMask"
{
 SubShader {Tags {"RenderPipeline"="UniversalPipeline"} Pass {ZWrite Off ZTest Always Cull Back
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 struct Attributes {float4 positionOS:POSITION;};
 struct Varyings {float4 positionCS:SV_POSITION;float eyeDepth:TEXCOORD0;};
 Varyings Vert(Attributes v){Varyings o;float3 world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(world);o.eyeDepth=-TransformWorldToView(world).z;return o;}
 half4 Frag(Varyings i):SV_Target {float2 uv=GetNormalizedScreenSpaceUV(i.positionCS);float depth=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams);clip(depth+.015-i.eyeDepth);return 1;}
 ENDHLSL
 }}
}
