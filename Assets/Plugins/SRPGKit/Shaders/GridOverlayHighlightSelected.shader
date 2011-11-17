//Based on http://forum.unity3d.com/threads/34508-Simple-Cross-Section-Shader
Shader "Custom/GridOverlayHighlightSelected" 
{
   Properties
   {
      //world origin xyz (ignore w)
      _MapWorldOrigin ("Map World Origin", Vector) = (0,0,0,1)
      //tile size xyz (ignore w)
      _MapTileSize ("Map Tile Size", Vector) = (10,10,10,1)
      _SelectedColor ("Selected Tile Color", Color) = (0.6,0.8,1.0,0.7)
      _SelectedPoint ("Selected Point", Vector) = (-1,-1,-1,1)
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
      
      float4 _MapWorldOrigin;
      float4 _MapTileSize;
      float4 _SelectedPoint;
      fixed4 _SelectedColor;
      
      void surf (Input IN, inout SurfaceOutput o) 
      {
        float3 localZero = IN.worldPos.xyz-_MapWorldOrigin.xyz; //10..??->0..??
        float3 mapCube = floor(localZero.xyz/_MapTileSize.xyz); //0..?? -> 0..20, 0..64, 0..20
        if(dot(IN.worldNormal, fixed3(0,1,0)) > 0.3) {
          if(mapCube.x == _SelectedPoint.x &&
             mapCube.z == _SelectedPoint.y &&
             mapCube.y >= _SelectedPoint.z &&
             mapCube.y <= _SelectedPoint.w) {
            o.Albedo = _SelectedColor.rgb;
            o.Alpha  = _SelectedColor.a;
          }
        }
      }
      ENDCG
    }
    Fallback "Diffuse"
}
