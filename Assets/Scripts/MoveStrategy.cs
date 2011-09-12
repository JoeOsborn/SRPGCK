using UnityEngine;
using System.Collections;

public class MoveStrategy : MonoBehaviour {
	Map map;
	
	void Start () {
	
	}
	
	void Update () {
		if(map == null && transform.parent != null) { map = transform.parent.GetComponent<Map>(); }
	}
	
	public Vector3[] GetValidMoves() {
		//for now, you get radius-3 around current tile
		//TODO: this "-(0,5,0)" pattern shows up a lot -- maybe make y-offset a property on character.
		Vector3 tc = map.InverseTransformPointWorld(transform.position-new Vector3(0,5,0));
		return map.TilesNear(tc, 3, 3);
	}
}
