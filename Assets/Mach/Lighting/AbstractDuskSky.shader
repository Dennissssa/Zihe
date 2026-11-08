Shader "Mach/Abstract Dusk Sky"
{
 Properties {
  _ZenithColor ("Upper sky - indigo", Color) = (.20,.27,.43,1)
  _MidColor ("Middle sky - muted teal", Color) = (.30,.47,.54,1)
  _HorizonColor ("Horizon - dusty peach", Color) = (.65,.48,.40,1)
  _NadirColor ("Lower sky", Color) = (.22,.26,.32,1)
  _RibbonColor ("Abstract bands - lavender", Color) = (.43,.39,.55,1)
  _Exposure ("Sky brightness", Range(0,2)) = .85
  _BandStrength ("Abstract band strength", Range(0,1)) = .28
  _Bands ("Color layers", Range(4,32)) = 12
  _Rotation ("Sky rotation", Range(0,360)) = 25
 }
 SubShader {
  Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"}
  Cull Off ZWrite Off
  Pass {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct Attributes {float4 position:POSITION;};
   struct Varyings {float4 position:SV_POSITION;float3 direction:TEXCOORD0;};
   half4 _ZenithColor,_MidColor,_HorizonColor,_NadirColor,_RibbonColor;
   float _Exposure,_BandStrength,_Bands,_Rotation;
   Varyings vert(Attributes v){Varyings o;o.position=UnityObjectToClipPos(v.position);o.direction=v.position.xyz;return o;}
   half4 frag(Varyings i):SV_Target {
    float3 d=normalize(i.direction);
    float angle=radians(_Rotation);float x=d.x*cos(angle)-d.z*sin(angle);float z=d.x*sin(angle)+d.z*cos(angle);
    // Broad, gently stepped color fields instead of realistic clouds or noise.
    float h=saturate(d.y+.045*sin(x*3+z*2));
    float layers=max(4,_Bands);float band=floor(h*layers)/layers;
    float height=lerp(h,band,.65);
    half3 color=lerp(_HorizonColor.rgb,_MidColor.rgb,smoothstep(0,.30,height));
    color=lerp(color,_ZenithColor.rgb,smoothstep(.25,.95,height));
    float wave=.30+.08*sin(x*3.5+z*1.7)+.035*sin(z*7-x*2);
    float ribbon=(1-smoothstep(.025,.095,abs(d.y-wave)))*smoothstep(0,.12,d.y);
    color=lerp(color,_RibbonColor.rgb,ribbon*_BandStrength);
    color=lerp(color,_NadirColor.rgb,smoothstep(0,.4,-d.y));
    return half4(color*_Exposure,1);
   }
   ENDHLSL
  }
 }
 Fallback Off
}
