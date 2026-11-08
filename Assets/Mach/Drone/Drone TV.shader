Shader "Mach/UI/Drone TV"
{
    Properties
    {
        _Strength ("Steady signal texture", Range(0,1)) = 0.38
        _Startup ("Tuning duration (seconds)", Range(0.25,3)) = 0.95
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Varyings { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings vert(appdata_base v)
            {
                Varyings o;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.uv=v.texcoord.xy;
                return o;
            }
            float _MachDroneSignalAge, _Strength, _Startup;
            float hash(float2 p)
            {
                float3 q=frac(p.xyx*0.1031);
                q+=dot(q,q.yzx+33.33);
                return frac((q.x+q.y)*q.z);
            }
            half4 frag(Varyings i):SV_Target
            {
                float age=_MachDroneSignalAge;
                float progress=saturate(age/max(.01,_Startup));
                float warm=1-smoothstep(.15,1,progress);
                float frame=floor(age*22);
                float grain=hash(floor(i.pos.xy/2)+frame);
                float scan=.5+.5*cos(i.pos.y*1.57);
                float edge=smoothstep(.5,1,length(i.uv*2-1));
                float band=saturate(1-abs(frac(i.uv.y-age*.65)-.5)*24);
                float snow=grain*(.08*_Strength+warm*.5)+band*(.025+warm*.10);
                float mask=step(smoothstep(.10,.63,progress),abs(i.uv.y*2-1));
                float shade=saturate(mask+(.12*scan+.22*edge)*_Strength);
                float alpha=shade+(1-shade)*snow;
                return half4(half3(.73,.92,.82)*snow*(1-shade)/max(.001,alpha),alpha);
            }
            ENDCG
        }
    }
}
