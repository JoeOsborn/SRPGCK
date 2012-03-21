using UnityEngine;
using System.Collections.Generic;

//TODO: Schedulers may also need to be responsible for scheduling timed attacks,
//      on seconds or on CT or end of turn or end of phase or whatever, according to the scheduler.

[AddComponentMenu("")]
public class Scheduler : MonoBehaviour {
	public List<Character> characters;
	public Character activeCharacter;
	public bool begun=false;

	public SkillDef pendingDeactivationSkill;
	public Character pendingDeactivationCharacter;

	public List<SkillActivation> pendingSkillActivations;

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

	public virtual void SkillApplied(SkillDef s) {

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
	public void SkillDeactivated(SkillDef s) {
		if(s == pendingDeactivationSkill) {
			if(pendingDeactivationCharacter == activeCharacter) {
				Deactivate(pendingDeactivationCharacter, s);
			}
			pendingDeactivationSkill = null;
			pendingDeactivationCharacter = null;
		}
	}
	public virtual void DeactivateAfterSkillApplication(Character c, SkillDef skill) {
		pendingDeactivationSkill = skill;
		pendingDeactivationCharacter = c;
	}

	public virtual void Start () {
		pendingSkillActivations = new List<SkillActivation>();
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

	public virtual void ApplySkillAfterDelay(SkillDef s, List<Target> currentTs, float delay) {
		if(pendingSkillActivations == null) {
			pendingSkillActivations = new List<SkillActivation>();
		}
		pendingSkillActivations.Add(new SkillActivation(s, currentTs, delay));
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
