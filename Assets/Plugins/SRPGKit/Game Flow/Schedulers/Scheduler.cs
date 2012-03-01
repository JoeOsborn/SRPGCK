using UnityEngine;
using System.Collections.Generic;

//TODO: Schedulers may also need to be responsible for scheduling timed attacks,
//      on seconds or on CT or end of turn or end of phase or whatever, according to the scheduler.

public class Scheduler : MonoBehaviour {
	[HideInInspector]
	public List<Character> characters;
	[HideInInspector]
	public Character activeCharacter;
	[HideInInspector]
	public bool begun=false;
	
	Skill pendingDeactivationSkill;
	Character pendingDeactivationCharacter;
	

	Map _map;
	protected Map map { get {
		if(_map == null) {
			_map = transform.parent.GetComponent<Map>();
		}
		return _map;
	} }

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
		c.SendMessage(
			"Activate",
			context,
			SendMessageOptions.RequireReceiver
		);
		map.BroadcastMessage(
			"ActivatedCharacter",
			c,
			SendMessageOptions.DontRequireReceiver
		);
	}

	public virtual void Deactivate(Character c=null, object context=null) {
		if(c == null) { c = activeCharacter; }
		if(activeCharacter == c) { 
			c.SendMessage("Deactivate", context, SendMessageOptions.RequireReceiver);
			map.BroadcastMessage(
				"DeactivatedCharacter",
				c,
				SendMessageOptions.DontRequireReceiver
			);
			activeCharacter = null; 
			pendingDeactivationSkill = null;
			pendingDeactivationCharacter = null;
		}
	}
	public void SkillDeactivated(Skill s) {
		if(s == pendingDeactivationSkill) {
			if(pendingDeactivationCharacter == activeCharacter) {
				Deactivate(pendingDeactivationCharacter, s);
			}
			pendingDeactivationSkill = null;
			pendingDeactivationCharacter = null;
		}
	}
	public virtual void DeactivateAfterSkillApplication(Character c, Skill skill) {
		pendingDeactivationSkill = skill;
		pendingDeactivationCharacter = c;
	}

	public virtual void Start () {

	}

	public virtual void CharacterMoved(Character c, Vector3 src, Vector3 dest, PathNode endOfPath) {
		map.BroadcastMessage(
			"MovedCharacter",
			new CharacterMoveReport(c, src, dest, endOfPath),
			SendMessageOptions.DontRequireReceiver
		);
	}
	public virtual void CharacterMovedIncremental(Character c, Vector3 src, Vector3 dest, PathNode endOfPath) {
		map.BroadcastMessage(
			"MovedCharacterIncremental",
			new CharacterMoveReport(c, src, dest, endOfPath),
			SendMessageOptions.DontRequireReceiver
		);
	}
	public virtual void CharacterMovedTemporary(Character c, Vector3 src, Vector3 dest, PathNode endOfPath) {
		map.BroadcastMessage(
			"MovedCharacterTemporary",
			new CharacterMoveReport(c, src, dest, endOfPath),
			SendMessageOptions.DontRequireReceiver
		);
	}

	protected virtual void Begin() {
		begun = true;
	}

	public virtual void Update () {

	}

	public virtual void FixedUpdate () {
		if(!begun) { Begin(); }
		if(activeCharacter != null && activeCharacter.isActive) { return; }
		if(activeCharacter != null && !activeCharacter.isActive) { Deactivate(activeCharacter); }
	}
}
