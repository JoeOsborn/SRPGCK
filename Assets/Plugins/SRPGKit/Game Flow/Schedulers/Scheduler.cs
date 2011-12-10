using UnityEngine;
using System.Collections.Generic;

//TODO: Schedulers may also need to be responsible for scheduling timed attacks, 
//      on seconds or on CT or end of turn or end of phase or whatever, according to the scheduler.

public class Scheduler : MonoBehaviour {
	[HideInInspector]
	public List<Character> characters;
	[HideInInspector]
	public Character activeCharacter;
	
	protected Map map;
	
	virtual public void AddCharacter(Character c) {
		if(!characters.Contains(c)) {
			characters.Add(c);
		}
	}
	
	virtual public void RemoveCharacter(Character c) {
		characters.Remove(c);
	}
	
	public bool ContainsCharacter(Character c) {
		return characters.Contains(c);
	}
	
	public virtual void SkillApplied(Skill s) {
		
	}
	
	public virtual void Activate(Character c, object context=null) {
		activeCharacter = c;
		c.SendMessage("Activate", context, SendMessageOptions.RequireReceiver);
		map.BroadcastMessage("ActivatedCharacter", c, SendMessageOptions.DontRequireReceiver);
	}
	
	public virtual void Deactivate(Character c=null, object context=null) {
		if(c == null) { c = activeCharacter; }
		if(activeCharacter == c) { activeCharacter = null; }
		c.SendMessage("Deactivate", context, SendMessageOptions.RequireReceiver);
		map.BroadcastMessage("DeactivatedCharacter", c, SendMessageOptions.DontRequireReceiver);
	}	

	public virtual void Start () {
	
	}
	
	public virtual void CharacterMoved(Character c, Vector3 src, Vector3 dest) {
		
	}
	public virtual void CharacterMovedIncremental(Character c, Vector3 src, Vector3 dest) {
		CharacterMovedTemporary(c, src, dest);
	}
	public virtual void CharacterMovedTemporary(Character c, Vector3 src, Vector3 dest) {
		
	}
	
	public virtual void Update () {
		if(map == null) { map = transform.parent.GetComponent<Map>(); }
		if(activeCharacter != null && activeCharacter.isActive) { return; }
		if(activeCharacter != null && !activeCharacter.isActive) { Deactivate(activeCharacter); }
	}
}
