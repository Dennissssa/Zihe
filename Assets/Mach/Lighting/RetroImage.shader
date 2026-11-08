Shader "Mach/RetroImage" {
 SubShader {Tags {"RenderPipeline"="UniversalPipeline"} Pass {
 ZWrite Off ZTest Always Cull Off
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 float4 _MountainFog,_DistantFogColor,_ValleyFogColor;
 TEXTURE2D(_MachSharpMask); SAMPLER(sampler_MachSharpMask);
 TEXTURE2D(_MissionMask); SAMPLER(sampler_MissionMask);
 float _HasMissionMask;
 half3 MissionMask(float2 uv){return SAMPLE_TEXTURE2D(_MissionMask,sampler_MissionMask,uv).rgb;}
 float4 _RetroGrid,_RetroEffects,_RetroBloom;
 half3 Read(float2 uv){return SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,saturate(uv)).rgb;}
 half3 Glow(float2 uv){half3 c=Read(uv);return max(c-_RetroBloom.y,0);}
 half4 Frag(Varyings input):SV_Target {
  float2 uv=input.texcoord;
  if(_RetroBloom.w>.5){
   float tick=floor(_Time.y*13);
   float burst=step(.84,frac(sin(tick*13.17)*4375.23));
   float strip=step(.72,frac(uv.y*5.0+tick*.37));
   uv.x=saturate(uv.x+burst*strip*sin(tick+floor(uv.y*125))*.0045);
  }
  half3 original=Read(uv);
  float2 pixel=(floor(uv*_RetroGrid.xy)+.5)/_RetroGrid.xy;
  half3 c=Read(pixel);
  float2 offset=_RetroEffects.zw*_RetroBloom.z;
  c+=(Glow(pixel+float2(offset.x,0))+Glow(pixel-float2(offset.x,0))+Glow(pixel+float2(0,offset.y))+Glow(pixel-float2(0,offset.y)))*(.25*_RetroBloom.x);
  float2 cell=floor(uv*_RetroGrid.xy);
  float noise=frac(dot(cell,float2(.75487766,.56984029)))-.5;
  c=floor(max(0,c*_RetroGrid.z+noise*_RetroEffects.x)+.5)/_RetroGrid.z;
  c*=1-_RetroEffects.y*(.5+.5*cos(uv.y*_RetroGrid.y*6.283185));
  half protect=SAMPLE_TEXTURE2D(_MachSharpMask,sampler_MachSharpMask,uv).r;
  half3 scene=lerp(original,c,_RetroGrid.w*(1-protect));
  if(_MountainFog.x>0 || _MountainFog.y>0){
   float depth=SampleSceneDepth(uv);
   #if UNITY_REVERSED_Z
    bool sky=depth<.00001;
   #else
    bool sky=depth>.99999;
    depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
   #endif
   float3 world=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
   float3 ray=world-_WorldSpaceCameraPos;
   float distanceToPixel=length(ray);
   float3 direction=ray/max(distanceToPixel,.001);
   float travel=sky?6000:min(distanceToPixel,6000);
   // Integrate density along the view ray: no plane, surface or hard height edge.
   float valleyOpticalDepth=0;
   [unroll] for(int sampleIndex=0;sampleIndex<16;sampleIndex++){
    float sampleHeight=_WorldSpaceCameraPos.y+direction.y*travel*((sampleIndex+.5)/16.0);
    valleyOpticalDepth+=exp(clamp((_MountainFog.z-sampleHeight)*_MountainFog.w,-20,4));
   }
   valleyOpticalDepth*=travel*_MountainFog.y/16.0;
   float distantOpticalDepth=travel*_MountainFog.x;
   if(sky)distantOpticalDepth*=1-smoothstep(.005,.15,direction.y);
   float opticalDepth=valleyOpticalDepth+distantOpticalDepth;
   float3 fogColor=lerp(_DistantFogColor.rgb,_ValleyFogColor.rgb,saturate(valleyOpticalDepth/max(opticalDepth,.0001)));
   scene=lerp(scene,fogColor,1-exp(-min(opticalDepth,25)));
  }
  if(_RetroBloom.w>.5){
   half luminance=dot(scene,half3(.2126,.7152,.0722));
   scene=max(0,(lerp(luminance.xxx,scene,.4)-.18)*1.4+.18);
  }
  if(_HasMissionMask>.5){
   float2 dx=float2(_RetroEffects.z*2,0),dy=float2(0,_RetroEffects.w*2);
   half3 center=MissionMask(uv);
   half3 spread=max(max(MissionMask(uv+dx),MissionMask(uv-dx)),max(MissionMask(uv+dy),MissionMask(uv-dy)));
   half3 edge=saturate(spread-center);
   scene=lerp(scene,half3(1,.64,.07),saturate(center.b*.48+edge.b*.9));
   scene=lerp(scene,half3(.05,.95,.95),saturate(center.g*.18+edge.g));
   scene=lerp(scene,half3(1,.009,.019),saturate(center.r*.52+edge.r*1.5));
  }
  return half4(scene,1);
 }
 ENDHLSL
 }}
}
