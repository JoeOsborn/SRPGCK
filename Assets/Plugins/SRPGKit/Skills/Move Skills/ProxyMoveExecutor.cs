using UnityEngine;
using System.Collections;

//does not influence the object's transform at all. 
//it is only used to track moves made by another script.
//for example, you may have a MoveIO that handles movement and animation
//on its own; in that case, use a ProxyMoveExecutor to "dummy out" the ME features.
[System.Serializable]
public class ProxyMoveExecutor : MoveExecutor {
	override public void TemporaryMoveTo(PathNode pn, MoveFinished callback, float timeout=10.0f) {
		Vector3 src = temporaryPosition;
		UpdatePositions();
		callback(src, temporaryDestNode, true);
	}

	override public void IncrementalMoveTo(PathNode pn, MoveFinished callback, float timeout=10.0f) {
		MoveTo(pn, callback, timeout);
	}
	
	override public void MoveTo(PathNode pn, MoveFinished callback, float timeout=10.0f) {
		Vector3 src = position;
		UpdatePositions();
		callback(src, destNode, true);
	}
	
	public void UpdatePositions() {
		destNode = new PathNode(owner.character.transform.position, null, 0);
		position = destination;
		temporaryPosition = position;
		temporaryDestNode = destNode;
	}
}
