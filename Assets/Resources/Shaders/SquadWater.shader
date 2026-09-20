Shader "SM/SquadWater"
{
 Properties{_Color("Water",Color)=(.035,.48,.58,1)}
 SubShader{Tags{"RenderType"="Opaque"}
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 struct a{float4 vertex:POSITION;};struct v{float4 pos:SV_POSITION;float3 world:TEXCOORD0;UNITY_FOG_COORDS(1)};
 fixed4 _Color;
 v vert(a i){v o;float3 w=mul(unity_ObjectToWorld,i.vertex).xyz;w.y+=sin(w.x*.24+_Time.y*.8)*.045+cos(w.z*.4-_Time.y*.6)*.03;o.world=w;o.pos=UnityWorldToClipPos(w);UNITY_TRANSFER_FOG(o,o.pos);return o;}
 fixed4 frag(v i):SV_Target{
 float wave=sin(i.world.x*.7+i.world.z*.22+_Time.y)*cos(i.world.z*.6-_Time.y*.7);
 float glitter=pow(saturate(wave),18);float d=distance(i.world.xz,_WorldSpaceCameraPos.xz);
 float3 c=lerp(_Color.rgb,float3(.11,.39,.57),saturate(d/130));c+=float3(.26,.42,.37)*glitter*.7;
 fixed4 outc=fixed4(c,1);UNITY_APPLY_FOG(i.fogCoord,outc);return outc;}
 ENDCG}
 }
}
