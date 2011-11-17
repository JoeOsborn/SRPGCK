using UnityEngine;
using System.Collections;

//does not provide any UI for performing moves. Simply tracks active/inactive move status. 
//You may already have a custom script that listens to Activate, Deactivate, and other events, and
//handles movement using standard CharacterController- or physics-type techniques. In that case,
//use a ProxyMoveIO and add code that calls EndMovePhase() on it when the move should end.
//In many such cases, you will also want to use a ProxyMoveExecutor.
public class ProxyMoveIO : MoveIO {	
	
	Vector3 lastPosition=Vector3.zero;
	
	override public void Start() {
		base.Start();
		lastPosition = transform.position;
	}
	
	override public void Update () {
		base.Update();
		if(character == null || !character.isActive) { return; }
		if(!isActive) { return; }
		if(lastPosition != transform.position) {
			lastPosition = transform.position;
			IncrementalMove(transform.position);
		}
	}
	
}
