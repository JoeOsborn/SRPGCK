//Based on http://forum.unity3d.com/threads/34508-Simple-Cross-Section-Shader
Shader "Custom/SphereOverlayClip" 
{
  Properties
  {
	  _Origin        ("Origin",        Vector) = (0,0,0,1)
	  _Radius        ("Radius",        Float ) = 10
	  _RimThickness  ("RimThickness",  Float ) = 1
	  _DrawRim       ("DrawRim",       Float ) = 0
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
    
		float4 _Origin;
		float _Radius;
		float _RimThickness;
		fixed _DrawRim;
		fixed _Invert;
		fixed4 _Color;
	
	  void surf (Input IN, inout SurfaceOutput o) 
	  {
  		if(dot(IN.worldNormal, fixed3(0,1,0)) > 0.3) {
		    //get distance of worldPos from origin; if greater than radius, discard
		    float dist = distance(IN.worldPos, _Origin);
		    if((_Invert == 0 && dist > _Radius) ||
		       (_Invert != 0 && dist < _Radius)) {
		    	return;
		    }
		    if(_DrawRim != 0 && abs(dist - _Radius) < _RimThickness) {
		     	o.Albedo = _Color.rgb;
		     	if(_Invert == 0) {
		    		o.Alpha = min(_Color.a*1.25, 1);
		      } else {
		      	o.Alpha = min(_Color.a*0.75, 1);
		     	}
		    } else {
		    	o.Albedo = _Color.rgb;
		      o.Alpha = _Color.a;
		    }
			}
	  }
    ENDCG
  }
  Fallback "Diffuse"
}
