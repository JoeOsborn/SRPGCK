using UnityEngine;
using System.Collections;

public class MoveExecutor : MonoBehaviour {
	Map map;
	public Vector3 position;
	public Vector3 destination;
	public Vector3 temporaryPosition;
	public Vector3 temporaryDestination;
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
			destination = transform.position;
			position = destination;
			temporaryPosition = destination;
			temporaryDestination = destination;
		}
	}

	public void TemporaryMoveTo(Vector3 tileCoord) {
		temporaryDestination = map.TransformPointWorld(tileCoord)+new Vector3(0,5f,0);
		temporaryPosition = destination;
		this.transform.position = destination;
	}
	
	public void MoveTo(Vector3 tileCoord) {
		destination = map.TransformPointWorld(tileCoord)+new Vector3(0,5f,0);
		position = destination;
		temporaryDestination = destination;
		temporaryPosition = destination;
		this.transform.position = destination;
	}
}
