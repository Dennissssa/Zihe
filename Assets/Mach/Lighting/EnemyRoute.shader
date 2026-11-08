Shader "Mach/EnemyRoute"
{
 SubShader{Tags{"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}Pass{Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 positionOS:POSITION;half4 color:COLOR;};struct V{float4 positionCS:SV_POSITION;half4 color:COLOR;};
 V Vert(A a){V v;v.positionCS=TransformObjectToHClip(a.positionOS.xyz);v.color=a.color;return v;}
 half4 Frag(V v):SV_Target{return v.color;}
 ENDHLSL
 }}
}
