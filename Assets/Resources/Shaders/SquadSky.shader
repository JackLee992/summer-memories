Shader "SM/SquadSky"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct a { float4 vertex:POSITION; };
            struct v { float4 pos:SV_POSITION; float3 direction:TEXCOORD0; };
            v vert(a i) { v o; o.pos=UnityObjectToClipPos(i.vertex);o.direction=i.vertex.xyz;return o; }
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            fixed4 frag(v i):SV_Target
            {
                float3 d=normalize(i.direction);float h=saturate(d.y);
                float3 sky=lerp(float3(.72,.84,.86),float3(.20,.46,.67),pow(h,.55));
                float2 p=d.xz/max(.12,d.y)*2.3;
                float cloud=noise(p)*.55+noise(p*2.1)*.28+noise(p*4.2)*.17;
                float c=smoothstep(.55,.73,cloud)*smoothstep(.02,.18,h);
                sky=lerp(sky,float3(.96,.96,.89),c*.82);
                float sun=pow(saturate(dot(d,normalize(float3(-.4,.55,-.6)))),380);
                return float4(sky+float3(1,.84,.51)*sun*.5,1);
            }
            ENDCG
        }
    }
}
