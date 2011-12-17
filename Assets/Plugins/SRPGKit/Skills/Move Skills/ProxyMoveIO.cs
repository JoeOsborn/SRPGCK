using UnityEngine;
using System.Collections;

//does not provide any UI for performing moves. Simply tracks active/inactive move status. 
//You may already have a custom script that listens to Activate, Deactivate, and other events, and
//handles movement using standard CharacterController- or physics-type techniques. In that case,
//create a move skill that uses these proxies and calls FinishMove on the moveio when the move ends.
//In many such cases, you will also want to use a ProxyMoveExecutor.
[System.Serializable]
public class ProxyMoveIO : MoveIO {	
	
	Vector3 lastPosition=Vector3.zero;
	
	override public void Activate() {
		base.Activate();
		lastPosition = owner.character.transform.position;
	}
	
	override public void Update () {
		base.Update();
		if(owner.character == null || !owner.character.isActive) { return; }
		if(!isActive) { return; }
		if(lastPosition != owner.character.transform.position) {
			lastPosition = owner.character.transform.position;
			owner.IncrementalMove(owner.character.transform.position);
		}
	}
	
}
