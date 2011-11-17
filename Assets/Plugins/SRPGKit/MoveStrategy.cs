using UnityEngine;
using System.Collections.Generic;

public class MoveStrategy : MonoBehaviour {
	protected Map map;
	protected Character character;
	
	public bool canCrossWalls=false;
	public float zDelta=3;
	public float xyRange=3;
	public bool canCrossEnemies=false;
	
	virtual public void Start () {
	
	}
	
	virtual public void Update () {
		if(map == null && transform.parent != null) { map = transform.parent.GetComponent<Map>(); }
		if(character == null && GetComponent<Character>() != null) { character = GetComponent<Character>(); }
	}
}
