using UnityEngine;
using System.Collections;

[AddComponentMenu("")]
public class TeamRoundsPickOneSchedulerFeedback : MonoBehaviour {
	protected Map map;
	
	public Color wrongTeamColor=new Color(0.3f, 0.3f, 0.3f, 1);
	public Color alreadyUsedColor=new Color(0.6f, 0.6f, 0.6f, 1);
	public Color activeColor=new Color(1,1,1,1);
	protected Color standardColor;

	// Use this for initialization
	void Start () {
		//shade other team, shade used
		standardColor = GetComponent<MeshRenderer>().material.color;
	}
	
	// Update is called once per frame
	void Update () {
		if(map == null) {
			if(this.transform.parent != null) {
				map = this.transform.parent.GetComponent<Map>();
			}
			if(map == null) { 
				Debug.Log("Characters must be children of Map objects!");
				return; 
			}
		}
	}
	
	public void RoundBegan(int newTeamID) {
		Character c = GetComponent<Character>();
		if(c != null && newTeamID == c.EffectiveTeamID) {
			//unshade
			MeshRenderer mr = GetComponent<MeshRenderer>();
			mr.material.color = standardColor;
/*			Debug.Log("back to "+standardColor);*/
		}
	}
	public void RoundEnded(int teamID) {
		Character c = GetComponent<Character>();
		if(c != null && teamID == c.EffectiveTeamID) {
			//shade
			MeshRenderer mr = GetComponent<MeshRenderer>();
			mr.material.color = wrongTeamColor;
/*			Debug.Log("change to wrong team "+wrongTeamColor);*/
		}
	}
	public void Activate() {
		//extra highlight?
		MeshRenderer mr = GetComponent<MeshRenderer>();
		standardColor = mr.material.color;
		mr.material.color = activeColor;
/*		Debug.Log("store "+standardColor);*/
/*		Debug.Log("change to active "+activeColor);*/
	}
	public void Deactivate() {
		//shade
/*		Debug.Log("change to already used "+alreadyUsedColor);*/
		MeshRenderer mr = GetComponent<MeshRenderer>();
		mr.material.color = alreadyUsedColor;
	}
}
