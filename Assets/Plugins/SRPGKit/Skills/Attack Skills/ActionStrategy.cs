using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ActionStrategy {
	[System.NonSerialized]
	public Skill owner;
	
	public bool canCrossWalls=true;
	public float xyRangeMin=1, xyRangeMax=1;
	public float zRangeUpMin=0, zRangeUpMax=1, zRangeDownMin=0, zRangeDownMax=2;

	public bool canEffectCrossWalls=true;
	public float xyRadius;
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
		if(canCrossWalls && (dz < 0 ? (dz > zRangeDownMin || dz <= zRangeDownMax) : (dz < zRangeUpMin || dz >= zRangeUpMax))) {
			return PathDecision.PassOnly;
		}
		return PathDecision.Normal;
	}

	public virtual PathDecision PathNodeIsValidRadius(Vector3 start, PathNode pn, Character c) {
		float dz = pn.position.z - start.z;
		float absDZ = Mathf.Abs(dz);
		//TODO: replace with some type of collision check?
		if(canEffectCrossWalls && (dz < 0 ? (absDZ > zRadiusUp) : (dz > 0 ? absDZ > zRadiusDown : false))) {
			return PathDecision.PassOnly;
		}
		return PathDecision.Normal;
	}
	
	public virtual PathNode[] GetValidActions() {
		//for now, you get radius-3 around current tile
		Vector3 tc = owner.map.InverseTransformPointWorld(owner.character.transform.position-owner.transformOffset);
		return owner.map.PathsAround(
			tc, 
			xyRangeMin, xyRangeMax, 
			zRangeDownMin, zRangeDownMax, 
			zRangeUpMin, zRangeUpMax,
			false,
			PathNodeIsValidRange
		);
	}	
	
	public virtual PathNode[] GetTargetedTiles(Vector3 targetTC) {
		return owner.map.PathsAround(
			targetTC, 
			xyRadius, xyRadius, 
			zRadiusDown, zRadiusDown, 
			zRadiusUp, zRadiusUp,
			false,
			PathNodeIsValidRadius
		);	
	}
}