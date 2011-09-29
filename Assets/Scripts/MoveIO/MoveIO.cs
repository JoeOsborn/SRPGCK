using UnityEngine;
using System.Collections;

public class MoveIO : MonoBehaviour {
	protected Map map;
	protected Character character;
	
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
	}
	
	public virtual void PresentMoves() {

	}
	
	public virtual void Deactivate() {

	}
	
	public virtual void TemporaryMove(Vector3 tc) {
		MoveExecutor me = GetComponent<MoveExecutor>();
		Vector3 src = me.temporaryPosition;
		me.TemporaryMoveTo(tc);
		map.scheduler.CharacterMovedTemporary(character, map.InverseTransformPointWorld(src), map.InverseTransformPointWorld(me.temporaryDestination));
	}
	
	public virtual void PerformMove(Vector3 tc) {
		//MOVE EXECUTOR: move there and then report back to character
		MoveExecutor me = GetComponent<MoveExecutor>();
		Vector3 src = me.destination;
		me.MoveTo(tc);
		map.scheduler.CharacterMoved(character, map.InverseTransformPointWorld(src), map.InverseTransformPointWorld(me.destination));
	}
}
