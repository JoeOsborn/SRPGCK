using System;
using System.Collections;

public class SpriteAnimation {

	public string name;
	public int[] frames;
	public bool looped;
	public float delay;
	public float frameRate;
	
	public SpriteAnimation(string name, int[] frames){
		this.name = name;
		this.frames = frames;
		this.looped = true;
		this.delay = 0.0f;
		this.frameRate = 1.0f;
	}
	
	public SpriteAnimation(string data){
		RestoreAnimation(data);	
	}
	
	public string SerializeAnimation(){
		string ret = name + "," + looped + "," + delay + ","+frameRate;
		for(int i = 0; i < frames.Length; i++){
			ret += "," + frames[i];
		}
		return ret;
	}
	
	public void RestoreAnimation(string data){
		char[] delimiters = { ',' };
		string[] animData = data.Split(delimiters);
		name = animData[0];
		looped = Convert.ToBoolean(animData[1]);
		delay = (float) Convert.ToDouble(animData[2]);
		frameRate = (float) Convert.ToDouble(animData[3]);
		frames = new int[animData.Length - 4];
		for(int i = 4; i < animData.Length; i++){
			frames[i-4] = Convert.ToInt32(animData[i]);
		}
	}
		
}
