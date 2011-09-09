//Based on http://forum.unity3d.com/threads/34508-Simple-Cross-Section-Shader
Shader "Custom/OverlayClip" 
{
   Properties
   {
			//world origin xyz (ignore w)
			_MapWorldOrigin ("Map World Origin", Vector) = (0,0,0,1)
			//tile size xyz (ignore w)
			_MapTileSize ("Map Tile Size", Vector) = (10,10,10,1)
			//map dimensions xyz (ignore w)
			_MapSizeInTiles ("Map Size in Tiles", Vector) = (20,20,20,1)
			//color of overlay
      _Color ("Overlay Color", Color) = (0.3,0.4,0.9,0.7)
			//one pixel per tile
			//ar stand for bounds 1 min and max
			//gb stand for bounds 2 min and max
			//any more than 2 discontinuous move destinations are unsupported
      _Boxes ("Box Map", 2D) = "black" {}
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
      
      sampler2D _Boxes;
			float4 _MapWorldOrigin;
			float4 _MapTileSize;
			float4 _MapSizeInTiles;
			fixed4 _Color;
			
			float3 trunc(float3 v)
			{
			  float3 rv;
			  int i;

			  for (i=0; i<3; i++) {
			    float x = v[i];

			    rv[i] = x < 0 ? -floor(-x) : floor(x);
			  }
			  return rv;
			}
			
			void surf (Input IN, inout SurfaceOutput o) 
			{
				float3 localZero = IN.worldPos.xyz-_MapWorldOrigin.xyz-float3(-5,0.01,-5); //10..??->0..??
				float3 mapCube = floor(localZero.xyz/_MapTileSize.xyz); //0..?? -> 0..20, 0..20, 0..64
				float3 boxUV = mapCube.xzy/_MapSizeInTiles.xzy; //0..20 -> 0..1, 0..1, 0..1
				float4 boxTex = tex2D(_Boxes, boxUV.xy);
				// o.Albedo = boxTex.xyz;
				// o.Alpha = 1;
				if(boxTex.a == 0 && boxTex.r == 0 &&
				   boxTex.g == 0 && boxTex.b == 0) { //nothing here
					return;
				}
				if(dot(IN.worldNormal, fixed3(0,1,0)) > 0.3) {
					if((boxUV.z >= boxTex.a && boxUV.z <= boxTex.r) ||  //does our Z hit the right spot?
				   	(boxUV.z >= boxTex.g && boxUV.z <= boxTex.b)) {
						o.Albedo = _Color.rgb;
						o.Alpha = _Color.a;
					}
				}
			}
      ENDCG
    }
    Fallback "Diffuse"
}
