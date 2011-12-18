using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class ActionStrategy {
	public bool canCrossWalls=true;
	public bool canEffectCrossWalls=true;
	public bool canCrossEnemies=true;
	public bool canEffectCrossEnemies=true;

	[System.NonSerialized]
	[HideInInspector]
	public Skill owner;
	
	[HideInInspector]
	public float xyRangeMin=1, xyRangeMax=1;
	[HideInInspector]
	public float zRangeUpMin=0, zRangeUpMax=1, zRangeDownMin=0, zRangeDownMax=2;

	[HideInInspector]
	public float xyRadius;
	[HideInInspector]
	public float zRadiusUp=0, zRadiusDown=2;
	
	virtual public void Update () {
		
	}
	
	virtual public void Activate() {
	
	}
	
	virtual public void Deactivate() {
	
	}
	
	public virtual PathDecision PathNodeIsValidRange(Vector3 start, PathNode pn, Character c) {
		float dz = pn.position.z - start.z;
		//TODO: replace with some type of collision check?
		if(c != null && c != owner.character) {
			if(c.EffectiveTeamID != owner.character.EffectiveTeamID) { return canCrossEnemies ? PathDecision.PassOnly : PathDecision.Invalid; }
			else { return PathDecision.PassOnly; }
		}
		if(canCrossWalls && (dz == 0 ? 
			(zRangeDownMin != 0 && zRangeUpMin != 0) : 
			(dz < 0 ? (dz > -zRangeDownMin || dz <= -zRangeDownMax) : 
								(dz < zRangeUpMin || dz >= zRangeUpMax)))) {
			return PathDecision.PassOnly;
		}
		return PathDecision.Normal;
	}

	public virtual PathDecision PathNodeIsValidRadius(Vector3 start, PathNode pn, Character c) {
		float dz = pn.position.z - start.z;
		float absDZ = Mathf.Abs(dz);
		//TODO: replace with some type of collision check?
		if(c != null && !canEffectCrossEnemies) {
			if(c.EffectiveTeamID != owner.character.EffectiveTeamID) { return PathDecision.Invalid; }			
		}
		if(canEffectCrossWalls && (dz < 0 ? (absDZ > zRadiusUp) : (dz > 0 ? absDZ > zRadiusDown : false))) {
			return PathDecision.PassOnly;
		}
		return PathDecision.Normal;
	}
	
	public virtual PathNode[] GetValidMoves() {
		return GetValidMoves(
			owner.character.TilePosition, 
			xyRangeMin, xyRangeMax, 
			zRangeDownMin, zRangeDownMax, 
			zRangeUpMin, zRangeUpMax
		);
	}
	public virtual PathNode[] GetValidMoves(Vector3 tc, float xyrmn, float xyrmx, float zrdmn, float zrdmx, float zrumn, float zrumx) {
		return owner.map.PathsAround(
			tc, 
			xyrmn, xyrmx,
			zrdmn, zrdmx, 
			zrumn, zrumx, 
			true, false, 
			PathNodeIsValidRange
		);
	}
	
	public virtual PathNode[] GetValidActions() {
		Vector3 tc = owner.character.TilePosition;		
		return owner.map.PathsAround(
			tc, 
			xyRangeMin, xyRangeMax, 
			zRangeDownMin, zRangeDownMax, 
			zRangeUpMin, zRangeUpMax,
			false,
			true,
			PathNodeIsValidRange
		);
	}	
	
	public virtual PathNode[] GetTargetedTiles(Quaternion q) {
		//pfeh, who knows? figure out regions first
		return GetTargetedTiles(owner.character.TilePosition);
	}
	
	public virtual PathNode[] GetTargetedTiles(Vector3 targetTC) {
		return owner.map.PathsAround(
			targetTC, 
			0, xyRadius, 
			0, zRadiusDown, 
			0, zRadiusUp,
			false,
			true,
			PathNodeIsValidRadius
		);
	}
	
	public virtual PathNode[] GetReactionTiles(Vector3 attackerTC) {
		PathNode[] validNodes = GetValidActions();
		Debug.Log("valid reaction nodes: "+validNodes.Length);
		if(validNodes.Any(n => Vector3.Distance(n.position, attackerTC) < 0.1)) {
			return GetTargetedTiles(attackerTC);
		}
		return new PathNode[]{};
	}
	
	public virtual List<Character> CharactersForTargetedTiles(PathNode[] tiles) {
		List<Character> targets = new List<Character>();
		foreach(PathNode pn in tiles) {
			Character c = owner.map.CharacterAt(pn.pos);
			if(c != null) {
				targets.Add(c);
			}
		}
		return targets;
	}
}