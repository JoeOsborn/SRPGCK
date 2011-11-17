using UnityEngine;
using System.Collections;

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
		TemporaryMoveToPathNode(new PathNode(tc, null, 0));
	}

	public virtual void IncrementalMove(Vector3 tc) {
		IncrementalMoveToPathNode(new PathNode(tc, null, 0));
	}
	
	public virtual void PerformMove(Vector3 tc) {
		PerformMoveToPathNode(new PathNode(tc, null, 0));
	}
	
	public virtual void TemporaryMoveToPathNode(PathNode pn) {
		MoveExecutor me = GetComponent<MoveExecutor>();
		me.TemporaryMoveTo(pn, delegate(Vector3 src, PathNode endNode, bool finishedNicely) {
			map.scheduler.CharacterMovedTemporary(
				character, 
				map.InverseTransformPointWorld(src), 
				map.InverseTransformPointWorld(endNode.pos)
			);
		});
	}

	public virtual void IncrementalMoveToPathNode(PathNode pn) {
		MoveExecutor me = GetComponent<MoveExecutor>();
		me.IncrementalMoveTo(pn, delegate(Vector3 src, PathNode endNode, bool finishedNicely) {
			map.scheduler.CharacterMovedIncremental(
				character, 
				map.InverseTransformPointWorld(src), 
				map.InverseTransformPointWorld(endNode.pos)
			);
		});
	}
	
	public virtual void PerformMoveToPathNode(PathNode pn) {
		MoveExecutor me = GetComponent<MoveExecutor>();
		me.MoveTo(pn, delegate(Vector3 src, PathNode endNode, bool finishedNicely) {
			map.scheduler.CharacterMoved(
				character, 
				map.InverseTransformPointWorld(src), 
				map.InverseTransformPointWorld(endNode.pos)
			);
			EndMovePhase();
		});
	}	
	
}
