//Based on http://forum.unity3d.com/threads/34508-Simple-Cross-Section-Shader
Shader "Custom/CylinderOverlayClipInverted" 
{
  Properties
  {
	  _Origin        ("Origin",        Vector) = (0,0,0,1)
	  _Radius        ("Radius",        Float ) = 10
	  _Height        ("Height",        Float ) = 10
	  _RimThickness  ("RimThickness",  Float ) = 1
	  _DrawRim       ("DrawRim",       Float ) = 0
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
		float _Height;
		float _RimThickness;
		fixed _DrawRim;
		fixed4 _Color;
	
	  void surf (Input IN, inout SurfaceOutput o) 
	  {
  		if(dot(IN.worldNormal, fixed3(0,1,0)) > 0.3) {
		    //get distance of worldPos from origin; if greater than radius, discard
		    float distXY = distance(IN.worldPos.xz, _Origin.xz);
				float dZ = abs(IN.worldPos.y - _Origin.y);
		    if(distXY < _Radius && dZ < _Height) {
		    	return;
		    }
		    if(_DrawRim != 0 && (abs(distXY - _Radius) < _RimThickness) && (abs(dZ - _Height) < _RimThickness)) {
		     	o.Albedo = _Color.rgb;
	      	o.Alpha = min(_Color.a*0.75, 1);
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
