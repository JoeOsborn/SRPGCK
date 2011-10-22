using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Billboard))]
public class Sprite : MonoBehaviour {
	
	public string startingAnimation;

	public int frameWidth;
	public int frameHeight;
	
	private int frameRows;
	private int frameColumns;
	
	private int currentFrame;
	private int frameIndex;
	private float frameTimer;

	[SerializeField]
	private List<string> an_data;
	public SpriteAnimation currentAnimation;
	
	public Dictionary<string, SpriteAnimation> animationTable;

	void Awake () {
		RestoreAnimationTable();
		CreateDefaultAnimation();
		
		SetupUVs();
		
		if(startingAnimation != ""){
			Debug.Log("playing starting animation");
			PlayAnimation(startingAnimation);
		} else {
			PlayAnimation("_default");
		}
	}
	
	public void SetupUVs(){
		Billboard billboard = GetComponent<Billboard>();

		frameColumns = (int) Mathf.Floor(billboard.areaRect.width / frameWidth);
		frameRows = (int) Mathf.Floor(billboard.areaRect.height / frameHeight);

		frameTimer = 0.0f;
	}
	
	/*void CreateAnimationTable(){
		animationTable = new Dictionary<string, SpriteAnimation>();
		for(int i = 0; i < animations.Count; i++){
			animationTable[animations[i].name] = animations[i];
		}
	}*/
	
	public void SerializeAnimationTable(){
		//Debug.Log("saving animation table");
		an_data = new List<string>();
		if(animationTable != null){
			foreach(KeyValuePair<string, SpriteAnimation> val in animationTable){
				an_data.Add(val.Value.SerializeAnimation());
			}
		}
	}
	
	public void RestoreAnimationTable(){
		if(an_data != null){
			animationTable = new Dictionary<string, SpriteAnimation>();
			foreach(string val in an_data){
				SpriteAnimation an = new SpriteAnimation(val);

				animationTable.Add(an.name, an);
			}
		}
	}
	
	public void CreateDefaultAnimation(){
		if(!animationTable.ContainsKey("_default")){
			int[] nf = new int[1];
			nf[0] = 0;
			SpriteAnimation defaultAnim = new SpriteAnimation("_default",nf);
			animationTable.Add("_default", defaultAnim);
		}
	}
	
	public void PlayAnimation(string name){
		if((currentAnimation == null || currentAnimation.name != name) && animationTable.ContainsKey(name)){
			currentAnimation = animationTable[name];
			currentFrame = 0;
			frameIndex = currentAnimation.frames[currentFrame];
			CalculateFrame();
		}
	}
	
	public void UpdateAnimation(float d){
		frameTimer += d;
		if(currentAnimation != null){
			if(frameTimer >= currentAnimation.frameRate){
				frameTimer -= currentAnimation.frameRate;
				frameIndex = currentAnimation.frames[currentFrame];
				currentFrame++;
				if(currentAnimation.looped && currentFrame == currentAnimation.frames.Length){
					currentFrame = 0;
				}
				CalculateFrame();
			}
		}
	}
	
	void CalculateFrame(){
		Billboard billboard = GetComponent<Billboard>();
		Vector2 size = new Vector2((1.0f / frameColumns) * billboard.transRect.width, (1.0f /frameRows)*billboard.transRect.height);
		
		float uIndex = (float) frameIndex % frameColumns;
		float vIndex = (float) Mathf.Floor(frameIndex / frameColumns);
		
		Vector2 offset = new Vector2(billboard.transRect.x + uIndex * size.x, 1 - billboard.transRect.y - size.y - vIndex * size.y);
		billboard.spriteSheet.SetTextureOffset("_MainTex", offset);
		billboard.spriteSheet.SetTextureScale("_MainTex", size);
		
	}
	
	void Update () {
		UpdateAnimation(Time.deltaTime);
	}
	
}
