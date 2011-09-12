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
	
	public virtual void PerformMove(Vector3 tc) {
		//MOVE EXECUTOR: move there and then report back to character
		GetComponent<MoveExecutor>().MoveTo(tc);
		//CHARACTER: is move over?
		//SCHEDULER: "If active character has moved on this turn, deactivate it"
		map.scheduler.Deactivate(character);	
	}
}
