using UnityEngine;

[System.Serializable]
public class Initiative {
	public Character character;
	public float initiative;
	public Initiative(Character c, float i) {
		character = c;
		initiative = i;
	}
}
