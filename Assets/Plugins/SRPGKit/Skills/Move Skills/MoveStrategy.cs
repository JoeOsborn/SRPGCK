using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MoveStrategy {
	[System.NonSerialized]
	public Skill owner;
	
	public bool canCrossWalls=false;
	public float zDelta=3;
	public float xyRange=3;
	public bool canCrossEnemies=false;
	
	virtual public void Update () {
		
	}
	
	virtual public void Activate() {
	}
	virtual public void Deactivate() {
	}
}
