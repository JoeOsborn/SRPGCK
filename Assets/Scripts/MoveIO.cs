using UnityEngine;
using System.Collections;

//TODO: PickTileMoveIO needs a way to wait the character facing a direction. 
//      ContinuousWithinTilesMoveIO may need something like that for its mouse-based inputs, too.
//	    Actually, WaitIO may be different from MoveIO.

public class MoveIO : MonoBehaviour {

	[System.NonSerialized]
	public Map map;

	[System.NonSerialized]
	public Character character;
	
	[HideInInspector]
	public bool isActive;
	
	public virtual void Start () {
		character = GetComponent<Character>();
	}
	
	public virtual void Update () {
		if(map == null) {
			if(this.transform.parent != null) {
				map = this.transform.parent.GetComponent<Map>();
			}
			if(map == null) { 
				Debug.Log("Characters must be children of Map objects!");
				return; 
			}
		}
		if(!isActive) { return; }
	}
	
	protected virtual void PresentMoves() {
		isActive = true;
	}
	
	protected virtual void FinishMove() {
		isActive = false;
		map.scheduler.EndMovePhase(character);
	}
	
	public virtual void PresentValidMoves() {
		SendMessage("PresentMoves", null);
	}
	
	public virtual void EndMovePhase() {
		if(isActive) {
			SendMessage("FinishMove", null);
		}
	}

	public virtual void Deactivate() {
		EndMovePhase();
	}
		
	public virtual void TemporaryMove(Vector3 tc) {
		MoveExecutor me = GetComponent<MoveExecutor>();
		Vector3 src = me.temporaryPosition;
		me.TemporaryMoveTo(tc);
		map.scheduler.CharacterMovedTemporary(
			character, 
			map.InverseTransformPointWorld(src), 
			map.InverseTransformPointWorld(me.temporaryDestination)
		);
	}

	public virtual void IncrementalMove(Vector3 tc) {
		MoveExecutor me = GetComponent<MoveExecutor>();
		Vector3 src = me.position;
		me.IncrementalMoveTo(tc);
		map.scheduler.CharacterMovedIncremental(
			character, 
			map.InverseTransformPointWorld(src), 
			map.InverseTransformPointWorld(me.destination)
		);
	}
	
	public virtual void PerformMove(Vector3 tc) {
		//MOVE EXECUTOR: move there and then report back to character
		MoveExecutor me = GetComponent<MoveExecutor>();
		Vector3 src = me.destination;
		me.MoveTo(tc);
		map.scheduler.CharacterMoved(
			character, 
			map.InverseTransformPointWorld(src),
			map.InverseTransformPointWorld(me.destination)
		);
		EndMovePhase();
	}
}
