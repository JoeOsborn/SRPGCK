using UnityEngine;
using System.Collections;

[System.Serializable]
public class GridMoveStrategy : MoveStrategy {

	public virtual PathDecision PathNodeIsValid(Vector3 start, PathNode pn, Character c) {
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
		Vector3 tc = owner.character.TilePosition;
		return owner.map.PathsAround(
			tc, 
			0, xyRange, 
			0, zDelta, 
			0, zDelta, 
			true, 
			false, 
			PathNodeIsValid
		);
	}
}
