using UnityEngine;
using System.Collections;

//does not influence the object's transform at all. 
//it is only used to track moves made by another script.
//for example, you may have a MoveIO that handles movement and animation
//on its own; in that case, use a ProxyMoveExecutor to "dummy out" the ME features.
public class ProxyMoveExecutor : MoveExecutor {
	override public void TemporaryMoveTo(Vector3 tileCoord) {
		MoveTo(tileCoord);
	}

	override public void IncrementalMoveTo(Vector3 tileCoord) {
		MoveTo(tileCoord);
	}
	
	override public void MoveTo(Vector3 tileCoord) {
		destination = transform.position;
		position = destination;
		temporaryPosition = destination;
		temporaryDestination = destination;
	}
}
