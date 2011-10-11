using UnityEngine;
using System.Collections;

public class MoveExecutor : MonoBehaviour {
	[System.NonSerialized]
	public Map map;
	public Vector3 position;
	public Vector3 destination;
	public Vector3 temporaryPosition;
	public Vector3 temporaryDestination;

	virtual public void Start () {

	}
	
	virtual public void Update () {
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

	virtual public void TemporaryMoveTo(Vector3 tileCoord) {
		temporaryDestination = map.TransformPointWorld(tileCoord)+new Vector3(0,5f,0);
		temporaryPosition = destination;
		this.transform.position = destination;
	}

	virtual public void IncrementalMoveTo(Vector3 tileCoord) {
		destination = map.TransformPointWorld(tileCoord)+new Vector3(0,5f,0);
		position = destination;
		temporaryDestination = destination;
		temporaryPosition = destination;
		this.transform.position = destination;
	}
	
	virtual public void MoveTo(Vector3 tileCoord) {
		IncrementalMoveTo(tileCoord);
	}
}
