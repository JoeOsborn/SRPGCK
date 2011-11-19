using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(Billboard))]
public class BillboardEditor : Editor {

	SerializedObject billboardObj;
	Billboard billboard;
	
	public void OnEnable(){
		billboardObj = new SerializedObject(target);
		billboard = target as Billboard;
		//if(billboard.editorMesh == null) billboard.editorMesh = billboard.GetComponent<MeshFilter>().sharedMesh;
		//Debug.Log(billboard.atlas);
	}
	
	public void SetupMaterials(){
		if(billboard.editorMaterial == null){
			Shader shad = Shader.Find("Transparent/Diffuse");
			billboard.editorMaterial = new Material(shad);
		}
		billboard.editorMaterial.SetTexture("_MainTex", billboard.atlas);
		billboard.spriteSheet = billboard.editorMaterial;
		
		billboard.SetupMaterial();
		billboard.renderer.sharedMaterial = billboard.spriteSheet;
		
	}
	
	public void SetupMeshes(){
		if(billboard.editorMesh != null){
			DestroyImmediate(billboard.editorMesh);
		}
		billboard.editorMesh = new Mesh();
		billboard.currentMesh = billboard.editorMesh;
		billboard.RedefineMesh();
		billboard.GetComponent<MeshFilter>().sharedMesh = billboard.currentMesh;
	}
	
	public void SetupSprite(){
		Sprite sprite = billboard.GetComponent<Sprite>();
		sprite.frameWidth = billboard.atlas.width;
		sprite.frameHeight = billboard.atlas.height;
		
		sprite.CreateDefaultAnimation();
		sprite.startingAnimation = "_default";
		
		sprite.SetupUVs();
	}
	
	public void ResetAspectRatio(){
		//float t = 1 / billboard.atlas.width;
		Vector3 ns = new Vector3(Mathf.Floor(billboard.transform.localScale.x), Mathf.Floor(billboard.transform.localScale.x), 1);
		if(billboard.repeatTexture){
			ns = new Vector3(billboard.columns, billboard.rows, 1);
		}
		billboard.transform.localScale = ns;
	}
	
	override public void OnInspectorGUI(){
		billboardObj.Update();
		bool rp = billboard.repeatTexture;
		EditorGUILayout.PropertyField(billboardObj.FindProperty("atlas"));
		if(billboard.atlas != null){
			EditorGUILayout.PropertyField(billboardObj.FindProperty("areaRect"));
			billboard.repeatTexture = EditorGUILayout.Toggle("Repeat Texture:", billboard.repeatTexture);
			if(billboard.repeatTexture){
				EditorGUILayout.PropertyField(billboardObj.FindProperty("columns"));
				EditorGUILayout.PropertyField(billboardObj.FindProperty("rows"));
			}
			if(GUILayout.Button("Reset Aspect Ratio")){
				ResetAspectRatio();
			}
		}
		
		Texture2D a = billboard.atlas;
		Rect r = billboard.areaRect;
		int cl = billboard.columns;
		int ro = billboard.rows;
		billboardObj.ApplyModifiedProperties();
		if(a != billboard.atlas){
			billboard.columns = 1;
			billboard.rows = 1;
			if(billboard.atlas != null){
				billboard.areaRect = new Rect(0,0, billboard.atlas.width, billboard.atlas.height);
			}
			ResetAspectRatio();
			SetupMeshes();
			SetupMaterials();
			if(billboard.GetComponent<Sprite>() != null){
				SetupSprite();
			}
		}
		
		if(r != billboard.areaRect || rp != billboard.repeatTexture || ro != billboard.rows || cl != billboard.columns){
			if(!billboard.repeatTexture){
				billboard.columns = 1;
				billboard.rows = 1;
			}
			
			SetupMeshes();
			SetupMaterials();
			
			
		}
		
	}
}
