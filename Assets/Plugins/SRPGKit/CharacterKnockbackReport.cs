using UnityEngine;

public class CharacterKnockbackReport {
	public Character character;
	public Vector3 start;
	public int amount;
	public float direction;
	public PathNode endOfPath;
	public int remainingVelocity;
	public float dropDistance;
	public Character collidedCharacter;
	public CharacterKnockbackReport(Character c, Vector3 st, int amt, float dir, PathNode path, int remainingVel, float drop, Character collided) {
		character = c;
		start = st;
		amount = amt;
		direction = dir;
		endOfPath = path;
		remainingVelocity = remainingVel;
		dropDistance = drop;
		collidedCharacter = collided;
	}
}
