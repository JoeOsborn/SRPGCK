using UnityEngine;
using System.Collections.Generic;

public class CharacterSpecialMoveReport {
	public Character character;
	public string moveType;
	public Skill cause;
	public Vector3 start;
	public int amount;
	public float direction;
	public bool canCrossWalls, canCrossCharacters, canGlide;
	public float zUpMax, zDownMax;
	public PathNode endOfPath;
	public int remainingVelocity;
	public float dropDistance;
	public List<Character> collidedCharacters;
	public CharacterSpecialMoveReport(
		Character c,
		string mt,
		Skill s,
		Vector3 st,
		int amt,
		float dir,
		bool crossWalls, bool crossChars, bool glide,
		float zUp, float zDown,
		PathNode path,
		int remainingVel, float drop,
		List<Character> collided
	) {
		character = c;
		moveType = mt;
		cause = s;
		start = st;
		amount = amt;
		direction = dir;
		canCrossWalls = crossWalls;
		canCrossCharacters = crossChars;
		canGlide = glide;
		zUpMax = zUp;
		zDownMax = zDown;
		endOfPath = path;
		remainingVelocity = remainingVel;
		dropDistance = drop;
		collidedCharacters = collided;
	}
}
