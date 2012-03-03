using UnityEngine;

public class CharacterSpecialMoveReport {
	public Character character;
	public string moveType;
	public Skill cause;
	public Vector3 start;
	public int amount;
	public float direction;
	public bool canCrossWalls, canCrossCharacters;
	public PathNode endOfPath;
	public int remainingVelocity;
	public float dropDistance;
	public Character collidedCharacter;
	public CharacterSpecialMoveReport(Character c, string mt, Skill s, Vector3 st, int amt, float dir, bool crossWalls, bool crossChars, PathNode path, int remainingVel, float drop, Character collided) {
		character = c;
		moveType = mt;
		cause = s;
		start = st;
		amount = amt;
		direction = dir;
		canCrossWalls = crossWalls;
		canCrossCharacters = crossChars;
		endOfPath = path;
		remainingVelocity = remainingVel;
		dropDistance = drop;
		collidedCharacter = collided;
	}
}
