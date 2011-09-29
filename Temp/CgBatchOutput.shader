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
      	Alphatest Greater 0 ZWrite Off ColorMask RGB
	
	Pass {
		Name "FORWARD"
		Tags { "LightMode" = "ForwardBase" }
		Blend SrcAlpha OneMinusSrcAlpha
Program "vp" {
// Vertex combos: 3
//   opengl - ALU: 11 to 62
//   d3d9 - ALU: 11 to 62
SubProgram "opengl " {
Keywords { "DIRECTIONAL" "LIGHTMAP_OFF" }
Bind "vertex" Vertex
Bind "normal" Normal
Vector 9 [unity_Scale]
Matrix 5 [_Object2World]
Vector 10 [unity_SHAr]
Vector 11 [unity_SHAg]
Vector 12 [unity_SHAb]
Vector 13 [unity_SHBr]
Vector 14 [unity_SHBg]
Vector 15 [unity_SHBb]
Vector 16 [unity_SHC]
"!!ARBvp1.0
# 32 ALU
PARAM c[17] = { { 1 },
		state.matrix.mvp,
		program.local[5..16] };
TEMP R0;
TEMP R1;
TEMP R2;
TEMP R3;
MUL R1.xyz, vertex.normal, c[9].w;
DP3 R3.w, R1, c[6];
DP3 R2.w, R1, c[7];
DP3 R0.x, R1, c[5];
MOV R0.y, R3.w;
MOV R0.z, R2.w;
MUL R1, R0.xyzz, R0.yzzx;
MOV R0.w, c[0].x;
DP4 R2.z, R0, c[12];
DP4 R2.y, R0, c[11];
DP4 R2.x, R0, c[10];
MUL R0.y, R3.w, R3.w;
DP4 R3.z, R1, c[15];
DP4 R3.y, R1, c[14];
DP4 R3.x, R1, c[13];
MAD R0.y, R0.x, R0.x, -R0;
MUL R1.xyz, R0.y, c[16];
ADD R2.xyz, R2, R3;
ADD result.texcoord[3].xyz, R2, R1;
MOV result.texcoord[2].z, R2.w;
MOV result.texcoord[2].y, R3.w;
MOV result.texcoord[2].x, R0;
DP4 result.position.w, vertex.position, c[4];
DP4 result.position.z, vertex.position, c[3];
DP4 result.position.y, vertex.position, c[2];
DP4 result.position.x, vertex.position, c[1];
DP3 result.texcoord[0].z, vertex.normal, c[7];
DP3 result.texcoord[0].y, vertex.normal, c[6];
DP3 result.texcoord[0].x, vertex.normal, c[5];
DP4 result.texcoord[1].z, vertex.position, c[7];
DP4 result.texcoord[1].y, vertex.position, c[6];
DP4 result.texcoord[1].x, vertex.position, c[5];
END
# 32 instructions, 4 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "DIRECTIONAL" "LIGHTMAP_OFF" }
Bind "vertex" Vertex
Bind "normal" Normal
Matrix 0 [glstate_matrix_mvp]
Vector 8 [unity_Scale]
Matrix 4 [_Object2World]
Vector 9 [unity_SHAr]
Vector 10 [unity_SHAg]
Vector 11 [unity_SHAb]
Vector 12 [unity_SHBr]
Vector 13 [unity_SHBg]
Vector 14 [unity_SHBb]
Vector 15 [unity_SHC]
"vs_2_0
; 32 ALU
def c16, 1.00000000, 0, 0, 0
dcl_position0 v0
dcl_normal0 v1
mul r1.xyz, v1, c8.w
dp3 r3.w, r1, c5
dp3 r2.w, r1, c6
dp3 r0.x, r1, c4
mov r0.y, r3.w
mov r0.z, r2.w
mul r1, r0.xyzz, r0.yzzx
mov r0.w, c16.x
dp4 r2.z, r0, c11
dp4 r2.y, r0, c10
dp4 r2.x, r0, c9
mul r0.y, r3.w, r3.w
dp4 r3.z, r1, c14
dp4 r3.y, r1, c13
dp4 r3.x, r1, c12
mad r0.y, r0.x, r0.x, -r0
mul r1.xyz, r0.y, c15
add r2.xyz, r2, r3
add oT3.xyz, r2, r1
mov oT2.z, r2.w
mov oT2.y, r3.w
mov oT2.x, r0
dp4 oPos.w, v0, c3
dp4 oPos.z, v0, c2
dp4 oPos.y, v0, c1
dp4 oPos.x, v0, c0
dp3 oT0.z, v1, c6
dp3 oT0.y, v1, c5
dp3 oT0.x, v1, c4
dp4 oT1.z, v0, c6
dp4 oT1.y, v0, c5
dp4 oT1.x, v0, c4
"
}

SubProgram "gles " {
Keywords { "DIRECTIONAL" "LIGHTMAP_OFF" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying lowp vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 unity_Scale;
uniform highp vec4 unity_SHC;
uniform highp vec4 unity_SHBr;
uniform highp vec4 unity_SHBg;
uniform highp vec4 unity_SHBb;
uniform highp vec4 unity_SHAr;
uniform highp vec4 unity_SHAg;
uniform highp vec4 unity_SHAb;

uniform highp mat4 _Object2World;
attribute vec3 _glesNormal;
attribute vec4 _glesVertex;
void main ()
{
  vec3 tmpvar_1;
  tmpvar_1 = normalize (_glesNormal);
  highp vec3 shlight;
  lowp vec3 tmpvar_2;
  lowp vec3 tmpvar_3;
  lowp vec3 tmpvar_4;
  mat3 tmpvar_5;
  tmpvar_5[0] = _Object2World[0].xyz;
  tmpvar_5[1] = _Object2World[1].xyz;
  tmpvar_5[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_6;
  tmpvar_6 = (tmpvar_5 * tmpvar_1);
  tmpvar_2 = tmpvar_6;
  mat3 tmpvar_7;
  tmpvar_7[0] = _Object2World[0].xyz;
  tmpvar_7[1] = _Object2World[1].xyz;
  tmpvar_7[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_8;
  tmpvar_8 = (tmpvar_7 * (tmpvar_1 * unity_Scale.w));
  tmpvar_3 = tmpvar_8;
  highp vec4 tmpvar_9;
  tmpvar_9.w = 1.0;
  tmpvar_9.xyz = tmpvar_8;
  mediump vec3 tmpvar_10;
  mediump vec4 normal;
  normal = tmpvar_9;
  mediump vec3 x3;
  highp float vC;
  mediump vec3 x2;
  mediump vec3 x1;
  highp float tmpvar_11;
  tmpvar_11 = dot (unity_SHAr, normal);
  x1.x = tmpvar_11;
  highp float tmpvar_12;
  tmpvar_12 = dot (unity_SHAg, normal);
  x1.y = tmpvar_12;
  highp float tmpvar_13;
  tmpvar_13 = dot (unity_SHAb, normal);
  x1.z = tmpvar_13;
  mediump vec4 tmpvar_14;
  tmpvar_14 = (normal.xyzz * normal.yzzx);
  highp float tmpvar_15;
  tmpvar_15 = dot (unity_SHBr, tmpvar_14);
  x2.x = tmpvar_15;
  highp float tmpvar_16;
  tmpvar_16 = dot (unity_SHBg, tmpvar_14);
  x2.y = tmpvar_16;
  highp float tmpvar_17;
  tmpvar_17 = dot (unity_SHBb, tmpvar_14);
  x2.z = tmpvar_17;
  mediump float tmpvar_18;
  tmpvar_18 = ((normal.x * normal.x) - (normal.y * normal.y));
  vC = tmpvar_18;
  highp vec3 tmpvar_19;
  tmpvar_19 = (unity_SHC.xyz * vC);
  x3 = tmpvar_19;
  tmpvar_10 = ((x1 + x2) + x3);
  shlight = tmpvar_10;
  tmpvar_4 = shlight;
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_TEXCOORD0 = tmpvar_2;
  xlv_TEXCOORD1 = (_Object2World * _glesVertex).xyz;
  xlv_TEXCOORD2 = tmpvar_3;
  xlv_TEXCOORD3 = tmpvar_4;
}



#endif
#ifdef FRAGMENT

varying lowp vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform lowp vec4 _WorldSpaceLightPos0;
uniform highp vec4 _MapWorldOrigin;
uniform highp vec4 _MapTileSize;
uniform highp vec4 _MapSizeInTiles;
uniform lowp vec4 _LightColor0;
uniform lowp vec4 _Color;
uniform sampler2D _Boxes;
void main ()
{
  lowp vec4 c;
  lowp vec3 tmpvar_1;
  lowp float tmpvar_2;
  highp vec3 tmpvar_3;
  tmpvar_3 = xlv_TEXCOORD0;
  tmpvar_1 = vec3(0.0, 0.0, 0.0);
  tmpvar_2 = 0.0;
  lowp vec3 tmpvar_4;
  lowp float tmpvar_5;
  tmpvar_4 = tmpvar_1;
  tmpvar_5 = tmpvar_2;
  highp vec4 boxTex;
  highp vec3 tmpvar_6;
  tmpvar_6 = (floor ((((xlv_TEXCOORD1 - _MapWorldOrigin.xyz) - vec3(-5.0, 0.01, -5.0)) / _MapTileSize.xyz)).xzy / _MapSizeInTiles.xzy);
  lowp vec4 tmpvar_7;
  tmpvar_7 = texture2D (_Boxes, tmpvar_6.xy);
  boxTex = tmpvar_7;
  bool tmpvar_8;
  if ((boxTex.w == 0.0)) {
    tmpvar_8 = (boxTex.x == 0.0);
  } else {
    tmpvar_8 = bool(0);
  };
  bool tmpvar_9;
  if (tmpvar_8) {
    tmpvar_9 = (boxTex.y == 0.0);
  } else {
    tmpvar_9 = bool(0);
  };
  bool tmpvar_10;
  if (tmpvar_9) {
    tmpvar_10 = (boxTex.z == 0.0);
  } else {
    tmpvar_10 = bool(0);
  };
  if (tmpvar_10) {
  } else {
    highp float tmpvar_11;
    tmpvar_11 = dot (tmpvar_3, vec3(0.0, 1.0, 0.0));
    if ((tmpvar_11 > 0.3)) {
      bool tmpvar_12;
      if ((tmpvar_6.z >= boxTex.w)) {
        tmpvar_12 = (tmpvar_6.z <= boxTex.x);
      } else {
        tmpvar_12 = bool(0);
      };
      bool tmpvar_13;
      if (tmpvar_12) {
        tmpvar_13 = bool(1);
      } else {
        bool tmpvar_14;
        if ((tmpvar_6.z >= boxTex.y)) {
          tmpvar_14 = (tmpvar_6.z <= boxTex.z);
        } else {
          tmpvar_14 = bool(0);
        };
        tmpvar_13 = tmpvar_14;
      };
      if (tmpvar_13) {
        tmpvar_4 = _Color.xyz;
        tmpvar_5 = _Color.w;
      };
    };
  };
  tmpvar_1 = tmpvar_4;
  tmpvar_2 = tmpvar_5;
  lowp vec4 c_i0;
  c_i0.xyz = ((tmpvar_4 * _LightColor0.xyz) * (max (0.0, dot (xlv_TEXCOORD2, _WorldSpaceLightPos0.xyz)) * 2.0));
  c_i0.w = tmpvar_5;
  c = c_i0;
  c.xyz = (c_i0.xyz + (tmpvar_4 * xlv_TEXCOORD3));
  c.w = tmpvar_5;
  gl_FragData[0] = c;
}



#endif"
}

SubProgram "glesdesktop " {
Keywords { "DIRECTIONAL" "LIGHTMAP_OFF" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying lowp vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 unity_Scale;
uniform highp vec4 unity_SHC;
uniform highp vec4 unity_SHBr;
uniform highp vec4 unity_SHBg;
uniform highp vec4 unity_SHBb;
uniform highp vec4 unity_SHAr;
uniform highp vec4 unity_SHAg;
uniform highp vec4 unity_SHAb;

uniform highp mat4 _Object2World;
attribute vec3 _glesNormal;
attribute vec4 _glesVertex;
void main ()
{
  vec3 tmpvar_1;
  tmpvar_1 = normalize (_glesNormal);
  highp vec3 shlight;
  lowp vec3 tmpvar_2;
  lowp vec3 tmpvar_3;
  lowp vec3 tmpvar_4;
  mat3 tmpvar_5;
  tmpvar_5[0] = _Object2World[0].xyz;
  tmpvar_5[1] = _Object2World[1].xyz;
  tmpvar_5[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_6;
  tmpvar_6 = (tmpvar_5 * tmpvar_1);
  tmpvar_2 = tmpvar_6;
  mat3 tmpvar_7;
  tmpvar_7[0] = _Object2World[0].xyz;
  tmpvar_7[1] = _Object2World[1].xyz;
  tmpvar_7[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_8;
  tmpvar_8 = (tmpvar_7 * (tmpvar_1 * unity_Scale.w));
  tmpvar_3 = tmpvar_8;
  highp vec4 tmpvar_9;
  tmpvar_9.w = 1.0;
  tmpvar_9.xyz = tmpvar_8;
  mediump vec3 tmpvar_10;
  mediump vec4 normal;
  normal = tmpvar_9;
  mediump vec3 x3;
  highp float vC;
  mediump vec3 x2;
  mediump vec3 x1;
  highp float tmpvar_11;
  tmpvar_11 = dot (unity_SHAr, normal);
  x1.x = tmpvar_11;
  highp float tmpvar_12;
  tmpvar_12 = dot (unity_SHAg, normal);
  x1.y = tmpvar_12;
  highp float tmpvar_13;
  tmpvar_13 = dot (unity_SHAb, normal);
  x1.z = tmpvar_13;
  mediump vec4 tmpvar_14;
  tmpvar_14 = (normal.xyzz * normal.yzzx);
  highp float tmpvar_15;
  tmpvar_15 = dot (unity_SHBr, tmpvar_14);
  x2.x = tmpvar_15;
  highp float tmpvar_16;
  tmpvar_16 = dot (unity_SHBg, tmpvar_14);
  x2.y = tmpvar_16;
  highp float tmpvar_17;
  tmpvar_17 = dot (unity_SHBb, tmpvar_14);
  x2.z = tmpvar_17;
  mediump float tmpvar_18;
  tmpvar_18 = ((normal.x * normal.x) - (normal.y * normal.y));
  vC = tmpvar_18;
  highp vec3 tmpvar_19;
  tmpvar_19 = (unity_SHC.xyz * vC);
  x3 = tmpvar_19;
  tmpvar_10 = ((x1 + x2) + x3);
  shlight = tmpvar_10;
  tmpvar_4 = shlight;
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_TEXCOORD0 = tmpvar_2;
  xlv_TEXCOORD1 = (_Object2World * _glesVertex).xyz;
  xlv_TEXCOORD2 = tmpvar_3;
  xlv_TEXCOORD3 = tmpvar_4;
}



#endif
#ifdef FRAGMENT

varying lowp vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform lowp vec4 _WorldSpaceLightPos0;
uniform highp vec4 _MapWorldOrigin;
uniform highp vec4 _MapTileSize;
uniform highp vec4 _MapSizeInTiles;
uniform lowp vec4 _LightColor0;
uniform lowp vec4 _Color;
uniform sampler2D _Boxes;
void main ()
{
  lowp vec4 c;
  lowp vec3 tmpvar_1;
  lowp float tmpvar_2;
  highp vec3 tmpvar_3;
  tmpvar_3 = xlv_TEXCOORD0;
  tmpvar_1 = vec3(0.0, 0.0, 0.0);
  tmpvar_2 = 0.0;
  lowp vec3 tmpvar_4;
  lowp float tmpvar_5;
  tmpvar_4 = tmpvar_1;
  tmpvar_5 = tmpvar_2;
  highp vec4 boxTex;
  highp vec3 tmpvar_6;
  tmpvar_6 = (floor ((((xlv_TEXCOORD1 - _MapWorldOrigin.xyz) - vec3(-5.0, 0.01, -5.0)) / _MapTileSize.xyz)).xzy / _MapSizeInTiles.xzy);
  lowp vec4 tmpvar_7;
  tmpvar_7 = texture2D (_Boxes, tmpvar_6.xy);
  boxTex = tmpvar_7;
  bool tmpvar_8;
  if ((boxTex.w == 0.0)) {
    tmpvar_8 = (boxTex.x == 0.0);
  } else {
    tmpvar_8 = bool(0);
  };
  bool tmpvar_9;
  if (tmpvar_8) {
    tmpvar_9 = (boxTex.y == 0.0);
  } else {
    tmpvar_9 = bool(0);
  };
  bool tmpvar_10;
  if (tmpvar_9) {
    tmpvar_10 = (boxTex.z == 0.0);
  } else {
    tmpvar_10 = bool(0);
  };
  if (tmpvar_10) {
  } else {
    highp float tmpvar_11;
    tmpvar_11 = dot (tmpvar_3, vec3(0.0, 1.0, 0.0));
    if ((tmpvar_11 > 0.3)) {
      bool tmpvar_12;
      if ((tmpvar_6.z >= boxTex.w)) {
        tmpvar_12 = (tmpvar_6.z <= boxTex.x);
      } else {
        tmpvar_12 = bool(0);
      };
      bool tmpvar_13;
      if (tmpvar_12) {
        tmpvar_13 = bool(1);
      } else {
        bool tmpvar_14;
        if ((tmpvar_6.z >= boxTex.y)) {
          tmpvar_14 = (tmpvar_6.z <= boxTex.z);
        } else {
          tmpvar_14 = bool(0);
        };
        tmpvar_13 = tmpvar_14;
      };
      if (tmpvar_13) {
        tmpvar_4 = _Color.xyz;
        tmpvar_5 = _Color.w;
      };
    };
  };
  tmpvar_1 = tmpvar_4;
  tmpvar_2 = tmpvar_5;
  lowp vec4 c_i0;
  c_i0.xyz = ((tmpvar_4 * _LightColor0.xyz) * (max (0.0, dot (xlv_TEXCOORD2, _WorldSpaceLightPos0.xyz)) * 2.0));
  c_i0.w = tmpvar_5;
  c = c_i0;
  c.xyz = (c_i0.xyz + (tmpvar_4 * xlv_TEXCOORD3));
  c.w = tmpvar_5;
  gl_FragData[0] = c;
}



#endif"
}

SubProgram "opengl " {
Keywords { "DIRECTIONAL" "LIGHTMAP_ON" }
Bind "vertex" Vertex
Bind "normal" Normal
Bind "texcoord1" TexCoord1
Matrix 5 [_Object2World]
Vector 9 [unity_LightmapST]
"!!ARBvp1.0
# 11 ALU
PARAM c[10] = { program.local[0],
		state.matrix.mvp,
		program.local[5..9] };
MAD result.texcoord[2].xy, vertex.texcoord[1], c[9], c[9].zwzw;
DP4 result.position.w, vertex.position, c[4];
DP4 result.position.z, vertex.position, c[3];
DP4 result.position.y, vertex.position, c[2];
DP4 result.position.x, vertex.position, c[1];
DP3 result.texcoord[0].z, vertex.normal, c[7];
DP3 result.texcoord[0].y, vertex.normal, c[6];
DP3 result.texcoord[0].x, vertex.normal, c[5];
DP4 result.texcoord[1].z, vertex.position, c[7];
DP4 result.texcoord[1].y, vertex.position, c[6];
DP4 result.texcoord[1].x, vertex.position, c[5];
END
# 11 instructions, 0 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "DIRECTIONAL" "LIGHTMAP_ON" }
Bind "vertex" Vertex
Bind "normal" Normal
Bind "texcoord1" TexCoord1
Matrix 0 [glstate_matrix_mvp]
Matrix 4 [_Object2World]
Vector 8 [unity_LightmapST]
"vs_2_0
; 11 ALU
dcl_position0 v0
dcl_normal0 v1
dcl_texcoord1 v2
mad oT2.xy, v2, c8, c8.zwzw
dp4 oPos.w, v0, c3
dp4 oPos.z, v0, c2
dp4 oPos.y, v0, c1
dp4 oPos.x, v0, c0
dp3 oT0.z, v1, c6
dp3 oT0.y, v1, c5
dp3 oT0.x, v1, c4
dp4 oT1.z, v0, c6
dp4 oT1.y, v0, c5
dp4 oT1.x, v0, c4
"
}

SubProgram "gles " {
Keywords { "DIRECTIONAL" "LIGHTMAP_ON" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying highp vec2 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 unity_LightmapST;

uniform highp mat4 _Object2World;
attribute vec4 _glesMultiTexCoord1;
attribute vec3 _glesNormal;
attribute vec4 _glesVertex;
void main ()
{
  lowp vec3 tmpvar_1;
  mat3 tmpvar_2;
  tmpvar_2[0] = _Object2World[0].xyz;
  tmpvar_2[1] = _Object2World[1].xyz;
  tmpvar_2[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_3;
  tmpvar_3 = (tmpvar_2 * normalize (_glesNormal));
  tmpvar_1 = tmpvar_3;
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_TEXCOORD0 = tmpvar_1;
  xlv_TEXCOORD1 = (_Object2World * _glesVertex).xyz;
  xlv_TEXCOORD2 = ((_glesMultiTexCoord1.xy * unity_LightmapST.xy) + unity_LightmapST.zw);
}



#endif
#ifdef FRAGMENT

varying highp vec2 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform sampler2D unity_Lightmap;
uniform highp vec4 _MapWorldOrigin;
uniform highp vec4 _MapTileSize;
uniform highp vec4 _MapSizeInTiles;
uniform lowp vec4 _Color;
uniform sampler2D _Boxes;
void main ()
{
  lowp vec4 c;
  lowp vec3 tmpvar_1;
  lowp float tmpvar_2;
  highp vec3 tmpvar_3;
  tmpvar_3 = xlv_TEXCOORD0;
  tmpvar_1 = vec3(0.0, 0.0, 0.0);
  tmpvar_2 = 0.0;
  lowp vec3 tmpvar_4;
  lowp float tmpvar_5;
  tmpvar_4 = tmpvar_1;
  tmpvar_5 = tmpvar_2;
  highp vec4 boxTex;
  highp vec3 tmpvar_6;
  tmpvar_6 = (floor ((((xlv_TEXCOORD1 - _MapWorldOrigin.xyz) - vec3(-5.0, 0.01, -5.0)) / _MapTileSize.xyz)).xzy / _MapSizeInTiles.xzy);
  lowp vec4 tmpvar_7;
  tmpvar_7 = texture2D (_Boxes, tmpvar_6.xy);
  boxTex = tmpvar_7;
  bool tmpvar_8;
  if ((boxTex.w == 0.0)) {
    tmpvar_8 = (boxTex.x == 0.0);
  } else {
    tmpvar_8 = bool(0);
  };
  bool tmpvar_9;
  if (tmpvar_8) {
    tmpvar_9 = (boxTex.y == 0.0);
  } else {
    tmpvar_9 = bool(0);
  };
  bool tmpvar_10;
  if (tmpvar_9) {
    tmpvar_10 = (boxTex.z == 0.0);
  } else {
    tmpvar_10 = bool(0);
  };
  if (tmpvar_10) {
  } else {
    highp float tmpvar_11;
    tmpvar_11 = dot (tmpvar_3, vec3(0.0, 1.0, 0.0));
    if ((tmpvar_11 > 0.3)) {
      bool tmpvar_12;
      if ((tmpvar_6.z >= boxTex.w)) {
        tmpvar_12 = (tmpvar_6.z <= boxTex.x);
      } else {
        tmpvar_12 = bool(0);
      };
      bool tmpvar_13;
      if (tmpvar_12) {
        tmpvar_13 = bool(1);
      } else {
        bool tmpvar_14;
        if ((tmpvar_6.z >= boxTex.y)) {
          tmpvar_14 = (tmpvar_6.z <= boxTex.z);
        } else {
          tmpvar_14 = bool(0);
        };
        tmpvar_13 = tmpvar_14;
      };
      if (tmpvar_13) {
        tmpvar_4 = _Color.xyz;
        tmpvar_5 = _Color.w;
      };
    };
  };
  tmpvar_1 = tmpvar_4;
  tmpvar_2 = tmpvar_5;
  c = vec4(0.0, 0.0, 0.0, 0.0);
  c.xyz = (tmpvar_4 * (2.0 * texture2D (unity_Lightmap, xlv_TEXCOORD2).xyz));
  c.w = tmpvar_5;
  c.w = tmpvar_5;
  gl_FragData[0] = c;
}



#endif"
}

SubProgram "glesdesktop " {
Keywords { "DIRECTIONAL" "LIGHTMAP_ON" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying highp vec2 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 unity_LightmapST;

uniform highp mat4 _Object2World;
attribute vec4 _glesMultiTexCoord1;
attribute vec3 _glesNormal;
attribute vec4 _glesVertex;
void main ()
{
  lowp vec3 tmpvar_1;
  mat3 tmpvar_2;
  tmpvar_2[0] = _Object2World[0].xyz;
  tmpvar_2[1] = _Object2World[1].xyz;
  tmpvar_2[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_3;
  tmpvar_3 = (tmpvar_2 * normalize (_glesNormal));
  tmpvar_1 = tmpvar_3;
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_TEXCOORD0 = tmpvar_1;
  xlv_TEXCOORD1 = (_Object2World * _glesVertex).xyz;
  xlv_TEXCOORD2 = ((_glesMultiTexCoord1.xy * unity_LightmapST.xy) + unity_LightmapST.zw);
}



#endif
#ifdef FRAGMENT

varying highp vec2 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform sampler2D unity_Lightmap;
uniform highp vec4 _MapWorldOrigin;
uniform highp vec4 _MapTileSize;
uniform highp vec4 _MapSizeInTiles;
uniform lowp vec4 _Color;
uniform sampler2D _Boxes;
void main ()
{
  lowp vec4 c;
  lowp vec3 tmpvar_1;
  lowp float tmpvar_2;
  highp vec3 tmpvar_3;
  tmpvar_3 = xlv_TEXCOORD0;
  tmpvar_1 = vec3(0.0, 0.0, 0.0);
  tmpvar_2 = 0.0;
  lowp vec3 tmpvar_4;
  lowp float tmpvar_5;
  tmpvar_4 = tmpvar_1;
  tmpvar_5 = tmpvar_2;
  highp vec4 boxTex;
  highp vec3 tmpvar_6;
  tmpvar_6 = (floor ((((xlv_TEXCOORD1 - _MapWorldOrigin.xyz) - vec3(-5.0, 0.01, -5.0)) / _MapTileSize.xyz)).xzy / _MapSizeInTiles.xzy);
  lowp vec4 tmpvar_7;
  tmpvar_7 = texture2D (_Boxes, tmpvar_6.xy);
  boxTex = tmpvar_7;
  bool tmpvar_8;
  if ((boxTex.w == 0.0)) {
    tmpvar_8 = (boxTex.x == 0.0);
  } else {
    tmpvar_8 = bool(0);
  };
  bool tmpvar_9;
  if (tmpvar_8) {
    tmpvar_9 = (boxTex.y == 0.0);
  } else {
    tmpvar_9 = bool(0);
  };
  bool tmpvar_10;
  if (tmpvar_9) {
    tmpvar_10 = (boxTex.z == 0.0);
  } else {
    tmpvar_10 = bool(0);
  };
  if (tmpvar_10) {
  } else {
    highp float tmpvar_11;
    tmpvar_11 = dot (tmpvar_3, vec3(0.0, 1.0, 0.0));
    if ((tmpvar_11 > 0.3)) {
      bool tmpvar_12;
      if ((tmpvar_6.z >= boxTex.w)) {
        tmpvar_12 = (tmpvar_6.z <= boxTex.x);
      } else {
        tmpvar_12 = bool(0);
      };
      bool tmpvar_13;
      if (tmpvar_12) {
        tmpvar_13 = bool(1);
      } else {
        bool tmpvar_14;
        if ((tmpvar_6.z >= boxTex.y)) {
          tmpvar_14 = (tmpvar_6.z <= boxTex.z);
        } else {
          tmpvar_14 = bool(0);
        };
        tmpvar_13 = tmpvar_14;
      };
      if (tmpvar_13) {
        tmpvar_4 = _Color.xyz;
        tmpvar_5 = _Color.w;
      };
    };
  };
  tmpvar_1 = tmpvar_4;
  tmpvar_2 = tmpvar_5;
  c = vec4(0.0, 0.0, 0.0, 0.0);
  lowp vec4 tmpvar_15;
  tmpvar_15 = texture2D (unity_Lightmap, xlv_TEXCOORD2);
  c.xyz = (tmpvar_4 * ((8.0 * tmpvar_15.w) * tmpvar_15.xyz));
  c.w = tmpvar_5;
  c.w = tmpvar_5;
  gl_FragData[0] = c;
}



#endif"
}

SubProgram "opengl " {
Keywords { "DIRECTIONAL" "LIGHTMAP_OFF" "VERTEXLIGHT_ON" }
Bind "vertex" Vertex
Bind "normal" Normal
Vector 9 [unity_Scale]
Matrix 5 [_Object2World]
Vector 10 [unity_4LightPosX0]
Vector 11 [unity_4LightPosY0]
Vector 12 [unity_4LightPosZ0]
Vector 13 [unity_4LightAtten0]
Vector 14 [unity_LightColor0]
Vector 15 [unity_LightColor1]
Vector 16 [unity_LightColor2]
Vector 17 [unity_LightColor3]
Vector 18 [unity_SHAr]
Vector 19 [unity_SHAg]
Vector 20 [unity_SHAb]
Vector 21 [unity_SHBr]
Vector 22 [unity_SHBg]
Vector 23 [unity_SHBb]
Vector 24 [unity_SHC]
"!!ARBvp1.0
# 62 ALU
PARAM c[25] = { { 1, 0 },
		state.matrix.mvp,
		program.local[5..24] };
TEMP R0;
TEMP R1;
TEMP R2;
TEMP R3;
TEMP R4;
TEMP R5;
MUL R3.xyz, vertex.normal, c[9].w;
DP3 R5.x, R3, c[5];
DP4 R4.zw, vertex.position, c[6];
ADD R2, -R4.z, c[11];
DP3 R4.z, R3, c[6];
DP3 R3.z, R3, c[7];
DP4 R3.w, vertex.position, c[5];
MUL R0, R4.z, R2;
ADD R1, -R3.w, c[10];
DP4 R4.xy, vertex.position, c[7];
MUL R2, R2, R2;
MOV R5.y, R4.z;
MOV R5.z, R3;
MOV R5.w, c[0].x;
MAD R0, R5.x, R1, R0;
MAD R2, R1, R1, R2;
ADD R1, -R4.x, c[12];
MAD R2, R1, R1, R2;
MAD R0, R3.z, R1, R0;
MUL R1, R2, c[13];
ADD R1, R1, c[0].x;
RSQ R2.x, R2.x;
RSQ R2.y, R2.y;
RSQ R2.z, R2.z;
RSQ R2.w, R2.w;
MUL R0, R0, R2;
DP4 R2.z, R5, c[20];
DP4 R2.y, R5, c[19];
DP4 R2.x, R5, c[18];
RCP R1.x, R1.x;
RCP R1.y, R1.y;
RCP R1.w, R1.w;
RCP R1.z, R1.z;
MAX R0, R0, c[0].y;
MUL R0, R0, R1;
MUL R1.xyz, R0.y, c[15];
MAD R1.xyz, R0.x, c[14], R1;
MAD R0.xyz, R0.z, c[16], R1;
MAD R1.xyz, R0.w, c[17], R0;
MUL R0, R5.xyzz, R5.yzzx;
MUL R1.w, R4.z, R4.z;
DP4 R5.w, R0, c[23];
DP4 R5.z, R0, c[22];
DP4 R5.y, R0, c[21];
MAD R1.w, R5.x, R5.x, -R1;
MUL R0.xyz, R1.w, c[24];
ADD R2.xyz, R2, R5.yzww;
ADD R0.xyz, R2, R0;
MOV R3.x, R4.w;
MOV R3.y, R4;
ADD result.texcoord[3].xyz, R0, R1;
MOV result.texcoord[1].xyz, R3.wxyw;
MOV result.texcoord[2].z, R3;
MOV result.texcoord[2].y, R4.z;
MOV result.texcoord[2].x, R5;
DP4 result.position.w, vertex.position, c[4];
DP4 result.position.z, vertex.position, c[3];
DP4 result.position.y, vertex.position, c[2];
DP4 result.position.x, vertex.position, c[1];
DP3 result.texcoord[0].z, vertex.normal, c[7];
DP3 result.texcoord[0].y, vertex.normal, c[6];
DP3 result.texcoord[0].x, vertex.normal, c[5];
END
# 62 instructions, 6 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "DIRECTIONAL" "LIGHTMAP_OFF" "VERTEXLIGHT_ON" }
Bind "vertex" Vertex
Bind "normal" Normal
Matrix 0 [glstate_matrix_mvp]
Vector 8 [unity_Scale]
Matrix 4 [_Object2World]
Vector 9 [unity_4LightPosX0]
Vector 10 [unity_4LightPosY0]
Vector 11 [unity_4LightPosZ0]
Vector 12 [unity_4LightAtten0]
Vector 13 [unity_LightColor0]
Vector 14 [unity_LightColor1]
Vector 15 [unity_LightColor2]
Vector 16 [unity_LightColor3]
Vector 17 [unity_SHAr]
Vector 18 [unity_SHAg]
Vector 19 [unity_SHAb]
Vector 20 [unity_SHBr]
Vector 21 [unity_SHBg]
Vector 22 [unity_SHBb]
Vector 23 [unity_SHC]
"vs_2_0
; 62 ALU
def c24, 1.00000000, 0.00000000, 0, 0
dcl_position0 v0
dcl_normal0 v1
mul r3.xyz, v1, c8.w
dp3 r5.x, r3, c4
dp4 r4.zw, v0, c5
add r2, -r4.z, c10
dp3 r4.z, r3, c5
dp3 r3.z, r3, c6
dp4 r3.w, v0, c4
mul r0, r4.z, r2
add r1, -r3.w, c9
dp4 r4.xy, v0, c6
mul r2, r2, r2
mov r5.y, r4.z
mov r5.z, r3
mov r5.w, c24.x
mad r0, r5.x, r1, r0
mad r2, r1, r1, r2
add r1, -r4.x, c11
mad r2, r1, r1, r2
mad r0, r3.z, r1, r0
mul r1, r2, c12
add r1, r1, c24.x
rsq r2.x, r2.x
rsq r2.y, r2.y
rsq r2.z, r2.z
rsq r2.w, r2.w
mul r0, r0, r2
dp4 r2.z, r5, c19
dp4 r2.y, r5, c18
dp4 r2.x, r5, c17
rcp r1.x, r1.x
rcp r1.y, r1.y
rcp r1.w, r1.w
rcp r1.z, r1.z
max r0, r0, c24.y
mul r0, r0, r1
mul r1.xyz, r0.y, c14
mad r1.xyz, r0.x, c13, r1
mad r0.xyz, r0.z, c15, r1
mad r1.xyz, r0.w, c16, r0
mul r0, r5.xyzz, r5.yzzx
mul r1.w, r4.z, r4.z
dp4 r5.w, r0, c22
dp4 r5.z, r0, c21
dp4 r5.y, r0, c20
mad r1.w, r5.x, r5.x, -r1
mul r0.xyz, r1.w, c23
add r2.xyz, r2, r5.yzww
add r0.xyz, r2, r0
mov r3.x, r4.w
mov r3.y, r4
add oT3.xyz, r0, r1
mov oT1.xyz, r3.wxyw
mov oT2.z, r3
mov oT2.y, r4.z
mov oT2.x, r5
dp4 oPos.w, v0, c3
dp4 oPos.z, v0, c2
dp4 oPos.y, v0, c1
dp4 oPos.x, v0, c0
dp3 oT0.z, v1, c6
dp3 oT0.y, v1, c5
dp3 oT0.x, v1, c4
"
}

SubProgram "gles " {
Keywords { "DIRECTIONAL" "LIGHTMAP_OFF" "VERTEXLIGHT_ON" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying lowp vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 unity_Scale;
uniform highp vec4 unity_SHC;
uniform highp vec4 unity_SHBr;
uniform highp vec4 unity_SHBg;
uniform highp vec4 unity_SHBb;
uniform highp vec4 unity_SHAr;
uniform highp vec4 unity_SHAg;
uniform highp vec4 unity_SHAb;
uniform vec4 unity_LightColor[4];
uniform highp vec4 unity_4LightPosZ0;
uniform highp vec4 unity_4LightPosY0;
uniform highp vec4 unity_4LightPosX0;
uniform highp vec4 unity_4LightAtten0;

uniform highp mat4 _Object2World;
attribute vec3 _glesNormal;
attribute vec4 _glesVertex;
void main ()
{
  vec3 tmpvar_1;
  tmpvar_1 = normalize (_glesNormal);
  highp vec3 shlight;
  lowp vec3 tmpvar_2;
  lowp vec3 tmpvar_3;
  lowp vec3 tmpvar_4;
  mat3 tmpvar_5;
  tmpvar_5[0] = _Object2World[0].xyz;
  tmpvar_5[1] = _Object2World[1].xyz;
  tmpvar_5[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_6;
  tmpvar_6 = (tmpvar_5 * tmpvar_1);
  tmpvar_2 = tmpvar_6;
  mat3 tmpvar_7;
  tmpvar_7[0] = _Object2World[0].xyz;
  tmpvar_7[1] = _Object2World[1].xyz;
  tmpvar_7[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_8;
  tmpvar_8 = (tmpvar_7 * (tmpvar_1 * unity_Scale.w));
  tmpvar_3 = tmpvar_8;
  highp vec4 tmpvar_9;
  tmpvar_9.w = 1.0;
  tmpvar_9.xyz = tmpvar_8;
  mediump vec3 tmpvar_10;
  mediump vec4 normal;
  normal = tmpvar_9;
  mediump vec3 x3;
  highp float vC;
  mediump vec3 x2;
  mediump vec3 x1;
  highp float tmpvar_11;
  tmpvar_11 = dot (unity_SHAr, normal);
  x1.x = tmpvar_11;
  highp float tmpvar_12;
  tmpvar_12 = dot (unity_SHAg, normal);
  x1.y = tmpvar_12;
  highp float tmpvar_13;
  tmpvar_13 = dot (unity_SHAb, normal);
  x1.z = tmpvar_13;
  mediump vec4 tmpvar_14;
  tmpvar_14 = (normal.xyzz * normal.yzzx);
  highp float tmpvar_15;
  tmpvar_15 = dot (unity_SHBr, tmpvar_14);
  x2.x = tmpvar_15;
  highp float tmpvar_16;
  tmpvar_16 = dot (unity_SHBg, tmpvar_14);
  x2.y = tmpvar_16;
  highp float tmpvar_17;
  tmpvar_17 = dot (unity_SHBb, tmpvar_14);
  x2.z = tmpvar_17;
  mediump float tmpvar_18;
  tmpvar_18 = ((normal.x * normal.x) - (normal.y * normal.y));
  vC = tmpvar_18;
  highp vec3 tmpvar_19;
  tmpvar_19 = (unity_SHC.xyz * vC);
  x3 = tmpvar_19;
  tmpvar_10 = ((x1 + x2) + x3);
  shlight = tmpvar_10;
  tmpvar_4 = shlight;
  highp vec3 tmpvar_20;
  tmpvar_20 = (_Object2World * _glesVertex).xyz;
  highp vec4 tmpvar_21;
  tmpvar_21 = (unity_4LightPosX0 - tmpvar_20.x);
  highp vec4 tmpvar_22;
  tmpvar_22 = (unity_4LightPosY0 - tmpvar_20.y);
  highp vec4 tmpvar_23;
  tmpvar_23 = (unity_4LightPosZ0 - tmpvar_20.z);
  highp vec4 tmpvar_24;
  tmpvar_24 = (((tmpvar_21 * tmpvar_21) + (tmpvar_22 * tmpvar_22)) + (tmpvar_23 * tmpvar_23));
  highp vec4 tmpvar_25;
  tmpvar_25 = (max (vec4(0.0, 0.0, 0.0, 0.0), ((((tmpvar_21 * tmpvar_8.x) + (tmpvar_22 * tmpvar_8.y)) + (tmpvar_23 * tmpvar_8.z)) * inversesqrt (tmpvar_24))) * 1.0/((1.0 + (tmpvar_24 * unity_4LightAtten0))));
  highp vec3 tmpvar_26;
  tmpvar_26 = (tmpvar_4 + ((((unity_LightColor[0].xyz * tmpvar_25.x) + (unity_LightColor[1].xyz * tmpvar_25.y)) + (unity_LightColor[2].xyz * tmpvar_25.z)) + (unity_LightColor[3].xyz * tmpvar_25.w)));
  tmpvar_4 = tmpvar_26;
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_TEXCOORD0 = tmpvar_2;
  xlv_TEXCOORD1 = (_Object2World * _glesVertex).xyz;
  xlv_TEXCOORD2 = tmpvar_3;
  xlv_TEXCOORD3 = tmpvar_4;
}



#endif
#ifdef FRAGMENT

varying lowp vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform lowp vec4 _WorldSpaceLightPos0;
uniform highp vec4 _MapWorldOrigin;
uniform highp vec4 _MapTileSize;
uniform highp vec4 _MapSizeInTiles;
uniform lowp vec4 _LightColor0;
uniform lowp vec4 _Color;
uniform sampler2D _Boxes;
void main ()
{
  lowp vec4 c;
  lowp vec3 tmpvar_1;
  lowp float tmpvar_2;
  highp vec3 tmpvar_3;
  tmpvar_3 = xlv_TEXCOORD0;
  tmpvar_1 = vec3(0.0, 0.0, 0.0);
  tmpvar_2 = 0.0;
  lowp vec3 tmpvar_4;
  lowp float tmpvar_5;
  tmpvar_4 = tmpvar_1;
  tmpvar_5 = tmpvar_2;
  highp vec4 boxTex;
  highp vec3 tmpvar_6;
  tmpvar_6 = (floor ((((xlv_TEXCOORD1 - _MapWorldOrigin.xyz) - vec3(-5.0, 0.01, -5.0)) / _MapTileSize.xyz)).xzy / _MapSizeInTiles.xzy);
  lowp vec4 tmpvar_7;
  tmpvar_7 = texture2D (_Boxes, tmpvar_6.xy);
  boxTex = tmpvar_7;
  bool tmpvar_8;
  if ((boxTex.w == 0.0)) {
    tmpvar_8 = (boxTex.x == 0.0);
  } else {
    tmpvar_8 = bool(0);
  };
  bool tmpvar_9;
  if (tmpvar_8) {
    tmpvar_9 = (boxTex.y == 0.0);
  } else {
    tmpvar_9 = bool(0);
  };
  bool tmpvar_10;
  if (tmpvar_9) {
    tmpvar_10 = (boxTex.z == 0.0);
  } else {
    tmpvar_10 = bool(0);
  };
  if (tmpvar_10) {
  } else {
    highp float tmpvar_11;
    tmpvar_11 = dot (tmpvar_3, vec3(0.0, 1.0, 0.0));
    if ((tmpvar_11 > 0.3)) {
      bool tmpvar_12;
      if ((tmpvar_6.z >= boxTex.w)) {
        tmpvar_12 = (tmpvar_6.z <= boxTex.x);
      } else {
        tmpvar_12 = bool(0);
      };
      bool tmpvar_13;
      if (tmpvar_12) {
        tmpvar_13 = bool(1);
      } else {
        bool tmpvar_14;
        if ((tmpvar_6.z >= boxTex.y)) {
          tmpvar_14 = (tmpvar_6.z <= boxTex.z);
        } else {
          tmpvar_14 = bool(0);
        };
        tmpvar_13 = tmpvar_14;
      };
      if (tmpvar_13) {
        tmpvar_4 = _Color.xyz;
        tmpvar_5 = _Color.w;
      };
    };
  };
  tmpvar_1 = tmpvar_4;
  tmpvar_2 = tmpvar_5;
  lowp vec4 c_i0;
  c_i0.xyz = ((tmpvar_4 * _LightColor0.xyz) * (max (0.0, dot (xlv_TEXCOORD2, _WorldSpaceLightPos0.xyz)) * 2.0));
  c_i0.w = tmpvar_5;
  c = c_i0;
  c.xyz = (c_i0.xyz + (tmpvar_4 * xlv_TEXCOORD3));
  c.w = tmpvar_5;
  gl_FragData[0] = c;
}



#endif"
}

SubProgram "glesdesktop " {
Keywords { "DIRECTIONAL" "LIGHTMAP_OFF" "VERTEXLIGHT_ON" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying lowp vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 unity_Scale;
uniform highp vec4 unity_SHC;
uniform highp vec4 unity_SHBr;
uniform highp vec4 unity_SHBg;
uniform highp vec4 unity_SHBb;
uniform highp vec4 unity_SHAr;
uniform highp vec4 unity_SHAg;
uniform highp vec4 unity_SHAb;
uniform vec4 unity_LightColor[4];
uniform highp vec4 unity_4LightPosZ0;
uniform highp vec4 unity_4LightPosY0;
uniform highp vec4 unity_4LightPosX0;
uniform highp vec4 unity_4LightAtten0;

uniform highp mat4 _Object2World;
attribute vec3 _glesNormal;
attribute vec4 _glesVertex;
void main ()
{
  vec3 tmpvar_1;
  tmpvar_1 = normalize (_glesNormal);
  highp vec3 shlight;
  lowp vec3 tmpvar_2;
  lowp vec3 tmpvar_3;
  lowp vec3 tmpvar_4;
  mat3 tmpvar_5;
  tmpvar_5[0] = _Object2World[0].xyz;
  tmpvar_5[1] = _Object2World[1].xyz;
  tmpvar_5[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_6;
  tmpvar_6 = (tmpvar_5 * tmpvar_1);
  tmpvar_2 = tmpvar_6;
  mat3 tmpvar_7;
  tmpvar_7[0] = _Object2World[0].xyz;
  tmpvar_7[1] = _Object2World[1].xyz;
  tmpvar_7[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_8;
  tmpvar_8 = (tmpvar_7 * (tmpvar_1 * unity_Scale.w));
  tmpvar_3 = tmpvar_8;
  highp vec4 tmpvar_9;
  tmpvar_9.w = 1.0;
  tmpvar_9.xyz = tmpvar_8;
  mediump vec3 tmpvar_10;
  mediump vec4 normal;
  normal = tmpvar_9;
  mediump vec3 x3;
  highp float vC;
  mediump vec3 x2;
  mediump vec3 x1;
  highp float tmpvar_11;
  tmpvar_11 = dot (unity_SHAr, normal);
  x1.x = tmpvar_11;
  highp float tmpvar_12;
  tmpvar_12 = dot (unity_SHAg, normal);
  x1.y = tmpvar_12;
  highp float tmpvar_13;
  tmpvar_13 = dot (unity_SHAb, normal);
  x1.z = tmpvar_13;
  mediump vec4 tmpvar_14;
  tmpvar_14 = (normal.xyzz * normal.yzzx);
  highp float tmpvar_15;
  tmpvar_15 = dot (unity_SHBr, tmpvar_14);
  x2.x = tmpvar_15;
  highp float tmpvar_16;
  tmpvar_16 = dot (unity_SHBg, tmpvar_14);
  x2.y = tmpvar_16;
  highp float tmpvar_17;
  tmpvar_17 = dot (unity_SHBb, tmpvar_14);
  x2.z = tmpvar_17;
  mediump float tmpvar_18;
  tmpvar_18 = ((normal.x * normal.x) - (normal.y * normal.y));
  vC = tmpvar_18;
  highp vec3 tmpvar_19;
  tmpvar_19 = (unity_SHC.xyz * vC);
  x3 = tmpvar_19;
  tmpvar_10 = ((x1 + x2) + x3);
  shlight = tmpvar_10;
  tmpvar_4 = shlight;
  highp vec3 tmpvar_20;
  tmpvar_20 = (_Object2World * _glesVertex).xyz;
  highp vec4 tmpvar_21;
  tmpvar_21 = (unity_4LightPosX0 - tmpvar_20.x);
  highp vec4 tmpvar_22;
  tmpvar_22 = (unity_4LightPosY0 - tmpvar_20.y);
  highp vec4 tmpvar_23;
  tmpvar_23 = (unity_4LightPosZ0 - tmpvar_20.z);
  highp vec4 tmpvar_24;
  tmpvar_24 = (((tmpvar_21 * tmpvar_21) + (tmpvar_22 * tmpvar_22)) + (tmpvar_23 * tmpvar_23));
  highp vec4 tmpvar_25;
  tmpvar_25 = (max (vec4(0.0, 0.0, 0.0, 0.0), ((((tmpvar_21 * tmpvar_8.x) + (tmpvar_22 * tmpvar_8.y)) + (tmpvar_23 * tmpvar_8.z)) * inversesqrt (tmpvar_24))) * 1.0/((1.0 + (tmpvar_24 * unity_4LightAtten0))));
  highp vec3 tmpvar_26;
  tmpvar_26 = (tmpvar_4 + ((((unity_LightColor[0].xyz * tmpvar_25.x) + (unity_LightColor[1].xyz * tmpvar_25.y)) + (unity_LightColor[2].xyz * tmpvar_25.z)) + (unity_LightColor[3].xyz * tmpvar_25.w)));
  tmpvar_4 = tmpvar_26;
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_TEXCOORD0 = tmpvar_2;
  xlv_TEXCOORD1 = (_Object2World * _glesVertex).xyz;
  xlv_TEXCOORD2 = tmpvar_3;
  xlv_TEXCOORD3 = tmpvar_4;
}



#endif
#ifdef FRAGMENT

varying lowp vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform lowp vec4 _WorldSpaceLightPos0;
uniform highp vec4 _MapWorldOrigin;
uniform highp vec4 _MapTileSize;
uniform highp vec4 _MapSizeInTiles;
uniform lowp vec4 _LightColor0;
uniform lowp vec4 _Color;
uniform sampler2D _Boxes;
void main ()
{
  lowp vec4 c;
  lowp vec3 tmpvar_1;
  lowp float tmpvar_2;
  highp vec3 tmpvar_3;
  tmpvar_3 = xlv_TEXCOORD0;
  tmpvar_1 = vec3(0.0, 0.0, 0.0);
  tmpvar_2 = 0.0;
  lowp vec3 tmpvar_4;
  lowp float tmpvar_5;
  tmpvar_4 = tmpvar_1;
  tmpvar_5 = tmpvar_2;
  highp vec4 boxTex;
  highp vec3 tmpvar_6;
  tmpvar_6 = (floor ((((xlv_TEXCOORD1 - _MapWorldOrigin.xyz) - vec3(-5.0, 0.01, -5.0)) / _MapTileSize.xyz)).xzy / _MapSizeInTiles.xzy);
  lowp vec4 tmpvar_7;
  tmpvar_7 = texture2D (_Boxes, tmpvar_6.xy);
  boxTex = tmpvar_7;
  bool tmpvar_8;
  if ((boxTex.w == 0.0)) {
    tmpvar_8 = (boxTex.x == 0.0);
  } else {
    tmpvar_8 = bool(0);
  };
  bool tmpvar_9;
  if (tmpvar_8) {
    tmpvar_9 = (boxTex.y == 0.0);
  } else {
    tmpvar_9 = bool(0);
  };
  bool tmpvar_10;
  if (tmpvar_9) {
    tmpvar_10 = (boxTex.z == 0.0);
  } else {
    tmpvar_10 = bool(0);
  };
  if (tmpvar_10) {
  } else {
    highp float tmpvar_11;
    tmpvar_11 = dot (tmpvar_3, vec3(0.0, 1.0, 0.0));
    if ((tmpvar_11 > 0.3)) {
      bool tmpvar_12;
      if ((tmpvar_6.z >= boxTex.w)) {
        tmpvar_12 = (tmpvar_6.z <= boxTex.x);
      } else {
        tmpvar_12 = bool(0);
      };
      bool tmpvar_13;
      if (tmpvar_12) {
        tmpvar_13 = bool(1);
      } else {
        bool tmpvar_14;
        if ((tmpvar_6.z >= boxTex.y)) {
          tmpvar_14 = (tmpvar_6.z <= boxTex.z);
        } else {
          tmpvar_14 = bool(0);
        };
        tmpvar_13 = tmpvar_14;
      };
      if (tmpvar_13) {
        tmpvar_4 = _Color.xyz;
        tmpvar_5 = _Color.w;
      };
    };
  };
  tmpvar_1 = tmpvar_4;
  tmpvar_2 = tmpvar_5;
  lowp vec4 c_i0;
  c_i0.xyz = ((tmpvar_4 * _LightColor0.xyz) * (max (0.0, dot (xlv_TEXCOORD2, _WorldSpaceLightPos0.xyz)) * 2.0));
  c_i0.w = tmpvar_5;
  c = c_i0;
  c.xyz = (c_i0.xyz + (tmpvar_4 * xlv_TEXCOORD3));
  c.w = tmpvar_5;
  gl_FragData[0] = c;
}



#endif"
}

}
Program "fp" {
// Fragment combos: 2
//   opengl - ALU: 40 to 42, TEX: 1 to 2
//   d3d9 - ALU: 47 to 51, TEX: 1 to 2
SubProgram "opengl " {
Keywords { "DIRECTIONAL" "LIGHTMAP_OFF" }
Vector 0 [_WorldSpaceLightPos0]
Vector 1 [_LightColor0]
Vector 2 [_MapWorldOrigin]
Vector 3 [_MapTileSize]
Vector 4 [_MapSizeInTiles]
Vector 5 [_Color]
SetTexture 0 [_Boxes] 2D
"!!ARBfp1.0
OPTION ARB_precision_hint_fastest;
# 42 ALU, 1 TEX
PARAM c[8] = { program.local[0..5],
		{ 1, 0, -5, 0.0099999998 },
		{ 0.30000001, 2 } };
TEMP R0;
TEMP R1;
ADD R0.xyz, fragment.texcoord[1], -c[2];
ADD R1.xyz, R0, -c[6].zwzw;
RCP R0.x, c[3].x;
RCP R0.z, c[3].z;
RCP R0.y, c[3].y;
MUL R0.xyz, R1, R0;
FLR R1.xyz, R0;
RCP R0.x, c[4].x;
RCP R0.y, c[4].z;
RCP R0.z, c[4].y;
MUL R1.xyz, R1.xzyw, R0;
TEX R0, R1, texture[0], 2D;
SGE R1.y, R0.z, R1.z;
SGE R1.x, R1.z, R0.y;
MUL R1.w, R1.x, R1.y;
ABS R0.y, R0;
ABS R0.z, R0;
SGE R1.y, R0.x, R1.z;
SGE R1.x, R1.z, R0.w;
MAD_SAT R1.x, R1, R1.y, R1.w;
ABS R1.y, R0.x;
ABS R0.x, R0.w;
DP3 R1.w, fragment.texcoord[2], c[0];
CMP R0.w, -R1.y, c[6].y, c[6].x;
CMP R0.x, -R0, c[6].y, c[6];
MUL R0.x, R0, R0.w;
CMP R0.y, -R0, c[6], c[6].x;
MUL R0.x, R0, R0.y;
CMP R0.z, -R0, c[6].y, c[6].x;
MUL R0.x, R0, R0.z;
CMP R0.x, -R0, c[6].y, c[6];
SLT R0.y, c[7].x, fragment.texcoord[0];
MUL R0.y, R0.x, R0;
MUL R0.y, R0, R1.x;
MOV R0.x, c[6].y;
CMP R0, -R0.y, c[5], R0.x;
MUL R1.xyz, R0, fragment.texcoord[3];
MUL R0.xyz, R0, c[1];
MAX R1.w, R1, c[6].y;
MUL R0.xyz, R1.w, R0;
MAD result.color.xyz, R0, c[7].y, R1;
MOV result.color.w, R0;
END
# 42 instructions, 2 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "DIRECTIONAL" "LIGHTMAP_OFF" }
Vector 0 [_WorldSpaceLightPos0]
Vector 1 [_LightColor0]
Vector 2 [_MapWorldOrigin]
Vector 3 [_MapTileSize]
Vector 4 [_MapSizeInTiles]
Vector 5 [_Color]
SetTexture 0 [_Boxes] 2D
"ps_2_0
; 51 ALU, 1 TEX
dcl_2d s0
def c6, 5.00000000, -0.01000000, 1.00000000, 0.00000000
def c7, 0.30000001, 2.00000000, 0, 0
dcl t0.xy
dcl t1.xyz
dcl t2.xyz
dcl t3.xyz
mov r0.xz, c6.x
mov r0.y, c6
add r1.xyz, t1, -c2
add r1.xyz, r1, r0
rcp r0.x, c3.x
rcp r0.z, c3.z
rcp r0.y, c3.y
mul r0.xyz, r1, r0
frc r1.xyz, r0
add r0.xyz, r0, -r1
rcp r1.x, c4.x
rcp r1.y, c4.z
rcp r1.z, c4.y
mov r0.yz, r0.wzyx
mul r0.xyz, r0, r1
texld r3, r0, s0
add r2.x, r3, -r0.z
add r1.x, r3.z, -r0.z
add r0.x, -r3.y, r0.z
cmp r1.x, r1, c6.z, c6.w
cmp r0.x, r0, c6.z, c6.w
mul_pp r0.x, r0, r1
add r1.x, -r3.w, r0.z
cmp r1.x, r1, c6.z, c6.w
cmp r2.x, r2, c6.z, c6.w
mad_pp_sat r0.x, r1, r2, r0
abs r2.x, r3
abs r1.x, r3.y
abs r3.x, r3.w
cmp r3.x, -r3, c6.z, c6.w
cmp r2.x, -r2, c6.z, c6.w
mul_pp r2.x, r3, r2
cmp r1.x, -r1, c6.z, c6.w
mul_pp r1.x, r2, r1
abs r3.x, r3.z
cmp r3.x, -r3, c6.z, c6.w
add r2.x, -t0.y, c7
mul_pp r1.x, r1, r3
cmp r2.x, r2, c6.w, c6.z
cmp_pp r1.x, -r1, c6.z, c6.w
mul_pp r1.x, r1, r2
mul_pp r0.x, r1, r0
mov_pp r2, c5
cmp_pp r1, -r0.x, c6.w, r2
mul_pp r2.xyz, r1, t3
dp3_pp r0.x, t2, c0
mul_pp r1.xyz, r1, c1
max_pp r0.x, r0, c6.w
mul_pp r0.xyz, r0.x, r1
mad_pp r0.xyz, r0, c7.y, r2
mov_pp r0.w, r1
mov_pp oC0, r0
"
}

SubProgram "gles " {
Keywords { "DIRECTIONAL" "LIGHTMAP_OFF" }
"!!GLES"
}

SubProgram "glesdesktop " {
Keywords { "DIRECTIONAL" "LIGHTMAP_OFF" }
"!!GLES"
}

SubProgram "opengl " {
Keywords { "DIRECTIONAL" "LIGHTMAP_ON" }
Vector 0 [_MapWorldOrigin]
Vector 1 [_MapTileSize]
Vector 2 [_MapSizeInTiles]
Vector 3 [_Color]
SetTexture 0 [_Boxes] 2D
SetTexture 1 [unity_Lightmap] 2D
"!!ARBfp1.0
OPTION ARB_precision_hint_fastest;
# 40 ALU, 2 TEX
PARAM c[6] = { program.local[0..3],
		{ 1, 0, -5, 0.0099999998 },
		{ 0.30000001, 8 } };
TEMP R0;
TEMP R1;
TEMP R2;
ADD R0.xyz, fragment.texcoord[1], -c[0];
ADD R1.xyz, R0, -c[4].zwzw;
RCP R0.x, c[1].x;
RCP R0.z, c[1].z;
RCP R0.y, c[1].y;
MUL R0.xyz, R1, R0;
FLR R1.xyz, R0;
RCP R0.x, c[2].x;
RCP R0.y, c[2].z;
RCP R0.z, c[2].y;
MUL R2.xyz, R1.xzyw, R0;
TEX R0, R2, texture[0], 2D;
TEX R1, fragment.texcoord[2], texture[1], 2D;
SGE R2.y, R0.z, R2.z;
SGE R2.x, R2.z, R0.y;
MUL R2.w, R2.x, R2.y;
ABS R0.y, R0;
ABS R0.z, R0;
SGE R2.y, R0.x, R2.z;
SGE R2.x, R2.z, R0.w;
MAD_SAT R2.x, R2, R2.y, R2.w;
ABS R2.y, R0.x;
ABS R0.x, R0.w;
CMP R0.w, -R2.y, c[4].y, c[4].x;
CMP R0.x, -R0, c[4].y, c[4];
MUL R0.x, R0, R0.w;
CMP R0.y, -R0, c[4], c[4].x;
MUL R0.x, R0, R0.y;
CMP R0.z, -R0, c[4].y, c[4].x;
MUL R0.x, R0, R0.z;
CMP R0.x, -R0, c[4].y, c[4];
SLT R0.y, c[5].x, fragment.texcoord[0];
MUL R0.y, R0.x, R0;
MOV R0.x, c[4].y;
MUL R0.y, R0, R2.x;
CMP R0, -R0.y, c[3], R0.x;
MUL R1.xyz, R1.w, R1;
MUL R0.xyz, R1, R0;
MOV result.color.w, R0;
MUL result.color.xyz, R0, c[5].y;
END
# 40 instructions, 3 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "DIRECTIONAL" "LIGHTMAP_ON" }
Vector 0 [_MapWorldOrigin]
Vector 1 [_MapTileSize]
Vector 2 [_MapSizeInTiles]
Vector 3 [_Color]
SetTexture 0 [_Boxes] 2D
SetTexture 1 [unity_Lightmap] 2D
"ps_2_0
; 47 ALU, 2 TEX
dcl_2d s0
dcl_2d s1
def c4, 5.00000000, -0.01000000, 1.00000000, 0.00000000
def c5, 0.30000001, 8.00000000, 0, 0
dcl t0.xy
dcl t1.xyz
dcl t2.xy
texld r3, t2, s1
mov r0.xz, c4.x
mov r0.y, c4
add r1.xyz, t1, -c0
add r1.xyz, r1, r0
rcp r0.x, c1.x
rcp r0.z, c1.z
rcp r0.y, c1.y
mul r0.xyz, r1, r0
frc r1.xyz, r0
add r0.xyz, r0, -r1
rcp r1.x, c2.x
rcp r1.y, c2.z
rcp r1.z, c2.y
mov r0.yz, r0.wzyx
mul r0.xyz, r0, r1
texld r4, r0, s0
add r0.x, r4.z, -r0.z
add r1.x, -r4.y, r0.z
add r2.x, -r4.w, r0.z
cmp r1.x, r1, c4.z, c4.w
cmp r0.x, r0, c4.z, c4.w
mul_pp r0.x, r1, r0
add r1.x, r4, -r0.z
cmp r2.x, r2, c4.z, c4.w
cmp r1.x, r1, c4.z, c4.w
mad_pp_sat r0.x, r2, r1, r0
abs r2.x, r4
abs r1.x, r4.y
abs r4.x, r4.w
cmp r4.x, -r4, c4.z, c4.w
cmp r2.x, -r2, c4.z, c4.w
mul_pp r2.x, r4, r2
cmp r1.x, -r1, c4.z, c4.w
mul_pp r1.x, r2, r1
abs r4.x, r4.z
cmp r4.x, -r4, c4.z, c4.w
add r2.x, -t0.y, c5
mul_pp r1.x, r1, r4
cmp r2.x, r2, c4.w, c4.z
cmp_pp r1.x, -r1, c4.z, c4.w
mul_pp r1.x, r1, r2
mul_pp r0.x, r1, r0
mov_pp r1, c3
cmp_pp r0, -r0.x, c4.w, r1
mul_pp r1.xyz, r3.w, r3
mul_pp r0.xyz, r1, r0
mul_pp r0.xyz, r0, c5.y
mov_pp oC0, r0
"
}

SubProgram "gles " {
Keywords { "DIRECTIONAL" "LIGHTMAP_ON" }
"!!GLES"
}

SubProgram "glesdesktop " {
Keywords { "DIRECTIONAL" "LIGHTMAP_ON" }
"!!GLES"
}

}
	}
	Pass {
		Name "FORWARD"
		Tags { "LightMode" = "ForwardAdd" }
		ZWrite Off Blend One One Fog { Color (0,0,0,0) }
		Blend SrcAlpha One
Program "vp" {
// Vertex combos: 5
//   opengl - ALU: 15 to 22
//   d3d9 - ALU: 15 to 22
SubProgram "opengl " {
Keywords { "POINT" }
Bind "vertex" Vertex
Bind "normal" Normal
Vector 13 [unity_Scale]
Vector 14 [_WorldSpaceLightPos0]
Matrix 5 [_Object2World]
Matrix 9 [_LightMatrix0]
"!!ARBvp1.0
# 21 ALU
PARAM c[15] = { program.local[0],
		state.matrix.mvp,
		program.local[5..14] };
TEMP R0;
TEMP R1;
DP4 R1.z, vertex.position, c[7];
DP4 R1.x, vertex.position, c[5];
DP4 R1.y, vertex.position, c[6];
MOV R0.xyz, R1;
DP4 R0.w, vertex.position, c[8];
DP4 result.texcoord[4].z, R0, c[11];
DP4 result.texcoord[4].y, R0, c[10];
DP4 result.texcoord[4].x, R0, c[9];
MUL R0.xyz, vertex.normal, c[13].w;
MOV result.texcoord[1].xyz, R1;
DP3 result.texcoord[2].z, R0, c[7];
DP3 result.texcoord[2].y, R0, c[6];
DP3 result.texcoord[2].x, R0, c[5];
ADD result.texcoord[3].xyz, -R1, c[14];
DP4 result.position.w, vertex.position, c[4];
DP4 result.position.z, vertex.position, c[3];
DP4 result.position.y, vertex.position, c[2];
DP4 result.position.x, vertex.position, c[1];
DP3 result.texcoord[0].z, vertex.normal, c[7];
DP3 result.texcoord[0].y, vertex.normal, c[6];
DP3 result.texcoord[0].x, vertex.normal, c[5];
END
# 21 instructions, 2 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "POINT" }
Bind "vertex" Vertex
Bind "normal" Normal
Matrix 0 [glstate_matrix_mvp]
Vector 12 [unity_Scale]
Vector 13 [_WorldSpaceLightPos0]
Matrix 4 [_Object2World]
Matrix 8 [_LightMatrix0]
"vs_2_0
; 21 ALU
dcl_position0 v0
dcl_normal0 v1
dp4 r1.z, v0, c6
dp4 r1.x, v0, c4
dp4 r1.y, v0, c5
mov r0.xyz, r1
dp4 r0.w, v0, c7
dp4 oT4.z, r0, c10
dp4 oT4.y, r0, c9
dp4 oT4.x, r0, c8
mul r0.xyz, v1, c12.w
mov oT1.xyz, r1
dp3 oT2.z, r0, c6
dp3 oT2.y, r0, c5
dp3 oT2.x, r0, c4
add oT3.xyz, -r1, c13
dp4 oPos.w, v0, c3
dp4 oPos.z, v0, c2
dp4 oPos.y, v0, c1
dp4 oPos.x, v0, c0
dp3 oT0.z, v1, c6
dp3 oT0.y, v1, c5
dp3 oT0.x, v1, c4
"
}

SubProgram "gles " {
Keywords { "POINT" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying highp vec3 xlv_TEXCOORD4;
varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 unity_Scale;

uniform highp vec4 _WorldSpaceLightPos0;
uniform highp mat4 _Object2World;
uniform highp mat4 _LightMatrix0;
attribute vec3 _glesNormal;
attribute vec4 _glesVertex;
void main ()
{
  vec3 tmpvar_1;
  tmpvar_1 = normalize (_glesNormal);
  lowp vec3 tmpvar_2;
  lowp vec3 tmpvar_3;
  mediump vec3 tmpvar_4;
  mat3 tmpvar_5;
  tmpvar_5[0] = _Object2World[0].xyz;
  tmpvar_5[1] = _Object2World[1].xyz;
  tmpvar_5[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_6;
  tmpvar_6 = (tmpvar_5 * tmpvar_1);
  tmpvar_2 = tmpvar_6;
  mat3 tmpvar_7;
  tmpvar_7[0] = _Object2World[0].xyz;
  tmpvar_7[1] = _Object2World[1].xyz;
  tmpvar_7[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_8;
  tmpvar_8 = (tmpvar_7 * (tmpvar_1 * unity_Scale.w));
  tmpvar_3 = tmpvar_8;
  highp vec3 tmpvar_9;
  tmpvar_9 = (_WorldSpaceLightPos0.xyz - (_Object2World * _glesVertex).xyz);
  tmpvar_4 = tmpvar_9;
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_TEXCOORD0 = tmpvar_2;
  xlv_TEXCOORD1 = (_Object2World * _glesVertex).xyz;
  xlv_TEXCOORD2 = tmpvar_3;
  xlv_TEXCOORD3 = tmpvar_4;
  xlv_TEXCOORD4 = (_LightMatrix0 * (_Object2World * _glesVertex)).xyz;
}



#endif
#ifdef FRAGMENT

varying highp vec3 xlv_TEXCOORD4;
varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 _MapWorldOrigin;
uniform highp vec4 _MapTileSize;
uniform highp vec4 _MapSizeInTiles;
uniform sampler2D _LightTexture0;
uniform lowp vec4 _LightColor0;
uniform lowp vec4 _Color;
uniform sampler2D _Boxes;
void main ()
{
  lowp vec4 c;
  lowp vec3 lightDir;
  lowp vec3 tmpvar_1;
  lowp float tmpvar_2;
  highp vec3 tmpvar_3;
  tmpvar_3 = xlv_TEXCOORD0;
  tmpvar_1 = vec3(0.0, 0.0, 0.0);
  tmpvar_2 = 0.0;
  lowp vec3 tmpvar_4;
  lowp float tmpvar_5;
  tmpvar_4 = tmpvar_1;
  tmpvar_5 = tmpvar_2;
  highp vec4 boxTex;
  highp vec3 tmpvar_6;
  tmpvar_6 = (floor ((((xlv_TEXCOORD1 - _MapWorldOrigin.xyz) - vec3(-5.0, 0.01, -5.0)) / _MapTileSize.xyz)).xzy / _MapSizeInTiles.xzy);
  lowp vec4 tmpvar_7;
  tmpvar_7 = texture2D (_Boxes, tmpvar_6.xy);
  boxTex = tmpvar_7;
  bool tmpvar_8;
  if ((boxTex.w == 0.0)) {
    tmpvar_8 = (boxTex.x == 0.0);
  } else {
    tmpvar_8 = bool(0);
  };
  bool tmpvar_9;
  if (tmpvar_8) {
    tmpvar_9 = (boxTex.y == 0.0);
  } else {
    tmpvar_9 = bool(0);
  };
  bool tmpvar_10;
  if (tmpvar_9) {
    tmpvar_10 = (boxTex.z == 0.0);
  } else {
    tmpvar_10 = bool(0);
  };
  if (tmpvar_10) {
  } else {
    highp float tmpvar_11;
    tmpvar_11 = dot (tmpvar_3, vec3(0.0, 1.0, 0.0));
    if ((tmpvar_11 > 0.3)) {
      bool tmpvar_12;
      if ((tmpvar_6.z >= boxTex.w)) {
        tmpvar_12 = (tmpvar_6.z <= boxTex.x);
      } else {
        tmpvar_12 = bool(0);
      };
      bool tmpvar_13;
      if (tmpvar_12) {
        tmpvar_13 = bool(1);
      } else {
        bool tmpvar_14;
        if ((tmpvar_6.z >= boxTex.y)) {
          tmpvar_14 = (tmpvar_6.z <= boxTex.z);
        } else {
          tmpvar_14 = bool(0);
        };
        tmpvar_13 = tmpvar_14;
      };
      if (tmpvar_13) {
        tmpvar_4 = _Color.xyz;
        tmpvar_5 = _Color.w;
      };
    };
  };
  tmpvar_1 = tmpvar_4;
  tmpvar_2 = tmpvar_5;
  mediump vec3 tmpvar_15;
  tmpvar_15 = normalize (xlv_TEXCOORD3);
  lightDir = tmpvar_15;
  highp vec2 tmpvar_16;
  tmpvar_16 = vec2(dot (xlv_TEXCOORD4, xlv_TEXCOORD4));
  lowp vec4 c_i0;
  c_i0.xyz = ((tmpvar_4 * _LightColor0.xyz) * ((max (0.0, dot (xlv_TEXCOORD2, lightDir)) * texture2D (_LightTexture0, tmpvar_16).w) * 2.0));
  c_i0.w = tmpvar_5;
  c = c_i0;
  c.w = tmpvar_5;
  gl_FragData[0] = c;
}



#endif"
}

SubProgram "glesdesktop " {
Keywords { "POINT" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying highp vec3 xlv_TEXCOORD4;
varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 unity_Scale;

uniform highp vec4 _WorldSpaceLightPos0;
uniform highp mat4 _Object2World;
uniform highp mat4 _LightMatrix0;
attribute vec3 _glesNormal;
attribute vec4 _glesVertex;
void main ()
{
  vec3 tmpvar_1;
  tmpvar_1 = normalize (_glesNormal);
  lowp vec3 tmpvar_2;
  lowp vec3 tmpvar_3;
  mediump vec3 tmpvar_4;
  mat3 tmpvar_5;
  tmpvar_5[0] = _Object2World[0].xyz;
  tmpvar_5[1] = _Object2World[1].xyz;
  tmpvar_5[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_6;
  tmpvar_6 = (tmpvar_5 * tmpvar_1);
  tmpvar_2 = tmpvar_6;
  mat3 tmpvar_7;
  tmpvar_7[0] = _Object2World[0].xyz;
  tmpvar_7[1] = _Object2World[1].xyz;
  tmpvar_7[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_8;
  tmpvar_8 = (tmpvar_7 * (tmpvar_1 * unity_Scale.w));
  tmpvar_3 = tmpvar_8;
  highp vec3 tmpvar_9;
  tmpvar_9 = (_WorldSpaceLightPos0.xyz - (_Object2World * _glesVertex).xyz);
  tmpvar_4 = tmpvar_9;
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_TEXCOORD0 = tmpvar_2;
  xlv_TEXCOORD1 = (_Object2World * _glesVertex).xyz;
  xlv_TEXCOORD2 = tmpvar_3;
  xlv_TEXCOORD3 = tmpvar_4;
  xlv_TEXCOORD4 = (_LightMatrix0 * (_Object2World * _glesVertex)).xyz;
}



#endif
#ifdef FRAGMENT

varying highp vec3 xlv_TEXCOORD4;
varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 _MapWorldOrigin;
uniform highp vec4 _MapTileSize;
uniform highp vec4 _MapSizeInTiles;
uniform sampler2D _LightTexture0;
uniform lowp vec4 _LightColor0;
uniform lowp vec4 _Color;
uniform sampler2D _Boxes;
void main ()
{
  lowp vec4 c;
  lowp vec3 lightDir;
  lowp vec3 tmpvar_1;
  lowp float tmpvar_2;
  highp vec3 tmpvar_3;
  tmpvar_3 = xlv_TEXCOORD0;
  tmpvar_1 = vec3(0.0, 0.0, 0.0);
  tmpvar_2 = 0.0;
  lowp vec3 tmpvar_4;
  lowp float tmpvar_5;
  tmpvar_4 = tmpvar_1;
  tmpvar_5 = tmpvar_2;
  highp vec4 boxTex;
  highp vec3 tmpvar_6;
  tmpvar_6 = (floor ((((xlv_TEXCOORD1 - _MapWorldOrigin.xyz) - vec3(-5.0, 0.01, -5.0)) / _MapTileSize.xyz)).xzy / _MapSizeInTiles.xzy);
  lowp vec4 tmpvar_7;
  tmpvar_7 = texture2D (_Boxes, tmpvar_6.xy);
  boxTex = tmpvar_7;
  bool tmpvar_8;
  if ((boxTex.w == 0.0)) {
    tmpvar_8 = (boxTex.x == 0.0);
  } else {
    tmpvar_8 = bool(0);
  };
  bool tmpvar_9;
  if (tmpvar_8) {
    tmpvar_9 = (boxTex.y == 0.0);
  } else {
    tmpvar_9 = bool(0);
  };
  bool tmpvar_10;
  if (tmpvar_9) {
    tmpvar_10 = (boxTex.z == 0.0);
  } else {
    tmpvar_10 = bool(0);
  };
  if (tmpvar_10) {
  } else {
    highp float tmpvar_11;
    tmpvar_11 = dot (tmpvar_3, vec3(0.0, 1.0, 0.0));
    if ((tmpvar_11 > 0.3)) {
      bool tmpvar_12;
      if ((tmpvar_6.z >= boxTex.w)) {
        tmpvar_12 = (tmpvar_6.z <= boxTex.x);
      } else {
        tmpvar_12 = bool(0);
      };
      bool tmpvar_13;
      if (tmpvar_12) {
        tmpvar_13 = bool(1);
      } else {
        bool tmpvar_14;
        if ((tmpvar_6.z >= boxTex.y)) {
          tmpvar_14 = (tmpvar_6.z <= boxTex.z);
        } else {
          tmpvar_14 = bool(0);
        };
        tmpvar_13 = tmpvar_14;
      };
      if (tmpvar_13) {
        tmpvar_4 = _Color.xyz;
        tmpvar_5 = _Color.w;
      };
    };
  };
  tmpvar_1 = tmpvar_4;
  tmpvar_2 = tmpvar_5;
  mediump vec3 tmpvar_15;
  tmpvar_15 = normalize (xlv_TEXCOORD3);
  lightDir = tmpvar_15;
  highp vec2 tmpvar_16;
  tmpvar_16 = vec2(dot (xlv_TEXCOORD4, xlv_TEXCOORD4));
  lowp vec4 c_i0;
  c_i0.xyz = ((tmpvar_4 * _LightColor0.xyz) * ((max (0.0, dot (xlv_TEXCOORD2, lightDir)) * texture2D (_LightTexture0, tmpvar_16).w) * 2.0));
  c_i0.w = tmpvar_5;
  c = c_i0;
  c.w = tmpvar_5;
  gl_FragData[0] = c;
}



#endif"
}

SubProgram "opengl " {
Keywords { "DIRECTIONAL" }
Bind "vertex" Vertex
Bind "normal" Normal
Vector 9 [unity_Scale]
Vector 10 [_WorldSpaceLightPos0]
Matrix 5 [_Object2World]
"!!ARBvp1.0
# 15 ALU
PARAM c[11] = { program.local[0],
		state.matrix.mvp,
		program.local[5..10] };
TEMP R0;
MUL R0.xyz, vertex.normal, c[9].w;
DP3 result.texcoord[2].z, R0, c[7];
DP3 result.texcoord[2].y, R0, c[6];
DP3 result.texcoord[2].x, R0, c[5];
MOV result.texcoord[3].xyz, c[10];
DP4 result.position.w, vertex.position, c[4];
DP4 result.position.z, vertex.position, c[3];
DP4 result.position.y, vertex.position, c[2];
DP4 result.position.x, vertex.position, c[1];
DP3 result.texcoord[0].z, vertex.normal, c[7];
DP3 result.texcoord[0].y, vertex.normal, c[6];
DP3 result.texcoord[0].x, vertex.normal, c[5];
DP4 result.texcoord[1].z, vertex.position, c[7];
DP4 result.texcoord[1].y, vertex.position, c[6];
DP4 result.texcoord[1].x, vertex.position, c[5];
END
# 15 instructions, 1 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "DIRECTIONAL" }
Bind "vertex" Vertex
Bind "normal" Normal
Matrix 0 [glstate_matrix_mvp]
Vector 8 [unity_Scale]
Vector 9 [_WorldSpaceLightPos0]
Matrix 4 [_Object2World]
"vs_2_0
; 15 ALU
dcl_position0 v0
dcl_normal0 v1
mul r0.xyz, v1, c8.w
dp3 oT2.z, r0, c6
dp3 oT2.y, r0, c5
dp3 oT2.x, r0, c4
mov oT3.xyz, c9
dp4 oPos.w, v0, c3
dp4 oPos.z, v0, c2
dp4 oPos.y, v0, c1
dp4 oPos.x, v0, c0
dp3 oT0.z, v1, c6
dp3 oT0.y, v1, c5
dp3 oT0.x, v1, c4
dp4 oT1.z, v0, c6
dp4 oT1.y, v0, c5
dp4 oT1.x, v0, c4
"
}

SubProgram "gles " {
Keywords { "DIRECTIONAL" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 unity_Scale;

uniform lowp vec4 _WorldSpaceLightPos0;
uniform highp mat4 _Object2World;
attribute vec3 _glesNormal;
attribute vec4 _glesVertex;
void main ()
{
  vec3 tmpvar_1;
  tmpvar_1 = normalize (_glesNormal);
  lowp vec3 tmpvar_2;
  lowp vec3 tmpvar_3;
  mediump vec3 tmpvar_4;
  mat3 tmpvar_5;
  tmpvar_5[0] = _Object2World[0].xyz;
  tmpvar_5[1] = _Object2World[1].xyz;
  tmpvar_5[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_6;
  tmpvar_6 = (tmpvar_5 * tmpvar_1);
  tmpvar_2 = tmpvar_6;
  mat3 tmpvar_7;
  tmpvar_7[0] = _Object2World[0].xyz;
  tmpvar_7[1] = _Object2World[1].xyz;
  tmpvar_7[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_8;
  tmpvar_8 = (tmpvar_7 * (tmpvar_1 * unity_Scale.w));
  tmpvar_3 = tmpvar_8;
  highp vec3 tmpvar_9;
  tmpvar_9 = _WorldSpaceLightPos0.xyz;
  tmpvar_4 = tmpvar_9;
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_TEXCOORD0 = tmpvar_2;
  xlv_TEXCOORD1 = (_Object2World * _glesVertex).xyz;
  xlv_TEXCOORD2 = tmpvar_3;
  xlv_TEXCOORD3 = tmpvar_4;
}



#endif
#ifdef FRAGMENT

varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 _MapWorldOrigin;
uniform highp vec4 _MapTileSize;
uniform highp vec4 _MapSizeInTiles;
uniform lowp vec4 _LightColor0;
uniform lowp vec4 _Color;
uniform sampler2D _Boxes;
void main ()
{
  lowp vec4 c;
  lowp vec3 lightDir;
  lowp vec3 tmpvar_1;
  lowp float tmpvar_2;
  highp vec3 tmpvar_3;
  tmpvar_3 = xlv_TEXCOORD0;
  tmpvar_1 = vec3(0.0, 0.0, 0.0);
  tmpvar_2 = 0.0;
  lowp vec3 tmpvar_4;
  lowp float tmpvar_5;
  tmpvar_4 = tmpvar_1;
  tmpvar_5 = tmpvar_2;
  highp vec4 boxTex;
  highp vec3 tmpvar_6;
  tmpvar_6 = (floor ((((xlv_TEXCOORD1 - _MapWorldOrigin.xyz) - vec3(-5.0, 0.01, -5.0)) / _MapTileSize.xyz)).xzy / _MapSizeInTiles.xzy);
  lowp vec4 tmpvar_7;
  tmpvar_7 = texture2D (_Boxes, tmpvar_6.xy);
  boxTex = tmpvar_7;
  bool tmpvar_8;
  if ((boxTex.w == 0.0)) {
    tmpvar_8 = (boxTex.x == 0.0);
  } else {
    tmpvar_8 = bool(0);
  };
  bool tmpvar_9;
  if (tmpvar_8) {
    tmpvar_9 = (boxTex.y == 0.0);
  } else {
    tmpvar_9 = bool(0);
  };
  bool tmpvar_10;
  if (tmpvar_9) {
    tmpvar_10 = (boxTex.z == 0.0);
  } else {
    tmpvar_10 = bool(0);
  };
  if (tmpvar_10) {
  } else {
    highp float tmpvar_11;
    tmpvar_11 = dot (tmpvar_3, vec3(0.0, 1.0, 0.0));
    if ((tmpvar_11 > 0.3)) {
      bool tmpvar_12;
      if ((tmpvar_6.z >= boxTex.w)) {
        tmpvar_12 = (tmpvar_6.z <= boxTex.x);
      } else {
        tmpvar_12 = bool(0);
      };
      bool tmpvar_13;
      if (tmpvar_12) {
        tmpvar_13 = bool(1);
      } else {
        bool tmpvar_14;
        if ((tmpvar_6.z >= boxTex.y)) {
          tmpvar_14 = (tmpvar_6.z <= boxTex.z);
        } else {
          tmpvar_14 = bool(0);
        };
        tmpvar_13 = tmpvar_14;
      };
      if (tmpvar_13) {
        tmpvar_4 = _Color.xyz;
        tmpvar_5 = _Color.w;
      };
    };
  };
  tmpvar_1 = tmpvar_4;
  tmpvar_2 = tmpvar_5;
  lightDir = xlv_TEXCOORD3;
  lowp vec4 c_i0;
  c_i0.xyz = ((tmpvar_4 * _LightColor0.xyz) * (max (0.0, dot (xlv_TEXCOORD2, lightDir)) * 2.0));
  c_i0.w = tmpvar_5;
  c = c_i0;
  c.w = tmpvar_5;
  gl_FragData[0] = c;
}



#endif"
}

SubProgram "glesdesktop " {
Keywords { "DIRECTIONAL" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 unity_Scale;

uniform lowp vec4 _WorldSpaceLightPos0;
uniform highp mat4 _Object2World;
attribute vec3 _glesNormal;
attribute vec4 _glesVertex;
void main ()
{
  vec3 tmpvar_1;
  tmpvar_1 = normalize (_glesNormal);
  lowp vec3 tmpvar_2;
  lowp vec3 tmpvar_3;
  mediump vec3 tmpvar_4;
  mat3 tmpvar_5;
  tmpvar_5[0] = _Object2World[0].xyz;
  tmpvar_5[1] = _Object2World[1].xyz;
  tmpvar_5[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_6;
  tmpvar_6 = (tmpvar_5 * tmpvar_1);
  tmpvar_2 = tmpvar_6;
  mat3 tmpvar_7;
  tmpvar_7[0] = _Object2World[0].xyz;
  tmpvar_7[1] = _Object2World[1].xyz;
  tmpvar_7[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_8;
  tmpvar_8 = (tmpvar_7 * (tmpvar_1 * unity_Scale.w));
  tmpvar_3 = tmpvar_8;
  highp vec3 tmpvar_9;
  tmpvar_9 = _WorldSpaceLightPos0.xyz;
  tmpvar_4 = tmpvar_9;
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_TEXCOORD0 = tmpvar_2;
  xlv_TEXCOORD1 = (_Object2World * _glesVertex).xyz;
  xlv_TEXCOORD2 = tmpvar_3;
  xlv_TEXCOORD3 = tmpvar_4;
}



#endif
#ifdef FRAGMENT

varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 _MapWorldOrigin;
uniform highp vec4 _MapTileSize;
uniform highp vec4 _MapSizeInTiles;
uniform lowp vec4 _LightColor0;
uniform lowp vec4 _Color;
uniform sampler2D _Boxes;
void main ()
{
  lowp vec4 c;
  lowp vec3 lightDir;
  lowp vec3 tmpvar_1;
  lowp float tmpvar_2;
  highp vec3 tmpvar_3;
  tmpvar_3 = xlv_TEXCOORD0;
  tmpvar_1 = vec3(0.0, 0.0, 0.0);
  tmpvar_2 = 0.0;
  lowp vec3 tmpvar_4;
  lowp float tmpvar_5;
  tmpvar_4 = tmpvar_1;
  tmpvar_5 = tmpvar_2;
  highp vec4 boxTex;
  highp vec3 tmpvar_6;
  tmpvar_6 = (floor ((((xlv_TEXCOORD1 - _MapWorldOrigin.xyz) - vec3(-5.0, 0.01, -5.0)) / _MapTileSize.xyz)).xzy / _MapSizeInTiles.xzy);
  lowp vec4 tmpvar_7;
  tmpvar_7 = texture2D (_Boxes, tmpvar_6.xy);
  boxTex = tmpvar_7;
  bool tmpvar_8;
  if ((boxTex.w == 0.0)) {
    tmpvar_8 = (boxTex.x == 0.0);
  } else {
    tmpvar_8 = bool(0);
  };
  bool tmpvar_9;
  if (tmpvar_8) {
    tmpvar_9 = (boxTex.y == 0.0);
  } else {
    tmpvar_9 = bool(0);
  };
  bool tmpvar_10;
  if (tmpvar_9) {
    tmpvar_10 = (boxTex.z == 0.0);
  } else {
    tmpvar_10 = bool(0);
  };
  if (tmpvar_10) {
  } else {
    highp float tmpvar_11;
    tmpvar_11 = dot (tmpvar_3, vec3(0.0, 1.0, 0.0));
    if ((tmpvar_11 > 0.3)) {
      bool tmpvar_12;
      if ((tmpvar_6.z >= boxTex.w)) {
        tmpvar_12 = (tmpvar_6.z <= boxTex.x);
      } else {
        tmpvar_12 = bool(0);
      };
      bool tmpvar_13;
      if (tmpvar_12) {
        tmpvar_13 = bool(1);
      } else {
        bool tmpvar_14;
        if ((tmpvar_6.z >= boxTex.y)) {
          tmpvar_14 = (tmpvar_6.z <= boxTex.z);
        } else {
          tmpvar_14 = bool(0);
        };
        tmpvar_13 = tmpvar_14;
      };
      if (tmpvar_13) {
        tmpvar_4 = _Color.xyz;
        tmpvar_5 = _Color.w;
      };
    };
  };
  tmpvar_1 = tmpvar_4;
  tmpvar_2 = tmpvar_5;
  lightDir = xlv_TEXCOORD3;
  lowp vec4 c_i0;
  c_i0.xyz = ((tmpvar_4 * _LightColor0.xyz) * (max (0.0, dot (xlv_TEXCOORD2, lightDir)) * 2.0));
  c_i0.w = tmpvar_5;
  c = c_i0;
  c.w = tmpvar_5;
  gl_FragData[0] = c;
}



#endif"
}

SubProgram "opengl " {
Keywords { "SPOT" }
Bind "vertex" Vertex
Bind "normal" Normal
Vector 13 [unity_Scale]
Vector 14 [_WorldSpaceLightPos0]
Matrix 5 [_Object2World]
Matrix 9 [_LightMatrix0]
"!!ARBvp1.0
# 22 ALU
PARAM c[15] = { program.local[0],
		state.matrix.mvp,
		program.local[5..14] };
TEMP R0;
TEMP R1;
DP4 R0.w, vertex.position, c[8];
DP4 R1.z, vertex.position, c[7];
DP4 R1.x, vertex.position, c[5];
DP4 R1.y, vertex.position, c[6];
MOV R0.xyz, R1;
DP4 result.texcoord[4].w, R0, c[12];
DP4 result.texcoord[4].z, R0, c[11];
DP4 result.texcoord[4].y, R0, c[10];
DP4 result.texcoord[4].x, R0, c[9];
MUL R0.xyz, vertex.normal, c[13].w;
MOV result.texcoord[1].xyz, R1;
DP3 result.texcoord[2].z, R0, c[7];
DP3 result.texcoord[2].y, R0, c[6];
DP3 result.texcoord[2].x, R0, c[5];
ADD result.texcoord[3].xyz, -R1, c[14];
DP4 result.position.w, vertex.position, c[4];
DP4 result.position.z, vertex.position, c[3];
DP4 result.position.y, vertex.position, c[2];
DP4 result.position.x, vertex.position, c[1];
DP3 result.texcoord[0].z, vertex.normal, c[7];
DP3 result.texcoord[0].y, vertex.normal, c[6];
DP3 result.texcoord[0].x, vertex.normal, c[5];
END
# 22 instructions, 2 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "SPOT" }
Bind "vertex" Vertex
Bind "normal" Normal
Matrix 0 [glstate_matrix_mvp]
Vector 12 [unity_Scale]
Vector 13 [_WorldSpaceLightPos0]
Matrix 4 [_Object2World]
Matrix 8 [_LightMatrix0]
"vs_2_0
; 22 ALU
dcl_position0 v0
dcl_normal0 v1
dp4 r0.w, v0, c7
dp4 r1.z, v0, c6
dp4 r1.x, v0, c4
dp4 r1.y, v0, c5
mov r0.xyz, r1
dp4 oT4.w, r0, c11
dp4 oT4.z, r0, c10
dp4 oT4.y, r0, c9
dp4 oT4.x, r0, c8
mul r0.xyz, v1, c12.w
mov oT1.xyz, r1
dp3 oT2.z, r0, c6
dp3 oT2.y, r0, c5
dp3 oT2.x, r0, c4
add oT3.xyz, -r1, c13
dp4 oPos.w, v0, c3
dp4 oPos.z, v0, c2
dp4 oPos.y, v0, c1
dp4 oPos.x, v0, c0
dp3 oT0.z, v1, c6
dp3 oT0.y, v1, c5
dp3 oT0.x, v1, c4
"
}

SubProgram "gles " {
Keywords { "SPOT" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying highp vec4 xlv_TEXCOORD4;
varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 unity_Scale;

uniform highp vec4 _WorldSpaceLightPos0;
uniform highp mat4 _Object2World;
uniform highp mat4 _LightMatrix0;
attribute vec3 _glesNormal;
attribute vec4 _glesVertex;
void main ()
{
  vec3 tmpvar_1;
  tmpvar_1 = normalize (_glesNormal);
  lowp vec3 tmpvar_2;
  lowp vec3 tmpvar_3;
  mediump vec3 tmpvar_4;
  mat3 tmpvar_5;
  tmpvar_5[0] = _Object2World[0].xyz;
  tmpvar_5[1] = _Object2World[1].xyz;
  tmpvar_5[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_6;
  tmpvar_6 = (tmpvar_5 * tmpvar_1);
  tmpvar_2 = tmpvar_6;
  mat3 tmpvar_7;
  tmpvar_7[0] = _Object2World[0].xyz;
  tmpvar_7[1] = _Object2World[1].xyz;
  tmpvar_7[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_8;
  tmpvar_8 = (tmpvar_7 * (tmpvar_1 * unity_Scale.w));
  tmpvar_3 = tmpvar_8;
  highp vec3 tmpvar_9;
  tmpvar_9 = (_WorldSpaceLightPos0.xyz - (_Object2World * _glesVertex).xyz);
  tmpvar_4 = tmpvar_9;
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_TEXCOORD0 = tmpvar_2;
  xlv_TEXCOORD1 = (_Object2World * _glesVertex).xyz;
  xlv_TEXCOORD2 = tmpvar_3;
  xlv_TEXCOORD3 = tmpvar_4;
  xlv_TEXCOORD4 = (_LightMatrix0 * (_Object2World * _glesVertex));
}



#endif
#ifdef FRAGMENT

varying highp vec4 xlv_TEXCOORD4;
varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 _MapWorldOrigin;
uniform highp vec4 _MapTileSize;
uniform highp vec4 _MapSizeInTiles;
uniform sampler2D _LightTextureB0;
uniform sampler2D _LightTexture0;
uniform lowp vec4 _LightColor0;
uniform lowp vec4 _Color;
uniform sampler2D _Boxes;
void main ()
{
  lowp vec4 c;
  lowp vec3 lightDir;
  lowp vec3 tmpvar_1;
  lowp float tmpvar_2;
  highp vec3 tmpvar_3;
  tmpvar_3 = xlv_TEXCOORD0;
  tmpvar_1 = vec3(0.0, 0.0, 0.0);
  tmpvar_2 = 0.0;
  lowp vec3 tmpvar_4;
  lowp float tmpvar_5;
  tmpvar_4 = tmpvar_1;
  tmpvar_5 = tmpvar_2;
  highp vec4 boxTex;
  highp vec3 tmpvar_6;
  tmpvar_6 = (floor ((((xlv_TEXCOORD1 - _MapWorldOrigin.xyz) - vec3(-5.0, 0.01, -5.0)) / _MapTileSize.xyz)).xzy / _MapSizeInTiles.xzy);
  lowp vec4 tmpvar_7;
  tmpvar_7 = texture2D (_Boxes, tmpvar_6.xy);
  boxTex = tmpvar_7;
  bool tmpvar_8;
  if ((boxTex.w == 0.0)) {
    tmpvar_8 = (boxTex.x == 0.0);
  } else {
    tmpvar_8 = bool(0);
  };
  bool tmpvar_9;
  if (tmpvar_8) {
    tmpvar_9 = (boxTex.y == 0.0);
  } else {
    tmpvar_9 = bool(0);
  };
  bool tmpvar_10;
  if (tmpvar_9) {
    tmpvar_10 = (boxTex.z == 0.0);
  } else {
    tmpvar_10 = bool(0);
  };
  if (tmpvar_10) {
  } else {
    highp float tmpvar_11;
    tmpvar_11 = dot (tmpvar_3, vec3(0.0, 1.0, 0.0));
    if ((tmpvar_11 > 0.3)) {
      bool tmpvar_12;
      if ((tmpvar_6.z >= boxTex.w)) {
        tmpvar_12 = (tmpvar_6.z <= boxTex.x);
      } else {
        tmpvar_12 = bool(0);
      };
      bool tmpvar_13;
      if (tmpvar_12) {
        tmpvar_13 = bool(1);
      } else {
        bool tmpvar_14;
        if ((tmpvar_6.z >= boxTex.y)) {
          tmpvar_14 = (tmpvar_6.z <= boxTex.z);
        } else {
          tmpvar_14 = bool(0);
        };
        tmpvar_13 = tmpvar_14;
      };
      if (tmpvar_13) {
        tmpvar_4 = _Color.xyz;
        tmpvar_5 = _Color.w;
      };
    };
  };
  tmpvar_1 = tmpvar_4;
  tmpvar_2 = tmpvar_5;
  mediump vec3 tmpvar_15;
  tmpvar_15 = normalize (xlv_TEXCOORD3);
  lightDir = tmpvar_15;
  highp vec3 LightCoord_i0;
  LightCoord_i0 = xlv_TEXCOORD4.xyz;
  highp vec2 tmpvar_16;
  tmpvar_16 = vec2(dot (LightCoord_i0, LightCoord_i0));
  lowp float atten;
  atten = ((float((xlv_TEXCOORD4.z > 0.0)) * texture2D (_LightTexture0, ((xlv_TEXCOORD4.xy / xlv_TEXCOORD4.w) + 0.5)).w) * texture2D (_LightTextureB0, tmpvar_16).w);
  lowp vec4 c_i0;
  c_i0.xyz = ((tmpvar_4 * _LightColor0.xyz) * ((max (0.0, dot (xlv_TEXCOORD2, lightDir)) * atten) * 2.0));
  c_i0.w = tmpvar_5;
  c = c_i0;
  c.w = tmpvar_5;
  gl_FragData[0] = c;
}



#endif"
}

SubProgram "glesdesktop " {
Keywords { "SPOT" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying highp vec4 xlv_TEXCOORD4;
varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 unity_Scale;

uniform highp vec4 _WorldSpaceLightPos0;
uniform highp mat4 _Object2World;
uniform highp mat4 _LightMatrix0;
attribute vec3 _glesNormal;
attribute vec4 _glesVertex;
void main ()
{
  vec3 tmpvar_1;
  tmpvar_1 = normalize (_glesNormal);
  lowp vec3 tmpvar_2;
  lowp vec3 tmpvar_3;
  mediump vec3 tmpvar_4;
  mat3 tmpvar_5;
  tmpvar_5[0] = _Object2World[0].xyz;
  tmpvar_5[1] = _Object2World[1].xyz;
  tmpvar_5[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_6;
  tmpvar_6 = (tmpvar_5 * tmpvar_1);
  tmpvar_2 = tmpvar_6;
  mat3 tmpvar_7;
  tmpvar_7[0] = _Object2World[0].xyz;
  tmpvar_7[1] = _Object2World[1].xyz;
  tmpvar_7[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_8;
  tmpvar_8 = (tmpvar_7 * (tmpvar_1 * unity_Scale.w));
  tmpvar_3 = tmpvar_8;
  highp vec3 tmpvar_9;
  tmpvar_9 = (_WorldSpaceLightPos0.xyz - (_Object2World * _glesVertex).xyz);
  tmpvar_4 = tmpvar_9;
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_TEXCOORD0 = tmpvar_2;
  xlv_TEXCOORD1 = (_Object2World * _glesVertex).xyz;
  xlv_TEXCOORD2 = tmpvar_3;
  xlv_TEXCOORD3 = tmpvar_4;
  xlv_TEXCOORD4 = (_LightMatrix0 * (_Object2World * _glesVertex));
}



#endif
#ifdef FRAGMENT

varying highp vec4 xlv_TEXCOORD4;
varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 _MapWorldOrigin;
uniform highp vec4 _MapTileSize;
uniform highp vec4 _MapSizeInTiles;
uniform sampler2D _LightTextureB0;
uniform sampler2D _LightTexture0;
uniform lowp vec4 _LightColor0;
uniform lowp vec4 _Color;
uniform sampler2D _Boxes;
void main ()
{
  lowp vec4 c;
  lowp vec3 lightDir;
  lowp vec3 tmpvar_1;
  lowp float tmpvar_2;
  highp vec3 tmpvar_3;
  tmpvar_3 = xlv_TEXCOORD0;
  tmpvar_1 = vec3(0.0, 0.0, 0.0);
  tmpvar_2 = 0.0;
  lowp vec3 tmpvar_4;
  lowp float tmpvar_5;
  tmpvar_4 = tmpvar_1;
  tmpvar_5 = tmpvar_2;
  highp vec4 boxTex;
  highp vec3 tmpvar_6;
  tmpvar_6 = (floor ((((xlv_TEXCOORD1 - _MapWorldOrigin.xyz) - vec3(-5.0, 0.01, -5.0)) / _MapTileSize.xyz)).xzy / _MapSizeInTiles.xzy);
  lowp vec4 tmpvar_7;
  tmpvar_7 = texture2D (_Boxes, tmpvar_6.xy);
  boxTex = tmpvar_7;
  bool tmpvar_8;
  if ((boxTex.w == 0.0)) {
    tmpvar_8 = (boxTex.x == 0.0);
  } else {
    tmpvar_8 = bool(0);
  };
  bool tmpvar_9;
  if (tmpvar_8) {
    tmpvar_9 = (boxTex.y == 0.0);
  } else {
    tmpvar_9 = bool(0);
  };
  bool tmpvar_10;
  if (tmpvar_9) {
    tmpvar_10 = (boxTex.z == 0.0);
  } else {
    tmpvar_10 = bool(0);
  };
  if (tmpvar_10) {
  } else {
    highp float tmpvar_11;
    tmpvar_11 = dot (tmpvar_3, vec3(0.0, 1.0, 0.0));
    if ((tmpvar_11 > 0.3)) {
      bool tmpvar_12;
      if ((tmpvar_6.z >= boxTex.w)) {
        tmpvar_12 = (tmpvar_6.z <= boxTex.x);
      } else {
        tmpvar_12 = bool(0);
      };
      bool tmpvar_13;
      if (tmpvar_12) {
        tmpvar_13 = bool(1);
      } else {
        bool tmpvar_14;
        if ((tmpvar_6.z >= boxTex.y)) {
          tmpvar_14 = (tmpvar_6.z <= boxTex.z);
        } else {
          tmpvar_14 = bool(0);
        };
        tmpvar_13 = tmpvar_14;
      };
      if (tmpvar_13) {
        tmpvar_4 = _Color.xyz;
        tmpvar_5 = _Color.w;
      };
    };
  };
  tmpvar_1 = tmpvar_4;
  tmpvar_2 = tmpvar_5;
  mediump vec3 tmpvar_15;
  tmpvar_15 = normalize (xlv_TEXCOORD3);
  lightDir = tmpvar_15;
  highp vec3 LightCoord_i0;
  LightCoord_i0 = xlv_TEXCOORD4.xyz;
  highp vec2 tmpvar_16;
  tmpvar_16 = vec2(dot (LightCoord_i0, LightCoord_i0));
  lowp float atten;
  atten = ((float((xlv_TEXCOORD4.z > 0.0)) * texture2D (_LightTexture0, ((xlv_TEXCOORD4.xy / xlv_TEXCOORD4.w) + 0.5)).w) * texture2D (_LightTextureB0, tmpvar_16).w);
  lowp vec4 c_i0;
  c_i0.xyz = ((tmpvar_4 * _LightColor0.xyz) * ((max (0.0, dot (xlv_TEXCOORD2, lightDir)) * atten) * 2.0));
  c_i0.w = tmpvar_5;
  c = c_i0;
  c.w = tmpvar_5;
  gl_FragData[0] = c;
}



#endif"
}

SubProgram "opengl " {
Keywords { "POINT_COOKIE" }
Bind "vertex" Vertex
Bind "normal" Normal
Vector 13 [unity_Scale]
Vector 14 [_WorldSpaceLightPos0]
Matrix 5 [_Object2World]
Matrix 9 [_LightMatrix0]
"!!ARBvp1.0
# 21 ALU
PARAM c[15] = { program.local[0],
		state.matrix.mvp,
		program.local[5..14] };
TEMP R0;
TEMP R1;
DP4 R1.z, vertex.position, c[7];
DP4 R1.x, vertex.position, c[5];
DP4 R1.y, vertex.position, c[6];
MOV R0.xyz, R1;
DP4 R0.w, vertex.position, c[8];
DP4 result.texcoord[4].z, R0, c[11];
DP4 result.texcoord[4].y, R0, c[10];
DP4 result.texcoord[4].x, R0, c[9];
MUL R0.xyz, vertex.normal, c[13].w;
MOV result.texcoord[1].xyz, R1;
DP3 result.texcoord[2].z, R0, c[7];
DP3 result.texcoord[2].y, R0, c[6];
DP3 result.texcoord[2].x, R0, c[5];
ADD result.texcoord[3].xyz, -R1, c[14];
DP4 result.position.w, vertex.position, c[4];
DP4 result.position.z, vertex.position, c[3];
DP4 result.position.y, vertex.position, c[2];
DP4 result.position.x, vertex.position, c[1];
DP3 result.texcoord[0].z, vertex.normal, c[7];
DP3 result.texcoord[0].y, vertex.normal, c[6];
DP3 result.texcoord[0].x, vertex.normal, c[5];
END
# 21 instructions, 2 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "POINT_COOKIE" }
Bind "vertex" Vertex
Bind "normal" Normal
Matrix 0 [glstate_matrix_mvp]
Vector 12 [unity_Scale]
Vector 13 [_WorldSpaceLightPos0]
Matrix 4 [_Object2World]
Matrix 8 [_LightMatrix0]
"vs_2_0
; 21 ALU
dcl_position0 v0
dcl_normal0 v1
dp4 r1.z, v0, c6
dp4 r1.x, v0, c4
dp4 r1.y, v0, c5
mov r0.xyz, r1
dp4 r0.w, v0, c7
dp4 oT4.z, r0, c10
dp4 oT4.y, r0, c9
dp4 oT4.x, r0, c8
mul r0.xyz, v1, c12.w
mov oT1.xyz, r1
dp3 oT2.z, r0, c6
dp3 oT2.y, r0, c5
dp3 oT2.x, r0, c4
add oT3.xyz, -r1, c13
dp4 oPos.w, v0, c3
dp4 oPos.z, v0, c2
dp4 oPos.y, v0, c1
dp4 oPos.x, v0, c0
dp3 oT0.z, v1, c6
dp3 oT0.y, v1, c5
dp3 oT0.x, v1, c4
"
}

SubProgram "gles " {
Keywords { "POINT_COOKIE" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying highp vec3 xlv_TEXCOORD4;
varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 unity_Scale;

uniform highp vec4 _WorldSpaceLightPos0;
uniform highp mat4 _Object2World;
uniform highp mat4 _LightMatrix0;
attribute vec3 _glesNormal;
attribute vec4 _glesVertex;
void main ()
{
  vec3 tmpvar_1;
  tmpvar_1 = normalize (_glesNormal);
  lowp vec3 tmpvar_2;
  lowp vec3 tmpvar_3;
  mediump vec3 tmpvar_4;
  mat3 tmpvar_5;
  tmpvar_5[0] = _Object2World[0].xyz;
  tmpvar_5[1] = _Object2World[1].xyz;
  tmpvar_5[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_6;
  tmpvar_6 = (tmpvar_5 * tmpvar_1);
  tmpvar_2 = tmpvar_6;
  mat3 tmpvar_7;
  tmpvar_7[0] = _Object2World[0].xyz;
  tmpvar_7[1] = _Object2World[1].xyz;
  tmpvar_7[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_8;
  tmpvar_8 = (tmpvar_7 * (tmpvar_1 * unity_Scale.w));
  tmpvar_3 = tmpvar_8;
  highp vec3 tmpvar_9;
  tmpvar_9 = (_WorldSpaceLightPos0.xyz - (_Object2World * _glesVertex).xyz);
  tmpvar_4 = tmpvar_9;
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_TEXCOORD0 = tmpvar_2;
  xlv_TEXCOORD1 = (_Object2World * _glesVertex).xyz;
  xlv_TEXCOORD2 = tmpvar_3;
  xlv_TEXCOORD3 = tmpvar_4;
  xlv_TEXCOORD4 = (_LightMatrix0 * (_Object2World * _glesVertex)).xyz;
}



#endif
#ifdef FRAGMENT

varying highp vec3 xlv_TEXCOORD4;
varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 _MapWorldOrigin;
uniform highp vec4 _MapTileSize;
uniform highp vec4 _MapSizeInTiles;
uniform sampler2D _LightTextureB0;
uniform samplerCube _LightTexture0;
uniform lowp vec4 _LightColor0;
uniform lowp vec4 _Color;
uniform sampler2D _Boxes;
void main ()
{
  lowp vec4 c;
  lowp vec3 lightDir;
  lowp vec3 tmpvar_1;
  lowp float tmpvar_2;
  highp vec3 tmpvar_3;
  tmpvar_3 = xlv_TEXCOORD0;
  tmpvar_1 = vec3(0.0, 0.0, 0.0);
  tmpvar_2 = 0.0;
  lowp vec3 tmpvar_4;
  lowp float tmpvar_5;
  tmpvar_4 = tmpvar_1;
  tmpvar_5 = tmpvar_2;
  highp vec4 boxTex;
  highp vec3 tmpvar_6;
  tmpvar_6 = (floor ((((xlv_TEXCOORD1 - _MapWorldOrigin.xyz) - vec3(-5.0, 0.01, -5.0)) / _MapTileSize.xyz)).xzy / _MapSizeInTiles.xzy);
  lowp vec4 tmpvar_7;
  tmpvar_7 = texture2D (_Boxes, tmpvar_6.xy);
  boxTex = tmpvar_7;
  bool tmpvar_8;
  if ((boxTex.w == 0.0)) {
    tmpvar_8 = (boxTex.x == 0.0);
  } else {
    tmpvar_8 = bool(0);
  };
  bool tmpvar_9;
  if (tmpvar_8) {
    tmpvar_9 = (boxTex.y == 0.0);
  } else {
    tmpvar_9 = bool(0);
  };
  bool tmpvar_10;
  if (tmpvar_9) {
    tmpvar_10 = (boxTex.z == 0.0);
  } else {
    tmpvar_10 = bool(0);
  };
  if (tmpvar_10) {
  } else {
    highp float tmpvar_11;
    tmpvar_11 = dot (tmpvar_3, vec3(0.0, 1.0, 0.0));
    if ((tmpvar_11 > 0.3)) {
      bool tmpvar_12;
      if ((tmpvar_6.z >= boxTex.w)) {
        tmpvar_12 = (tmpvar_6.z <= boxTex.x);
      } else {
        tmpvar_12 = bool(0);
      };
      bool tmpvar_13;
      if (tmpvar_12) {
        tmpvar_13 = bool(1);
      } else {
        bool tmpvar_14;
        if ((tmpvar_6.z >= boxTex.y)) {
          tmpvar_14 = (tmpvar_6.z <= boxTex.z);
        } else {
          tmpvar_14 = bool(0);
        };
        tmpvar_13 = tmpvar_14;
      };
      if (tmpvar_13) {
        tmpvar_4 = _Color.xyz;
        tmpvar_5 = _Color.w;
      };
    };
  };
  tmpvar_1 = tmpvar_4;
  tmpvar_2 = tmpvar_5;
  mediump vec3 tmpvar_15;
  tmpvar_15 = normalize (xlv_TEXCOORD3);
  lightDir = tmpvar_15;
  highp vec2 tmpvar_16;
  tmpvar_16 = vec2(dot (xlv_TEXCOORD4, xlv_TEXCOORD4));
  lowp vec4 c_i0;
  c_i0.xyz = ((tmpvar_4 * _LightColor0.xyz) * ((max (0.0, dot (xlv_TEXCOORD2, lightDir)) * (texture2D (_LightTextureB0, tmpvar_16).w * textureCube (_LightTexture0, xlv_TEXCOORD4).w)) * 2.0));
  c_i0.w = tmpvar_5;
  c = c_i0;
  c.w = tmpvar_5;
  gl_FragData[0] = c;
}



#endif"
}

SubProgram "glesdesktop " {
Keywords { "POINT_COOKIE" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying highp vec3 xlv_TEXCOORD4;
varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 unity_Scale;

uniform highp vec4 _WorldSpaceLightPos0;
uniform highp mat4 _Object2World;
uniform highp mat4 _LightMatrix0;
attribute vec3 _glesNormal;
attribute vec4 _glesVertex;
void main ()
{
  vec3 tmpvar_1;
  tmpvar_1 = normalize (_glesNormal);
  lowp vec3 tmpvar_2;
  lowp vec3 tmpvar_3;
  mediump vec3 tmpvar_4;
  mat3 tmpvar_5;
  tmpvar_5[0] = _Object2World[0].xyz;
  tmpvar_5[1] = _Object2World[1].xyz;
  tmpvar_5[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_6;
  tmpvar_6 = (tmpvar_5 * tmpvar_1);
  tmpvar_2 = tmpvar_6;
  mat3 tmpvar_7;
  tmpvar_7[0] = _Object2World[0].xyz;
  tmpvar_7[1] = _Object2World[1].xyz;
  tmpvar_7[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_8;
  tmpvar_8 = (tmpvar_7 * (tmpvar_1 * unity_Scale.w));
  tmpvar_3 = tmpvar_8;
  highp vec3 tmpvar_9;
  tmpvar_9 = (_WorldSpaceLightPos0.xyz - (_Object2World * _glesVertex).xyz);
  tmpvar_4 = tmpvar_9;
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_TEXCOORD0 = tmpvar_2;
  xlv_TEXCOORD1 = (_Object2World * _glesVertex).xyz;
  xlv_TEXCOORD2 = tmpvar_3;
  xlv_TEXCOORD3 = tmpvar_4;
  xlv_TEXCOORD4 = (_LightMatrix0 * (_Object2World * _glesVertex)).xyz;
}



#endif
#ifdef FRAGMENT

varying highp vec3 xlv_TEXCOORD4;
varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 _MapWorldOrigin;
uniform highp vec4 _MapTileSize;
uniform highp vec4 _MapSizeInTiles;
uniform sampler2D _LightTextureB0;
uniform samplerCube _LightTexture0;
uniform lowp vec4 _LightColor0;
uniform lowp vec4 _Color;
uniform sampler2D _Boxes;
void main ()
{
  lowp vec4 c;
  lowp vec3 lightDir;
  lowp vec3 tmpvar_1;
  lowp float tmpvar_2;
  highp vec3 tmpvar_3;
  tmpvar_3 = xlv_TEXCOORD0;
  tmpvar_1 = vec3(0.0, 0.0, 0.0);
  tmpvar_2 = 0.0;
  lowp vec3 tmpvar_4;
  lowp float tmpvar_5;
  tmpvar_4 = tmpvar_1;
  tmpvar_5 = tmpvar_2;
  highp vec4 boxTex;
  highp vec3 tmpvar_6;
  tmpvar_6 = (floor ((((xlv_TEXCOORD1 - _MapWorldOrigin.xyz) - vec3(-5.0, 0.01, -5.0)) / _MapTileSize.xyz)).xzy / _MapSizeInTiles.xzy);
  lowp vec4 tmpvar_7;
  tmpvar_7 = texture2D (_Boxes, tmpvar_6.xy);
  boxTex = tmpvar_7;
  bool tmpvar_8;
  if ((boxTex.w == 0.0)) {
    tmpvar_8 = (boxTex.x == 0.0);
  } else {
    tmpvar_8 = bool(0);
  };
  bool tmpvar_9;
  if (tmpvar_8) {
    tmpvar_9 = (boxTex.y == 0.0);
  } else {
    tmpvar_9 = bool(0);
  };
  bool tmpvar_10;
  if (tmpvar_9) {
    tmpvar_10 = (boxTex.z == 0.0);
  } else {
    tmpvar_10 = bool(0);
  };
  if (tmpvar_10) {
  } else {
    highp float tmpvar_11;
    tmpvar_11 = dot (tmpvar_3, vec3(0.0, 1.0, 0.0));
    if ((tmpvar_11 > 0.3)) {
      bool tmpvar_12;
      if ((tmpvar_6.z >= boxTex.w)) {
        tmpvar_12 = (tmpvar_6.z <= boxTex.x);
      } else {
        tmpvar_12 = bool(0);
      };
      bool tmpvar_13;
      if (tmpvar_12) {
        tmpvar_13 = bool(1);
      } else {
        bool tmpvar_14;
        if ((tmpvar_6.z >= boxTex.y)) {
          tmpvar_14 = (tmpvar_6.z <= boxTex.z);
        } else {
          tmpvar_14 = bool(0);
        };
        tmpvar_13 = tmpvar_14;
      };
      if (tmpvar_13) {
        tmpvar_4 = _Color.xyz;
        tmpvar_5 = _Color.w;
      };
    };
  };
  tmpvar_1 = tmpvar_4;
  tmpvar_2 = tmpvar_5;
  mediump vec3 tmpvar_15;
  tmpvar_15 = normalize (xlv_TEXCOORD3);
  lightDir = tmpvar_15;
  highp vec2 tmpvar_16;
  tmpvar_16 = vec2(dot (xlv_TEXCOORD4, xlv_TEXCOORD4));
  lowp vec4 c_i0;
  c_i0.xyz = ((tmpvar_4 * _LightColor0.xyz) * ((max (0.0, dot (xlv_TEXCOORD2, lightDir)) * (texture2D (_LightTextureB0, tmpvar_16).w * textureCube (_LightTexture0, xlv_TEXCOORD4).w)) * 2.0));
  c_i0.w = tmpvar_5;
  c = c_i0;
  c.w = tmpvar_5;
  gl_FragData[0] = c;
}



#endif"
}

SubProgram "opengl " {
Keywords { "DIRECTIONAL_COOKIE" }
Bind "vertex" Vertex
Bind "normal" Normal
Vector 13 [unity_Scale]
Vector 14 [_WorldSpaceLightPos0]
Matrix 5 [_Object2World]
Matrix 9 [_LightMatrix0]
"!!ARBvp1.0
# 20 ALU
PARAM c[15] = { program.local[0],
		state.matrix.mvp,
		program.local[5..14] };
TEMP R0;
TEMP R1;
DP4 R1.z, vertex.position, c[7];
DP4 R1.x, vertex.position, c[5];
DP4 R1.y, vertex.position, c[6];
MOV R0.xyz, R1;
DP4 R0.w, vertex.position, c[8];
DP4 result.texcoord[4].y, R0, c[10];
DP4 result.texcoord[4].x, R0, c[9];
MUL R0.xyz, vertex.normal, c[13].w;
MOV result.texcoord[1].xyz, R1;
DP3 result.texcoord[2].z, R0, c[7];
DP3 result.texcoord[2].y, R0, c[6];
DP3 result.texcoord[2].x, R0, c[5];
MOV result.texcoord[3].xyz, c[14];
DP4 result.position.w, vertex.position, c[4];
DP4 result.position.z, vertex.position, c[3];
DP4 result.position.y, vertex.position, c[2];
DP4 result.position.x, vertex.position, c[1];
DP3 result.texcoord[0].z, vertex.normal, c[7];
DP3 result.texcoord[0].y, vertex.normal, c[6];
DP3 result.texcoord[0].x, vertex.normal, c[5];
END
# 20 instructions, 2 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "DIRECTIONAL_COOKIE" }
Bind "vertex" Vertex
Bind "normal" Normal
Matrix 0 [glstate_matrix_mvp]
Vector 12 [unity_Scale]
Vector 13 [_WorldSpaceLightPos0]
Matrix 4 [_Object2World]
Matrix 8 [_LightMatrix0]
"vs_2_0
; 20 ALU
dcl_position0 v0
dcl_normal0 v1
dp4 r1.z, v0, c6
dp4 r1.x, v0, c4
dp4 r1.y, v0, c5
mov r0.xyz, r1
dp4 r0.w, v0, c7
dp4 oT4.y, r0, c9
dp4 oT4.x, r0, c8
mul r0.xyz, v1, c12.w
mov oT1.xyz, r1
dp3 oT2.z, r0, c6
dp3 oT2.y, r0, c5
dp3 oT2.x, r0, c4
mov oT3.xyz, c13
dp4 oPos.w, v0, c3
dp4 oPos.z, v0, c2
dp4 oPos.y, v0, c1
dp4 oPos.x, v0, c0
dp3 oT0.z, v1, c6
dp3 oT0.y, v1, c5
dp3 oT0.x, v1, c4
"
}

SubProgram "gles " {
Keywords { "DIRECTIONAL_COOKIE" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying highp vec2 xlv_TEXCOORD4;
varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 unity_Scale;

uniform lowp vec4 _WorldSpaceLightPos0;
uniform highp mat4 _Object2World;
uniform highp mat4 _LightMatrix0;
attribute vec3 _glesNormal;
attribute vec4 _glesVertex;
void main ()
{
  vec3 tmpvar_1;
  tmpvar_1 = normalize (_glesNormal);
  lowp vec3 tmpvar_2;
  lowp vec3 tmpvar_3;
  mediump vec3 tmpvar_4;
  mat3 tmpvar_5;
  tmpvar_5[0] = _Object2World[0].xyz;
  tmpvar_5[1] = _Object2World[1].xyz;
  tmpvar_5[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_6;
  tmpvar_6 = (tmpvar_5 * tmpvar_1);
  tmpvar_2 = tmpvar_6;
  mat3 tmpvar_7;
  tmpvar_7[0] = _Object2World[0].xyz;
  tmpvar_7[1] = _Object2World[1].xyz;
  tmpvar_7[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_8;
  tmpvar_8 = (tmpvar_7 * (tmpvar_1 * unity_Scale.w));
  tmpvar_3 = tmpvar_8;
  highp vec3 tmpvar_9;
  tmpvar_9 = _WorldSpaceLightPos0.xyz;
  tmpvar_4 = tmpvar_9;
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_TEXCOORD0 = tmpvar_2;
  xlv_TEXCOORD1 = (_Object2World * _glesVertex).xyz;
  xlv_TEXCOORD2 = tmpvar_3;
  xlv_TEXCOORD3 = tmpvar_4;
  xlv_TEXCOORD4 = (_LightMatrix0 * (_Object2World * _glesVertex)).xy;
}



#endif
#ifdef FRAGMENT

varying highp vec2 xlv_TEXCOORD4;
varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 _MapWorldOrigin;
uniform highp vec4 _MapTileSize;
uniform highp vec4 _MapSizeInTiles;
uniform sampler2D _LightTexture0;
uniform lowp vec4 _LightColor0;
uniform lowp vec4 _Color;
uniform sampler2D _Boxes;
void main ()
{
  lowp vec4 c;
  lowp vec3 lightDir;
  lowp vec3 tmpvar_1;
  lowp float tmpvar_2;
  highp vec3 tmpvar_3;
  tmpvar_3 = xlv_TEXCOORD0;
  tmpvar_1 = vec3(0.0, 0.0, 0.0);
  tmpvar_2 = 0.0;
  lowp vec3 tmpvar_4;
  lowp float tmpvar_5;
  tmpvar_4 = tmpvar_1;
  tmpvar_5 = tmpvar_2;
  highp vec4 boxTex;
  highp vec3 tmpvar_6;
  tmpvar_6 = (floor ((((xlv_TEXCOORD1 - _MapWorldOrigin.xyz) - vec3(-5.0, 0.01, -5.0)) / _MapTileSize.xyz)).xzy / _MapSizeInTiles.xzy);
  lowp vec4 tmpvar_7;
  tmpvar_7 = texture2D (_Boxes, tmpvar_6.xy);
  boxTex = tmpvar_7;
  bool tmpvar_8;
  if ((boxTex.w == 0.0)) {
    tmpvar_8 = (boxTex.x == 0.0);
  } else {
    tmpvar_8 = bool(0);
  };
  bool tmpvar_9;
  if (tmpvar_8) {
    tmpvar_9 = (boxTex.y == 0.0);
  } else {
    tmpvar_9 = bool(0);
  };
  bool tmpvar_10;
  if (tmpvar_9) {
    tmpvar_10 = (boxTex.z == 0.0);
  } else {
    tmpvar_10 = bool(0);
  };
  if (tmpvar_10) {
  } else {
    highp float tmpvar_11;
    tmpvar_11 = dot (tmpvar_3, vec3(0.0, 1.0, 0.0));
    if ((tmpvar_11 > 0.3)) {
      bool tmpvar_12;
      if ((tmpvar_6.z >= boxTex.w)) {
        tmpvar_12 = (tmpvar_6.z <= boxTex.x);
      } else {
        tmpvar_12 = bool(0);
      };
      bool tmpvar_13;
      if (tmpvar_12) {
        tmpvar_13 = bool(1);
      } else {
        bool tmpvar_14;
        if ((tmpvar_6.z >= boxTex.y)) {
          tmpvar_14 = (tmpvar_6.z <= boxTex.z);
        } else {
          tmpvar_14 = bool(0);
        };
        tmpvar_13 = tmpvar_14;
      };
      if (tmpvar_13) {
        tmpvar_4 = _Color.xyz;
        tmpvar_5 = _Color.w;
      };
    };
  };
  tmpvar_1 = tmpvar_4;
  tmpvar_2 = tmpvar_5;
  lightDir = xlv_TEXCOORD3;
  lowp vec4 c_i0;
  c_i0.xyz = ((tmpvar_4 * _LightColor0.xyz) * ((max (0.0, dot (xlv_TEXCOORD2, lightDir)) * texture2D (_LightTexture0, xlv_TEXCOORD4).w) * 2.0));
  c_i0.w = tmpvar_5;
  c = c_i0;
  c.w = tmpvar_5;
  gl_FragData[0] = c;
}



#endif"
}

SubProgram "glesdesktop " {
Keywords { "DIRECTIONAL_COOKIE" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying highp vec2 xlv_TEXCOORD4;
varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 unity_Scale;

uniform lowp vec4 _WorldSpaceLightPos0;
uniform highp mat4 _Object2World;
uniform highp mat4 _LightMatrix0;
attribute vec3 _glesNormal;
attribute vec4 _glesVertex;
void main ()
{
  vec3 tmpvar_1;
  tmpvar_1 = normalize (_glesNormal);
  lowp vec3 tmpvar_2;
  lowp vec3 tmpvar_3;
  mediump vec3 tmpvar_4;
  mat3 tmpvar_5;
  tmpvar_5[0] = _Object2World[0].xyz;
  tmpvar_5[1] = _Object2World[1].xyz;
  tmpvar_5[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_6;
  tmpvar_6 = (tmpvar_5 * tmpvar_1);
  tmpvar_2 = tmpvar_6;
  mat3 tmpvar_7;
  tmpvar_7[0] = _Object2World[0].xyz;
  tmpvar_7[1] = _Object2World[1].xyz;
  tmpvar_7[2] = _Object2World[2].xyz;
  highp vec3 tmpvar_8;
  tmpvar_8 = (tmpvar_7 * (tmpvar_1 * unity_Scale.w));
  tmpvar_3 = tmpvar_8;
  highp vec3 tmpvar_9;
  tmpvar_9 = _WorldSpaceLightPos0.xyz;
  tmpvar_4 = tmpvar_9;
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_TEXCOORD0 = tmpvar_2;
  xlv_TEXCOORD1 = (_Object2World * _glesVertex).xyz;
  xlv_TEXCOORD2 = tmpvar_3;
  xlv_TEXCOORD3 = tmpvar_4;
  xlv_TEXCOORD4 = (_LightMatrix0 * (_Object2World * _glesVertex)).xy;
}



#endif
#ifdef FRAGMENT

varying highp vec2 xlv_TEXCOORD4;
varying mediump vec3 xlv_TEXCOORD3;
varying lowp vec3 xlv_TEXCOORD2;
varying highp vec3 xlv_TEXCOORD1;
varying lowp vec3 xlv_TEXCOORD0;
uniform highp vec4 _MapWorldOrigin;
uniform highp vec4 _MapTileSize;
uniform highp vec4 _MapSizeInTiles;
uniform sampler2D _LightTexture0;
uniform lowp vec4 _LightColor0;
uniform lowp vec4 _Color;
uniform sampler2D _Boxes;
void main ()
{
  lowp vec4 c;
  lowp vec3 lightDir;
  lowp vec3 tmpvar_1;
  lowp float tmpvar_2;
  highp vec3 tmpvar_3;
  tmpvar_3 = xlv_TEXCOORD0;
  tmpvar_1 = vec3(0.0, 0.0, 0.0);
  tmpvar_2 = 0.0;
  lowp vec3 tmpvar_4;
  lowp float tmpvar_5;
  tmpvar_4 = tmpvar_1;
  tmpvar_5 = tmpvar_2;
  highp vec4 boxTex;
  highp vec3 tmpvar_6;
  tmpvar_6 = (floor ((((xlv_TEXCOORD1 - _MapWorldOrigin.xyz) - vec3(-5.0, 0.01, -5.0)) / _MapTileSize.xyz)).xzy / _MapSizeInTiles.xzy);
  lowp vec4 tmpvar_7;
  tmpvar_7 = texture2D (_Boxes, tmpvar_6.xy);
  boxTex = tmpvar_7;
  bool tmpvar_8;
  if ((boxTex.w == 0.0)) {
    tmpvar_8 = (boxTex.x == 0.0);
  } else {
    tmpvar_8 = bool(0);
  };
  bool tmpvar_9;
  if (tmpvar_8) {
    tmpvar_9 = (boxTex.y == 0.0);
  } else {
    tmpvar_9 = bool(0);
  };
  bool tmpvar_10;
  if (tmpvar_9) {
    tmpvar_10 = (boxTex.z == 0.0);
  } else {
    tmpvar_10 = bool(0);
  };
  if (tmpvar_10) {
  } else {
    highp float tmpvar_11;
    tmpvar_11 = dot (tmpvar_3, vec3(0.0, 1.0, 0.0));
    if ((tmpvar_11 > 0.3)) {
      bool tmpvar_12;
      if ((tmpvar_6.z >= boxTex.w)) {
        tmpvar_12 = (tmpvar_6.z <= boxTex.x);
      } else {
        tmpvar_12 = bool(0);
      };
      bool tmpvar_13;
      if (tmpvar_12) {
        tmpvar_13 = bool(1);
      } else {
        bool tmpvar_14;
        if ((tmpvar_6.z >= boxTex.y)) {
          tmpvar_14 = (tmpvar_6.z <= boxTex.z);
        } else {
          tmpvar_14 = bool(0);
        };
        tmpvar_13 = tmpvar_14;
      };
      if (tmpvar_13) {
        tmpvar_4 = _Color.xyz;
        tmpvar_5 = _Color.w;
      };
    };
  };
  tmpvar_1 = tmpvar_4;
  tmpvar_2 = tmpvar_5;
  lightDir = xlv_TEXCOORD3;
  lowp vec4 c_i0;
  c_i0.xyz = ((tmpvar_4 * _LightColor0.xyz) * ((max (0.0, dot (xlv_TEXCOORD2, lightDir)) * texture2D (_LightTexture0, xlv_TEXCOORD4).w) * 2.0));
  c_i0.w = tmpvar_5;
  c = c_i0;
  c.w = tmpvar_5;
  gl_FragData[0] = c;
}



#endif"
}

}
Program "fp" {
// Fragment combos: 5
//   opengl - ALU: 42 to 53, TEX: 1 to 3
//   d3d9 - ALU: 51 to 61, TEX: 1 to 3
SubProgram "opengl " {
Keywords { "POINT" }
Vector 0 [_LightColor0]
Vector 1 [_MapWorldOrigin]
Vector 2 [_MapTileSize]
Vector 3 [_MapSizeInTiles]
Vector 4 [_Color]
SetTexture 0 [_Boxes] 2D
SetTexture 1 [_LightTexture0] 2D
"!!ARBfp1.0
OPTION ARB_precision_hint_fastest;
# 47 ALU, 2 TEX
PARAM c[7] = { program.local[0..4],
		{ 1, 0, -5, 0.0099999998 },
		{ 0.30000001, 2 } };
TEMP R0;
TEMP R1;
ADD R0.xyz, fragment.texcoord[1], -c[1];
ADD R1.xyz, R0, -c[5].zwzw;
DP3 R1.w, fragment.texcoord[4], fragment.texcoord[4];
RCP R0.x, c[2].x;
RCP R0.z, c[2].z;
RCP R0.y, c[2].y;
MUL R0.xyz, R1, R0;
FLR R1.xyz, R0;
RCP R0.x, c[3].x;
RCP R0.y, c[3].z;
RCP R0.z, c[3].y;
MUL R1.xyz, R1.xzyw, R0;
TEX R0, R1, texture[0], 2D;
TEX R1.w, R1.w, texture[1], 2D;
SGE R1.x, R1.z, R0.y;
SGE R1.y, R0.z, R1.z;
MUL R1.y, R1.x, R1;
SGE R1.x, R1.z, R0.w;
SGE R1.z, R0.x, R1;
MAD_SAT R1.x, R1, R1.z, R1.y;
ABS R1.y, R0.x;
ABS R0.x, R0.w;
ABS R0.y, R0;
ABS R0.z, R0;
CMP R0.w, -R1.y, c[5].y, c[5].x;
CMP R0.x, -R0, c[5].y, c[5];
MUL R0.x, R0, R0.w;
CMP R0.y, -R0, c[5], c[5].x;
MUL R0.x, R0, R0.y;
CMP R0.z, -R0, c[5].y, c[5].x;
MUL R0.x, R0, R0.z;
DP3 R0.z, fragment.texcoord[3], fragment.texcoord[3];
SLT R0.y, c[6].x, fragment.texcoord[0];
CMP R0.x, -R0, c[5].y, c[5];
MUL R0.x, R0, R0.y;
MUL R0.y, R0.x, R1.x;
RSQ R0.z, R0.z;
MUL R1.xyz, R0.z, fragment.texcoord[3];
MOV R0.x, c[5].y;
CMP R0, -R0.y, c[4], R0.x;
DP3 R1.x, fragment.texcoord[2], R1;
MAX R1.x, R1, c[5].y;
MUL R0.xyz, R0, c[0];
MUL R1.x, R1, R1.w;
MUL R0.xyz, R1.x, R0;
MOV result.color.w, R0;
MUL result.color.xyz, R0, c[6].y;
END
# 47 instructions, 2 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "POINT" }
Vector 0 [_LightColor0]
Vector 1 [_MapWorldOrigin]
Vector 2 [_MapTileSize]
Vector 3 [_MapSizeInTiles]
Vector 4 [_Color]
SetTexture 0 [_Boxes] 2D
SetTexture 1 [_LightTexture0] 2D
"ps_2_0
; 57 ALU, 2 TEX
dcl_2d s0
dcl_2d s1
def c5, 5.00000000, -0.01000000, 1.00000000, 0.00000000
def c6, 0.30000001, 2.00000000, 0, 0
dcl t0.xy
dcl t1.xyz
dcl t2.xyz
dcl t3.xyz
dcl t4.xyz
mov r0.xz, c5.x
mov r0.y, c5
add r1.xyz, t1, -c1
add r1.xyz, r1, r0
rcp r2.x, c3.x
rcp r2.y, c3.z
rcp r2.z, c3.y
rcp r0.x, c2.x
rcp r0.z, c2.z
rcp r0.y, c2.y
mul r0.xyz, r1, r0
frc r1.xyz, r0
add r0.xyz, r0, -r1
mov r1.x, r0
mov r1.yz, r0.wzyx
mul r1.xyz, r1, r2
dp3 r0.x, t4, t4
mov r0.xy, r0.x
texld r3, r1, s0
texld r4, r0, s1
add r2.x, r3, -r1.z
add r1.x, r3.z, -r1.z
add r0.x, -r3.y, r1.z
cmp r1.x, r1, c5.z, c5.w
cmp r0.x, r0, c5.z, c5.w
mul_pp r0.x, r0, r1
add r1.x, -r3.w, r1.z
cmp r1.x, r1, c5.z, c5.w
cmp r2.x, r2, c5.z, c5.w
mad_pp_sat r0.x, r1, r2, r0
abs r2.x, r3
abs r1.x, r3.y
abs r3.x, r3.w
cmp r3.x, -r3, c5.z, c5.w
cmp r2.x, -r2, c5.z, c5.w
mul_pp r2.x, r3, r2
cmp r1.x, -r1, c5.z, c5.w
mul_pp r1.x, r2, r1
abs r3.x, r3.z
cmp r3.x, -r3, c5.z, c5.w
add r2.x, -t0.y, c6
mul_pp r1.x, r1, r3
cmp r2.x, r2, c5.w, c5.z
cmp_pp r1.x, -r1, c5.z, c5.w
mul_pp r1.x, r1, r2
mul_pp r1.x, r1, r0
dp3_pp r0.x, t3, t3
mov_pp r2, c4
cmp_pp r1, -r1.x, c5.w, r2
rsq_pp r0.x, r0.x
mul_pp r0.xyz, r0.x, t3
dp3_pp r0.x, t2, r0
mul_pp r1.xyz, r1, c0
max_pp r0.x, r0, c5.w
mul_pp r0.x, r0, r4
mul_pp r0.xyz, r0.x, r1
mul_pp r0.xyz, r0, c6.y
mov_pp r0.w, r1
mov_pp oC0, r0
"
}

SubProgram "gles " {
Keywords { "POINT" }
"!!GLES"
}

SubProgram "glesdesktop " {
Keywords { "POINT" }
"!!GLES"
}

SubProgram "opengl " {
Keywords { "DIRECTIONAL" }
Vector 0 [_LightColor0]
Vector 1 [_MapWorldOrigin]
Vector 2 [_MapTileSize]
Vector 3 [_MapSizeInTiles]
Vector 4 [_Color]
SetTexture 0 [_Boxes] 2D
"!!ARBfp1.0
OPTION ARB_precision_hint_fastest;
# 42 ALU, 1 TEX
PARAM c[7] = { program.local[0..4],
		{ 1, 0, -5, 0.0099999998 },
		{ 0.30000001, 2 } };
TEMP R0;
TEMP R1;
ADD R0.xyz, fragment.texcoord[1], -c[1];
ADD R1.xyz, R0, -c[5].zwzw;
RCP R0.x, c[2].x;
RCP R0.z, c[2].z;
RCP R0.y, c[2].y;
MUL R0.xyz, R1, R0;
FLR R1.xyz, R0;
RCP R0.x, c[3].x;
RCP R0.y, c[3].z;
RCP R0.z, c[3].y;
MUL R1.xyz, R1.xzyw, R0;
TEX R0, R1, texture[0], 2D;
SGE R1.y, R0.z, R1.z;
SGE R1.x, R1.z, R0.y;
MUL R1.w, R1.x, R1.y;
ABS R0.y, R0;
ABS R0.z, R0;
SGE R1.y, R0.x, R1.z;
SGE R1.x, R1.z, R0.w;
MAD_SAT R1.x, R1, R1.y, R1.w;
ABS R1.y, R0.x;
ABS R0.x, R0.w;
CMP R0.w, -R1.y, c[5].y, c[5].x;
CMP R0.x, -R0, c[5].y, c[5];
MUL R0.x, R0, R0.w;
CMP R0.y, -R0, c[5], c[5].x;
MUL R0.x, R0, R0.y;
CMP R0.z, -R0, c[5].y, c[5].x;
MUL R0.x, R0, R0.z;
SLT R0.y, c[6].x, fragment.texcoord[0];
CMP R0.x, -R0, c[5].y, c[5];
MUL R0.x, R0, R0.y;
MUL R0.y, R0.x, R1.x;
MOV R0.x, c[5].y;
CMP R0, -R0.y, c[4], R0.x;
MOV R1.xyz, fragment.texcoord[3];
DP3 R1.x, fragment.texcoord[2], R1;
MUL R0.xyz, R0, c[0];
MAX R1.x, R1, c[5].y;
MUL R0.xyz, R1.x, R0;
MOV result.color.w, R0;
MUL result.color.xyz, R0, c[6].y;
END
# 42 instructions, 2 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "DIRECTIONAL" }
Vector 0 [_LightColor0]
Vector 1 [_MapWorldOrigin]
Vector 2 [_MapTileSize]
Vector 3 [_MapSizeInTiles]
Vector 4 [_Color]
SetTexture 0 [_Boxes] 2D
"ps_2_0
; 51 ALU, 1 TEX
dcl_2d s0
def c5, 5.00000000, -0.01000000, 1.00000000, 0.00000000
def c6, 0.30000001, 2.00000000, 0, 0
dcl t0.xy
dcl t1.xyz
dcl t2.xyz
dcl t3.xyz
mov r0.xz, c5.x
mov r0.y, c5
add r1.xyz, t1, -c1
add r1.xyz, r1, r0
rcp r0.x, c2.x
rcp r0.z, c2.z
rcp r0.y, c2.y
mul r0.xyz, r1, r0
frc r1.xyz, r0
add r0.xyz, r0, -r1
rcp r1.x, c3.x
rcp r1.y, c3.z
rcp r1.z, c3.y
mov r0.yz, r0.wzyx
mul r0.xyz, r0, r1
texld r3, r0, s0
add r2.x, r3, -r0.z
add r1.x, r3.z, -r0.z
add r0.x, -r3.y, r0.z
cmp r1.x, r1, c5.z, c5.w
cmp r0.x, r0, c5.z, c5.w
mul_pp r0.x, r0, r1
add r1.x, -r3.w, r0.z
cmp r1.x, r1, c5.z, c5.w
cmp r2.x, r2, c5.z, c5.w
mad_pp_sat r0.x, r1, r2, r0
abs r2.x, r3
abs r1.x, r3.y
abs r3.x, r3.w
cmp r3.x, -r3, c5.z, c5.w
cmp r2.x, -r2, c5.z, c5.w
mul_pp r2.x, r3, r2
cmp r1.x, -r1, c5.z, c5.w
mul_pp r1.x, r2, r1
abs r3.x, r3.z
cmp r3.x, -r3, c5.z, c5.w
add r2.x, -t0.y, c6
mul_pp r1.x, r1, r3
cmp r2.x, r2, c5.w, c5.z
cmp_pp r1.x, -r1, c5.z, c5.w
mul_pp r1.x, r1, r2
mul_pp r0.x, r1, r0
mov_pp r1, c4
cmp_pp r1, -r0.x, c5.w, r1
mov_pp r2.xyz, t3
dp3_pp r0.x, t2, r2
mul_pp r1.xyz, r1, c0
max_pp r0.x, r0, c5.w
mul_pp r0.xyz, r0.x, r1
mul_pp r0.xyz, r0, c6.y
mov_pp r0.w, r1
mov_pp oC0, r0
"
}

SubProgram "gles " {
Keywords { "DIRECTIONAL" }
"!!GLES"
}

SubProgram "glesdesktop " {
Keywords { "DIRECTIONAL" }
"!!GLES"
}

SubProgram "opengl " {
Keywords { "SPOT" }
Vector 0 [_LightColor0]
Vector 1 [_MapWorldOrigin]
Vector 2 [_MapTileSize]
Vector 3 [_MapSizeInTiles]
Vector 4 [_Color]
SetTexture 0 [_Boxes] 2D
SetTexture 1 [_LightTexture0] 2D
SetTexture 2 [_LightTextureB0] 2D
"!!ARBfp1.0
OPTION ARB_precision_hint_fastest;
# 53 ALU, 3 TEX
PARAM c[7] = { program.local[0..4],
		{ 1, 0, -5, 0.0099999998 },
		{ 0.30000001, 0.5, 2 } };
TEMP R0;
TEMP R1;
TEMP R2;
ADD R0.xyz, fragment.texcoord[1], -c[1];
ADD R1.xyz, R0, -c[5].zwzw;
RCP R0.w, fragment.texcoord[4].w;
RCP R0.x, c[2].x;
RCP R0.z, c[2].z;
RCP R0.y, c[2].y;
MUL R0.xyz, R1, R0;
FLR R1.xyz, R0;
RCP R0.x, c[3].x;
RCP R0.y, c[3].z;
RCP R0.z, c[3].y;
MUL R1.xyz, R1.xzyw, R0;
MAD R0.xy, fragment.texcoord[4], R0.w, c[6].y;
DP3 R0.z, fragment.texcoord[4], fragment.texcoord[4];
TEX R2, R1, texture[0], 2D;
TEX R0.w, R0, texture[1], 2D;
TEX R1.w, R0.z, texture[2], 2D;
ABS R1.x, R2.z;
SGE R0.y, R2.z, R1.z;
SGE R0.x, R1.z, R2.y;
MUL R0.z, R0.x, R0.y;
SGE R0.y, R2.x, R1.z;
SGE R0.x, R1.z, R2.w;
MAD_SAT R0.x, R0, R0.y, R0.z;
ABS R0.z, R2.x;
ABS R0.y, R2.w;
CMP R0.z, -R0, c[5].y, c[5].x;
CMP R0.y, -R0, c[5], c[5].x;
MUL R0.y, R0, R0.z;
ABS R0.z, R2.y;
CMP R0.z, -R0, c[5].y, c[5].x;
MUL R0.y, R0, R0.z;
CMP R1.x, -R1, c[5].y, c[5];
MUL R0.y, R0, R1.x;
SLT R0.z, c[6].x, fragment.texcoord[0].y;
CMP R0.y, -R0, c[5], c[5].x;
MUL R0.y, R0, R0.z;
MUL R0.y, R0, R0.x;
MOV R0.x, c[5].y;
CMP R2, -R0.y, c[4], R0.x;
DP3 R0.z, fragment.texcoord[3], fragment.texcoord[3];
RSQ R0.z, R0.z;
MUL R0.xyz, R0.z, fragment.texcoord[3];
DP3 R0.x, fragment.texcoord[2], R0;
SLT R0.y, c[5], fragment.texcoord[4].z;
MUL R0.y, R0, R0.w;
MUL R0.y, R0, R1.w;
MAX R0.x, R0, c[5].y;
MUL R1.xyz, R2, c[0];
MUL R0.x, R0, R0.y;
MUL R0.xyz, R0.x, R1;
MOV result.color.w, R2;
MUL result.color.xyz, R0, c[6].z;
END
# 53 instructions, 3 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "SPOT" }
Vector 0 [_LightColor0]
Vector 1 [_MapWorldOrigin]
Vector 2 [_MapTileSize]
Vector 3 [_MapSizeInTiles]
Vector 4 [_Color]
SetTexture 0 [_Boxes] 2D
SetTexture 1 [_LightTexture0] 2D
SetTexture 2 [_LightTextureB0] 2D
"ps_2_0
; 61 ALU, 3 TEX
dcl_2d s0
dcl_2d s1
dcl_2d s2
def c5, 5.00000000, -0.01000000, 1.00000000, 0.00000000
def c6, 0.30000001, 0.50000000, 2.00000000, 0
dcl t0.xy
dcl t1.xyz
dcl t2.xyz
dcl t3.xyz
dcl t4
dp3 r2.x, t4, t4
mov r2.xy, r2.x
mov r0.xz, c5.x
mov r0.y, c5
add r1.xyz, t1, -c1
add r1.xyz, r1, r0
rcp r0.x, c2.x
rcp r0.z, c2.z
rcp r0.y, c2.y
mul r0.xyz, r1, r0
frc r1.xyz, r0
add r0.xyz, r0, -r1
mov r0.yz, r0.wzyx
rcp r1.x, c3.x
rcp r1.y, c3.z
rcp r1.z, c3.y
mul r1.xyz, r0, r1
rcp r0.x, t4.w
mad r0.xy, t4, r0.x, c6.y
texld r3, r1, s0
texld r4, r2, s2
texld r0, r0, s1
add r2.x, r3, -r1.z
add r1.x, r3.z, -r1.z
add r0.x, -r3.y, r1.z
cmp r1.x, r1, c5.z, c5.w
cmp r0.x, r0, c5.z, c5.w
mul_pp r0.x, r0, r1
add r1.x, -r3.w, r1.z
cmp r1.x, r1, c5.z, c5.w
cmp r2.x, r2, c5.z, c5.w
mad_pp_sat r0.x, r1, r2, r0
abs r2.x, r3
abs r1.x, r3.y
abs r3.x, r3.w
cmp r3.x, -r3, c5.z, c5.w
cmp r2.x, -r2, c5.z, c5.w
mul_pp r2.x, r3, r2
cmp r1.x, -r1, c5.z, c5.w
mul_pp r1.x, r2, r1
abs r3.x, r3.z
cmp r3.x, -r3, c5.z, c5.w
add r2.x, -t0.y, c6
mul_pp r1.x, r1, r3
cmp r2.x, r2, c5.w, c5.z
cmp_pp r1.x, -r1, c5.z, c5.w
mul_pp r1.x, r1, r2
mul_pp r0.x, r1, r0
mov_pp r2, c4
cmp_pp r1, -r0.x, c5.w, r2
dp3_pp r0.x, t3, t3
mul_pp r2.xyz, r1, c0
rsq_pp r1.x, r0.x
cmp r0.x, -t4.z, c5.w, c5.z
mul_pp r0.x, r0, r0.w
mul_pp r1.xyz, r1.x, t3
dp3_pp r1.x, t2, r1
mul_pp r0.x, r0, r4
max_pp r1.x, r1, c5.w
mul_pp r0.x, r1, r0
mul_pp r0.xyz, r0.x, r2
mul_pp r0.xyz, r0, c6.z
mov_pp r0.w, r1
mov_pp oC0, r0
"
}

SubProgram "gles " {
Keywords { "SPOT" }
"!!GLES"
}

SubProgram "glesdesktop " {
Keywords { "SPOT" }
"!!GLES"
}

SubProgram "opengl " {
Keywords { "POINT_COOKIE" }
Vector 0 [_LightColor0]
Vector 1 [_MapWorldOrigin]
Vector 2 [_MapTileSize]
Vector 3 [_MapSizeInTiles]
Vector 4 [_Color]
SetTexture 0 [_Boxes] 2D
SetTexture 1 [_LightTextureB0] 2D
SetTexture 2 [_LightTexture0] CUBE
"!!ARBfp1.0
OPTION ARB_precision_hint_fastest;
# 49 ALU, 3 TEX
PARAM c[7] = { program.local[0..4],
		{ 1, 0, -5, 0.0099999998 },
		{ 0.30000001, 2 } };
TEMP R0;
TEMP R1;
TEMP R2;
TEX R1.w, fragment.texcoord[4], texture[2], CUBE;
ADD R0.xyz, fragment.texcoord[1], -c[1];
ADD R1.xyz, R0, -c[5].zwzw;
DP3 R0.w, fragment.texcoord[4], fragment.texcoord[4];
RCP R0.x, c[2].x;
RCP R0.z, c[2].z;
RCP R0.y, c[2].y;
MUL R0.xyz, R1, R0;
FLR R1.xyz, R0;
RCP R0.x, c[3].x;
RCP R0.y, c[3].z;
RCP R0.z, c[3].y;
MUL R0.xyz, R1.xzyw, R0;
TEX R2, R0, texture[0], 2D;
TEX R0.w, R0.w, texture[1], 2D;
SGE R0.y, R2.z, R0.z;
SGE R0.x, R0.z, R2.y;
MUL R1.x, R0, R0.y;
SGE R0.y, R2.x, R0.z;
SGE R0.x, R0.z, R2.w;
MAD_SAT R0.x, R0, R0.y, R1;
ABS R0.z, R2.x;
ABS R0.y, R2.w;
ABS R1.x, R2.z;
CMP R0.z, -R0, c[5].y, c[5].x;
CMP R0.y, -R0, c[5], c[5].x;
MUL R0.y, R0, R0.z;
ABS R0.z, R2.y;
CMP R0.z, -R0, c[5].y, c[5].x;
MUL R0.y, R0, R0.z;
CMP R1.x, -R1, c[5].y, c[5];
MUL R0.y, R0, R1.x;
SLT R0.z, c[6].x, fragment.texcoord[0].y;
CMP R0.y, -R0, c[5], c[5].x;
MUL R0.y, R0, R0.z;
MUL R0.y, R0, R0.x;
MOV R0.x, c[5].y;
CMP R2, -R0.y, c[4], R0.x;
DP3 R0.z, fragment.texcoord[3], fragment.texcoord[3];
RSQ R0.z, R0.z;
MUL R0.xyz, R0.z, fragment.texcoord[3];
DP3 R0.x, fragment.texcoord[2], R0;
MUL R0.y, R0.w, R1.w;
MAX R0.x, R0, c[5].y;
MUL R1.xyz, R2, c[0];
MUL R0.x, R0, R0.y;
MUL R0.xyz, R0.x, R1;
MOV result.color.w, R2;
MUL result.color.xyz, R0, c[6].y;
END
# 49 instructions, 3 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "POINT_COOKIE" }
Vector 0 [_LightColor0]
Vector 1 [_MapWorldOrigin]
Vector 2 [_MapTileSize]
Vector 3 [_MapSizeInTiles]
Vector 4 [_Color]
SetTexture 0 [_Boxes] 2D
SetTexture 1 [_LightTextureB0] 2D
SetTexture 2 [_LightTexture0] CUBE
"ps_2_0
; 58 ALU, 3 TEX
dcl_2d s0
dcl_2d s1
dcl_cube s2
def c5, 5.00000000, -0.01000000, 1.00000000, 0.00000000
def c6, 0.30000001, 2.00000000, 0, 0
dcl t0.xy
dcl t1.xyz
dcl t2.xyz
dcl t3.xyz
dcl t4.xyz
mov r0.xz, c5.x
mov r0.y, c5
add r1.xyz, t1, -c1
add r1.xyz, r1, r0
rcp r2.x, c3.x
rcp r2.y, c3.z
rcp r2.z, c3.y
rcp r0.x, c2.x
rcp r0.z, c2.z
rcp r0.y, c2.y
mul r0.xyz, r1, r0
frc r1.xyz, r0
add r0.xyz, r0, -r1
mov r1.x, r0
mov r1.yz, r0.wzyx
mul r1.xyz, r1, r2
dp3 r0.x, t4, t4
mov r2.xy, r0.x
texld r3, r1, s0
texld r4, r2, s1
texld r0, t4, s2
add r2.x, r3, -r1.z
add r1.x, r3.z, -r1.z
add r0.x, -r3.y, r1.z
cmp r1.x, r1, c5.z, c5.w
cmp r0.x, r0, c5.z, c5.w
mul_pp r0.x, r0, r1
add r1.x, -r3.w, r1.z
cmp r1.x, r1, c5.z, c5.w
cmp r2.x, r2, c5.z, c5.w
mad_pp_sat r0.x, r1, r2, r0
abs r2.x, r3
abs r1.x, r3.y
abs r3.x, r3.w
cmp r3.x, -r3, c5.z, c5.w
cmp r2.x, -r2, c5.z, c5.w
mul_pp r2.x, r3, r2
cmp r1.x, -r1, c5.z, c5.w
mul_pp r1.x, r2, r1
abs r3.x, r3.z
cmp r3.x, -r3, c5.z, c5.w
add r2.x, -t0.y, c6
mul_pp r1.x, r1, r3
cmp r2.x, r2, c5.w, c5.z
cmp_pp r1.x, -r1, c5.z, c5.w
mul_pp r1.x, r1, r2
mul_pp r0.x, r1, r0
mov_pp r2, c4
cmp_pp r1, -r0.x, c5.w, r2
dp3_pp r0.x, t3, t3
rsq_pp r0.x, r0.x
mul_pp r0.xyz, r0.x, t3
dp3_pp r0.x, t2, r0
mul_pp r2.xyz, r1, c0
mul r1.x, r4, r0.w
max_pp r0.x, r0, c5.w
mul_pp r0.x, r0, r1
mul_pp r0.xyz, r0.x, r2
mul_pp r0.xyz, r0, c6.y
mov_pp r0.w, r1
mov_pp oC0, r0
"
}

SubProgram "gles " {
Keywords { "POINT_COOKIE" }
"!!GLES"
}

SubProgram "glesdesktop " {
Keywords { "POINT_COOKIE" }
"!!GLES"
}

SubProgram "opengl " {
Keywords { "DIRECTIONAL_COOKIE" }
Vector 0 [_LightColor0]
Vector 1 [_MapWorldOrigin]
Vector 2 [_MapTileSize]
Vector 3 [_MapSizeInTiles]
Vector 4 [_Color]
SetTexture 0 [_Boxes] 2D
SetTexture 1 [_LightTexture0] 2D
"!!ARBfp1.0
OPTION ARB_precision_hint_fastest;
# 44 ALU, 2 TEX
PARAM c[7] = { program.local[0..4],
		{ 1, 0, -5, 0.0099999998 },
		{ 0.30000001, 2 } };
TEMP R0;
TEMP R1;
TEX R1.w, fragment.texcoord[4], texture[1], 2D;
ADD R0.xyz, fragment.texcoord[1], -c[1];
ADD R1.xyz, R0, -c[5].zwzw;
RCP R0.x, c[2].x;
RCP R0.z, c[2].z;
RCP R0.y, c[2].y;
MUL R0.xyz, R1, R0;
FLR R1.xyz, R0;
RCP R0.x, c[3].x;
RCP R0.y, c[3].z;
RCP R0.z, c[3].y;
MUL R1.xyz, R1.xzyw, R0;
TEX R0, R1, texture[0], 2D;
SGE R1.x, R1.z, R0.y;
SGE R1.y, R0.z, R1.z;
MUL R1.y, R1.x, R1;
SGE R1.x, R1.z, R0.w;
SGE R1.z, R0.x, R1;
MAD_SAT R1.x, R1, R1.z, R1.y;
ABS R1.y, R0.x;
ABS R0.x, R0.w;
ABS R0.y, R0;
ABS R0.z, R0;
CMP R0.w, -R1.y, c[5].y, c[5].x;
CMP R0.x, -R0, c[5].y, c[5];
MUL R0.x, R0, R0.w;
CMP R0.y, -R0, c[5], c[5].x;
MUL R0.x, R0, R0.y;
CMP R0.z, -R0, c[5].y, c[5].x;
MUL R0.x, R0, R0.z;
SLT R0.y, c[6].x, fragment.texcoord[0];
CMP R0.x, -R0, c[5].y, c[5];
MUL R0.x, R0, R0.y;
MUL R0.y, R0.x, R1.x;
MOV R0.x, c[5].y;
CMP R0, -R0.y, c[4], R0.x;
MOV R1.xyz, fragment.texcoord[3];
DP3 R1.x, fragment.texcoord[2], R1;
MAX R1.x, R1, c[5].y;
MUL R0.xyz, R0, c[0];
MUL R1.x, R1, R1.w;
MUL R0.xyz, R1.x, R0;
MOV result.color.w, R0;
MUL result.color.xyz, R0, c[6].y;
END
# 44 instructions, 2 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "DIRECTIONAL_COOKIE" }
Vector 0 [_LightColor0]
Vector 1 [_MapWorldOrigin]
Vector 2 [_MapTileSize]
Vector 3 [_MapSizeInTiles]
Vector 4 [_Color]
SetTexture 0 [_Boxes] 2D
SetTexture 1 [_LightTexture0] 2D
"ps_2_0
; 52 ALU, 2 TEX
dcl_2d s0
dcl_2d s1
def c5, 5.00000000, -0.01000000, 1.00000000, 0.00000000
def c6, 0.30000001, 2.00000000, 0, 0
dcl t0.xy
dcl t1.xyz
dcl t2.xyz
dcl t3.xyz
dcl t4.xy
mov r0.xz, c5.x
mov r0.y, c5
add r1.xyz, t1, -c1
add r1.xyz, r1, r0
rcp r0.x, c2.x
rcp r0.z, c2.z
rcp r0.y, c2.y
mul r0.xyz, r1, r0
frc r1.xyz, r0
add r0.xyz, r0, -r1
mov r0.yz, r0.wzyx
rcp r1.x, c3.x
rcp r1.y, c3.z
rcp r1.z, c3.y
mul r1.xyz, r0, r1
texld r3, r1, s0
texld r0, t4, s1
add r2.x, r3, -r1.z
add r1.x, r3.z, -r1.z
add r0.x, -r3.y, r1.z
cmp r1.x, r1, c5.z, c5.w
cmp r0.x, r0, c5.z, c5.w
mul_pp r0.x, r0, r1
add r1.x, -r3.w, r1.z
cmp r1.x, r1, c5.z, c5.w
cmp r2.x, r2, c5.z, c5.w
mad_pp_sat r0.x, r1, r2, r0
abs r2.x, r3
abs r1.x, r3.y
abs r3.x, r3.w
cmp r3.x, -r3, c5.z, c5.w
cmp r2.x, -r2, c5.z, c5.w
mul_pp r2.x, r3, r2
cmp r1.x, -r1, c5.z, c5.w
mul_pp r1.x, r2, r1
abs r3.x, r3.z
cmp r3.x, -r3, c5.z, c5.w
add r2.x, -t0.y, c6
mul_pp r1.x, r1, r3
cmp r2.x, r2, c5.w, c5.z
cmp_pp r1.x, -r1, c5.z, c5.w
mul_pp r1.x, r1, r2
mul_pp r0.x, r1, r0
mov_pp r2, c4
cmp_pp r1, -r0.x, c5.w, r2
mov_pp r0.xyz, t3
dp3_pp r0.x, t2, r0
max_pp r0.x, r0, c5.w
mul_pp r0.x, r0, r0.w
mul_pp r1.xyz, r1, c0
mul_pp r0.xyz, r0.x, r1
mul_pp r0.xyz, r0, c6.y
mov_pp r0.w, r1
mov_pp oC0, r0
"
}

SubProgram "gles " {
Keywords { "DIRECTIONAL_COOKIE" }
"!!GLES"
}

SubProgram "glesdesktop " {
Keywords { "DIRECTIONAL_COOKIE" }
"!!GLES"
}

}
	}

#LINE 69

    }
    Fallback "Diffuse"
}
