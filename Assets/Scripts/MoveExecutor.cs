using UnityEngine;
using System.Collections;

public class MoveExecutor : MonoBehaviour {
	Map map;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if(map == null) {
			if(this.transform.parent != null) {
				map = this.transform.parent.GetComponent<Map>();
			}
			if(map == null) { 
				Debug.Log("Characters must be children of Map objects!");
				return; 
			}
		}
	}

	public void TemporaryMoveTo(Vector3 tileCoord) {
		this.transform.position = map.TransformPointWorld(tileCoord)+new Vector3(0,5f,0);
	}
	
	public void MoveTo(Vector3 tileCoord) {
		this.transform.position = map.TransformPointWorld(tileCoord)+new Vector3(0,5f,0);		
	}
}
