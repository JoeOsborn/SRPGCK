using UnityEngine;
using System.Collections.Generic;

public class MoveStrategy : MonoBehaviour {
	protected Map map;
	protected Character character;
	
	public bool canCrossWalls=false;
	public int zDelta=3;
	public int xyRange=3;
	public bool canCrossEnemies=false;
	
	void Start () {
	
	}
	
	void Update () {
		if(map == null && transform.parent != null) { map = transform.parent.GetComponent<Map>(); }
		if(character == null && GetComponent<Character>() != null) { character = GetComponent<Character>(); }
	}
	
	public PathNode[] GetValidMoves() {
		//for now, you get radius-3 around current tile
		//TODO: this "-(0,5,0)" pattern shows up a lot -- maybe make y-offset a property on character.
		Vector3 tc = map.InverseTransformPointWorld(transform.position-new Vector3(0,5,0));
		int ourTeam = character.GetEffectiveTeamID();
		return map.PathsAround(tc, xyRange, zDelta, delegate (PathNode pn, Character c) {
			if(c != null) {
				if(pn.distance > xyRange) { return PathDecision.Invalid; }
				if(!canCrossEnemies && c.GetEffectiveTeamID() != ourTeam) { return PathDecision.Invalid; }
				return PathDecision.PassOnly;
			}
			if(canCrossWalls && pn.dz > zDelta) {
				return PathDecision.PassOnly;
			}
			return PathDecision.Normal;
		});
	}
}
