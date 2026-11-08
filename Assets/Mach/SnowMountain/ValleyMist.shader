Shader "Mach/Valley Mist"
{
    Properties { _Color ("Mist color", Color) = (.52,.62,.73,.5) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes {float4 positionOS:POSITION;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;};
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            CBUFFER_END
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.world=TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.world);
                return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                float veil=.78+.12*sin(i.world.x*.013+_Time.y*.018)*sin(i.world.z*.019-_Time.y*.013);
                float fade=saturate((1800-length(i.world.xz-float2(260,-140)))/450);
                return half4(_Color.rgb,_Color.a*veil*fade);
            }
            ENDHLSL
        }
    }
}
