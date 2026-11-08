Shader "Mach/Island Water"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            struct A { float4 vertex:POSITION; half4 color:COLOR; };
            struct V { float4 position:SV_POSITION; half4 color:COLOR; float fog:TEXCOORD0; };
            V vert(A v) { V o; o.position=TransformObjectToHClip(v.vertex.xyz); o.color=v.color; o.fog=ComputeFogFactor(o.position.z);return o; }
            half4 frag(V i):SV_Target { return half4(MixFog(SRGBToLinear(i.color.rgb),i.fog),1); }
            ENDHLSL
        }
    }
}
