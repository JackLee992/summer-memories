Shader "SM/SquadSurface"
{
    Properties { _Color("Color",Color)=(1,1,1,1) _SurfaceDetail("Stone detail", Range(0,2))=0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _Color;
        half _SurfaceDetail;
        struct Input { float3 worldPos; };
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 p=IN.worldPos.xz;
            float noise=frac(sin(dot(floor(p*35),float2(12.9898,78.233)))*43758.5453);
            float2 tile=frac(p/float2(1.25,2.0));
            float grout=1-smoothstep(.008,.019,min(min(tile.x,1-tile.x),min(tile.y,1-tile.y)));
            float stone=frac(sin(dot(floor(p/float2(1.25,2.0)),float2(27.3,41.7)))*1731.6);
            float factor=lerp(1,.97+noise*.035,saturate(_SurfaceDetail));
            factor*=lerp(1,.91+stone*.15-grout*.18,saturate(_SurfaceDetail-1));
            o.Albedo=_Color.rgb*factor; o.Metallic=0; o.Smoothness=.12; o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
