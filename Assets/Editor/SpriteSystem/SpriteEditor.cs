using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(Sprite))]
public class SpriteEditor : Editor {
	
	SerializedObject spriteObj;
	Sprite sprite;
	bool showAnimations;
	Vector2 animationScrollPosition;
	int nextAnimationNum = 0;
	bool animationDirty;
	Material tempMat;
	//bool spriteSetup = false;
	
	double lastUpdateTime;
	
	public void OnEnable(){
		//Debug.Log("enable");
		spriteObj = new SerializedObject(target);
		sprite = target as Sprite;
		sprite.RestoreAnimationTable();
		nextAnimationNum = sprite.animationTable.Count;
		animationScrollPosition = Vector2.zero;
		sprite.CreateDefaultAnimation();
		//SetupSprite();
		sprite.SetupUVs();
		lastUpdateTime = EditorApplication.timeSinceStartup;

		EditorApplication.update = TestUpdate;
	}
	
	/*public void SetupSprite(){
		if(sprite.spriteAtlas != null){
			if(sprite.editorMaterial == null){
				//Debug.Log("setup editorMaterial");
				Shader shad = Shader.Find("Transparent/Diffuse");
				sprite.editorMaterial = new Material(shad);
				sprite.editorMaterial.SetTexture("_MainTex", sprite.spriteAtlas);
				sprite.spriteSheet = sprite.editorMaterial;
				sprite.renderer.sharedMaterial = sprite.editorMaterial;
			}
			sprite.SetupUVs();
			sprite.PlayAnimation(sprite.startingAnimation);
		}
	}*/
	
	public void TestUpdate(){
		if(sprite.GetComponent<Billboard>().atlas != null){
			sprite.PlayAnimation(sprite.startingAnimation);
			sprite.UpdateAnimation((float) (EditorApplication.timeSinceStartup - lastUpdateTime));
			lastUpdateTime = EditorApplication.timeSinceStartup;
		}
	}
	
	public void OnDisable(){
		EditorApplication.update = null;
		//Debug.Log("disable");
		sprite.SerializeAnimationTable();
		
	}
	
	override public void OnInspectorGUI(){
		spriteObj.Update();
		//EditorGUILayout.PropertyField(spriteObj.FindProperty("spriteAtlas"));
		if(sprite.GetComponent<Billboard>().atlas != null){
			int fw = sprite.frameWidth;
			int fh = sprite.frameHeight;
			//Rect a = new Rect(sprite.areaRect.x, sprite.areaRect.y, sprite.areaRect.width, sprite.areaRect.height);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Frame Width:", GUILayout.Width(80));
			sprite.frameWidth = EditorGUILayout.IntField(sprite.frameWidth, GUILayout.Width(30));
			GUILayout.Label("Frame Height:", GUILayout.Width(80));
			sprite.frameHeight = EditorGUILayout.IntField(sprite.frameHeight, GUILayout.Width(30));
			EditorGUILayout.EndHorizontal();
			//EditorGUILayout.PropertyField(spriteObj.FindProperty("areaRect"));
			showAnimations = EditorGUILayout.Foldout(showAnimations, "Animations:");
			if(showAnimations){
				animationScrollPosition = EditorGUILayout.BeginScrollView(animationScrollPosition, true, false, GUILayout.Height(sprite.animationTable.Count*24 + 40));
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("name", GUILayout.Width(70));
				GUILayout.Label("#fr", GUILayout.Width(24));
				GUILayout.Label("fps", GUILayout.Width(30));
				GUILayout.Label("frames");
				EditorGUILayout.EndHorizontal();
				List<SpriteAnimation> toRemove = new List<SpriteAnimation>();
				foreach(KeyValuePair<string, SpriteAnimation> val in sprite.animationTable){
					if(DisplayAnimationEditor(val.Value)) toRemove.Add(val.Value);
				}
				EditorGUILayout.EndScrollView();
				for(int i = 0; i < toRemove.Count; i++){
					animationDirty = true;
					sprite.animationTable.Remove(toRemove[i].name);
					//sprite.SerializeAnimationTable();
					//animations.Remove(toRemove[i]);
				}
				if(GUILayout.Button("Add Animation")){
					animationDirty = true;
					int[] fr = new int[1];
					fr[0] = 0;
					SpriteAnimation newAnim = new SpriteAnimation("anim"+nextAnimationNum, fr);
					sprite.animationTable.Add(newAnim.name, newAnim);
					//sprite.SerializeAnimationTable();
					//animations.Add(new SpriteAnimation("anim"+nextAnimationNum, fr));
					nextAnimationNum++;
				}
				
				if(sprite.startingAnimation == null) sprite.startingAnimation = "";
				string anim = sprite.startingAnimation;
				sprite.startingAnimation = EditorGUILayout.TextField("Default: ", sprite.startingAnimation);
				if(anim != sprite.startingAnimation){
					if(sprite.animationTable.ContainsKey(sprite.startingAnimation)){
						sprite.SetupUVs();
						//Debug.Log("playing: "+sprite.startingAnimation);
						sprite.PlayAnimation(sprite.startingAnimation);
					}
				}
				if(animationDirty){
					animationDirty = false;
					sprite.SerializeAnimationTable();
				}
			}
			
			//reset uvs if we've changed frames:
			if(sprite.frameWidth != fw || sprite.frameHeight != fh){
				sprite.GetComponent<Billboard>().RedefineMesh();
				sprite.SetupUVs();
			}
		}
		//Texture2D os = sprite.spriteAtlas;
		spriteObj.ApplyModifiedProperties();
		/*if(os == null && sprite.spriteAtlas != null){
			sprite.frameWidth = sprite.spriteAtlas.width;
			sprite.frameHeight = sprite.spriteAtlas.height;
			sprite.areaRect = new Rect(0,0, sprite.frameWidth, sprite.frameHeight);
			sprite.framesPerSecond = 1;
			int[] nf = new int[1];
			nf[0] = 0;
			SpriteAnimation defaultAnim = new SpriteAnimation("_default",nf);
			sprite.animationTable.Add("_default", defaultAnim);
			animations.Add(defaultAnim);
			sprite.startingAnimation = "_default";
			
			SetupSprite();	
		}*/
	}
	
	public bool DisplayAnimationEditor(SpriteAnimation anim){
		EditorGUILayout.BeginHorizontal();
		anim.name = EditorGUILayout.TextField(anim.name, GUILayout.Width(70));
		int nl = EditorGUILayout.IntField(anim.frames.Length, GUILayout.Width(24));
		float fps = 1 / anim.frameRate;
		float ofps = fps;
		fps = EditorGUILayout.FloatField(fps, GUILayout.Width(30));
		if(ofps != fps) animationDirty = true;
		anim.frameRate = 1 / fps;
		if(nl > anim.frames.Length){
			animationDirty = true;
			int[] na = new int[nl];
			anim.frames.CopyTo(na,0);
			anim.frames = na;
		} else if(nl < anim.frames.Length && nl != 0){
			animationDirty = true;
			int[] na = new int[nl];
			for(int i = 0; i < na.Length; i++){
				na[i] = anim.frames[i];
			}
			anim.frames = na;
		}
		for(int i = 0; i < anim.frames.Length; i++){
			int oldf = anim.frames[i];
			anim.frames[i] = EditorGUILayout.IntField(anim.frames[i], GUILayout.Width(24));
			if(oldf != anim.frames[i]) animationDirty = true;
		}
		bool ret = GUILayout.Button("-", GUILayout.Width(20));
		EditorGUILayout.EndHorizontal();
		return ret;
	}
}
