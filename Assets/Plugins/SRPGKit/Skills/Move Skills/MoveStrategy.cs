using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MoveStrategy {
	[System.NonSerialized]
	public Skill owner;
	
	public bool canCrossWalls=false;
	public bool canCrossEnemies=false;
	[HideInInspector]
	public float zDelta=3;
	[HideInInspector]
	public float xyRange=3;
	
	virtual public void Update () {
		
	}
	
	virtual public void Activate() {
	}
	virtual public void Deactivate() {
	}
}
