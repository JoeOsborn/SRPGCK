using UnityEngine;
using System.Collections;

[System.Serializable]
public class GridMoveStrategy : MoveStrategy {

	override public void Start () {
		base.Start();
	}
	
	public virtual PathDecision PathNodeIsValid(PathNode pn, Character c) {
		if(c != null) {
			if(pn.distance > xyRange) { return PathDecision.Invalid; }
			if(!canCrossEnemies && c.EffectiveTeamID != owner.character.EffectiveTeamID) { return PathDecision.Invalid; }
			return PathDecision.PassOnly;
		}
		if(canCrossWalls && pn.dz > zDelta) {
			return PathDecision.PassOnly;
		}
		return PathDecision.Normal;
	}
	
	public virtual PathNode[] GetValidMoves() {
		//for now, you get radius-3 around current tile
		//TODO: this "-(0,5,0)" pattern shows up a lot -- maybe make y-offset a property on character.
		Vector3 tc = owner.map.InverseTransformPointWorld(owner.character.transform.position-new Vector3(0,5,0));
		return owner.map.PathsAround(tc, xyRange, zDelta, PathNodeIsValid);
	}
}
