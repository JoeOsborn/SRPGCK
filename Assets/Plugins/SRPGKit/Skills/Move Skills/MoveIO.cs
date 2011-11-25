using UnityEngine;
using System.Collections;

[System.Serializable]
public class MoveIO {
	[System.NonSerialized]
	public MoveSkill owner;
	
	[HideInInspector]
	public bool isActive;
	
	public virtual void Activate () {
		isActive = true;
	}
	
	public virtual void Update () {

	}
	
	public virtual void PresentMoves() {

	}
	
	protected virtual void FinishMove() {
		if(isActive) {
			owner.map.BroadcastMessage("SkillApplied", owner, SendMessageOptions.DontRequireReceiver);
		}
		owner.DeactivateSkill();
	}
	
	public virtual void Deactivate() {
		isActive = false;
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
		MoveExecutor me = owner.executor;
		me.TemporaryMoveTo(pn, delegate(Vector3 src, PathNode endNode, bool finishedNicely) {
			owner.scheduler.CharacterMovedTemporary(
				owner.character, 
				owner.map.InverseTransformPointWorld(src), 
				owner.map.InverseTransformPointWorld(endNode.pos)
			);
		});
	}

	public virtual void IncrementalMoveToPathNode(PathNode pn) {
		MoveExecutor me = owner.executor;
		me.IncrementalMoveTo(pn, delegate(Vector3 src, PathNode endNode, bool finishedNicely) {
			owner.scheduler.CharacterMovedIncremental(
				owner.character, 
				owner.map.InverseTransformPointWorld(src), 
				owner.map.InverseTransformPointWorld(endNode.pos)
			);
		});
	}
	
	public virtual void PerformMoveToPathNode(PathNode pn) {
		MoveExecutor me = owner.executor;
		me.MoveTo(pn, delegate(Vector3 src, PathNode endNode, bool finishedNicely) {
			owner.scheduler.CharacterMoved(
				owner.character, 
				owner.map.InverseTransformPointWorld(src), 
				owner.map.InverseTransformPointWorld(endNode.pos)
			);
			FinishMove();
		});
	}	
	
}
