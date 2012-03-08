using UnityEngine;

[System.Serializable]
public class CharacterMoveReport {
	public Character character;
	public Vector3 src;
	public Vector3 dest;
	public PathNode endOfPath;
	public CharacterMoveReport(Character c, Vector3 s, Vector3 d, PathNode eop) {
		character = c;
		src = s;
		dest = d;
		endOfPath = eop;
	}
}

