using UnityEngine;
using System.Collections.Generic;

public class CharacterSpecialMoveReport {
	public Character character;
	public string moveType;
	public Region lineMove;
	public Skill cause;
	public Vector3 start;
	public PathNode endOfPath;
	public float direction;
	public float amount;
	public float remainingVelocity;
	public float dropDistance;
	public List<Character> collidedCharacters;
	public CharacterSpecialMoveReport(
		Character c,
		string mt,
		Region lm,
		Skill s,
		Vector3 st,
		PathNode path,
		float dir,
		float amt,
		float remainingVel,
		float drop,
		List<Character> collided
	) {
		character = c;
		moveType = mt;
		lineMove = lm;
		cause = s;
		start = st;
		endOfPath = path;
		direction = dir;
		amount = amt;
		remainingVelocity = remainingVel;
		dropDistance = drop;
		collidedCharacters = collided;
	}
}
