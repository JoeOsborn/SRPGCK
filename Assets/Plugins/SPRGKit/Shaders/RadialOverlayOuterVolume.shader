//Based on http://forum.unity3d.com/threads/34508-Simple-Cross-Section-Shader
Shader "Custom/RadialOverlayOuterVolume" 
{
  Properties
  {
	  _Invert        ("Invert",        Float ) = 0
    _Color         ("Overlay Color", Color ) = (0.8,0.8,0.8,0.4)
  }
  SubShader {
    Tags { "RenderType" = "Transparent"  "Queue" = "Transparent" }
    Cull Off
    CGPROGRAM
    #pragma surface surf Lambert alpha
    struct Input {
			float3 worldPos;
			float3 worldNormal;
    };
    
		fixed _Invert;
		fixed4 _Color;
	
	  void surf (Input IN, inout SurfaceOutput o) 
	  {
			o.Albedo = _Color.rgb;
			if(_Invert == 0) {
				o.Alpha = min(_Color.a*1.25, 1);
			} else {
				o.Alpha = min(_Color.a*0.75, 1);
			}
	  }
    ENDCG
  }
  Fallback "Diffuse"
}
