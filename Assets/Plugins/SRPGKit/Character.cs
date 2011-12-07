using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Character : MonoBehaviour {	
	[System.NonSerialized]
	public Map map=null;

	//I believe this is a stored property here in character, 
	//not merely a query to scheduler for whether active==this 
	//(what if you can have multiple active dudes?)
	public bool isActive=false;

	public int teamID;
	
	public Vector3 transformOffset = new Vector3(0, 5, 0);
	
	[HideInInspector]
	public Dictionary<string, Formula> stats;

	public List<string> statNames;
	public List<Formula> statValues;
	
	public string[] equipmentSlots;
	
	//skills are monobehaviors (though I wish I could make them components)
	//that are added/configured normally. skill instances can have a "path" that
	//denotes how to get to them via menus, but that's an application concern
	//a skill group is a gameobject that contains a bunch of skills with the right configurations,
	//and it can add (by duplication) or remove its component skills from a target gameobject.
	
	[HideInInspector]
	public string currentAnimation;	
	
	public MoveSkill moveSkill { get { 
		return GetComponent<MoveSkill>();
	} }

	public WaitSkill waitSkill { get { 
		return GetComponent<WaitSkill>();
	} }
	
	void Awake () {
		for(int i = 0; i < equipmentSlots.Length; i++) {
			equipmentSlots[i] = equipmentSlots[i].NormalizeName();
		}
	}
	
	public virtual void Reset() {
		equipmentSlots = new string[]{"hand", "hand", "head", "body", "accessory"};
	}
	
	//can be modulated by charm, etc
	public int EffectiveTeamID { get {
		return teamID;
	} }
	
	public Vector3 TilePosition { get {
		return map == null ? Vector3.zero : map.InverseTransformPointWorld(transform.position-transformOffset);
	} }
	
	public void Activate() {
		isActive = true;
	}
	
	public void Deactivate() {
		if(this.moveSkill.isActive) {
			this.moveSkill.Cancel(); 
		}
		isActive = false;
	}
	
	public void TriggerAnimation(string animType, bool force=false) {
		if(currentAnimation != animType || force) {
			currentAnimation = animType;
			Debug.Log("triggering "+animType);
			SendMessage("UseAnimation", animType, SendMessageOptions.DontRequireReceiver);
		}
	}
	
	public virtual Quaternion Facing { 
		get {
			return transform.rotation;
		} 
		set {
			transform.rotation = value;
			SendMessage("UseFacing", transform.rotation, SendMessageOptions.DontRequireReceiver);
		}
	}
	
	
	void Update () {
		if(map == null) {
			if(this.transform.parent != null) {
				map = this.transform.parent.GetComponent<Map>();
				if(map != null && map.scheduler != null) {
					map.scheduler.AddCharacter(this);
				}
			}
			if(map == null) { 
				Debug.Log("Characters must be children of Map objects!");
				return; 
			}
		}
		//Five things are going on here:
		//we're ACTIVATING/DEACTIVATING
		//we're CALCULATING WHERE WE CAN MOVE
		//we're DISPLAYING WHERE WE CAN MOVE
		//we're REQUESTING A MOVE DESTINATION
		//and, eventually, we'll be MOVING in an animated way
		//ideally, we will move as many of these as possible out of the Character script.
		//ACTIVATING/DEACTIVATING should live in a "Scheduler" GameObject and script (though deactivation requests may be sent by other scripts to the scheduler)
		//CALCULATING should live in a MoveStrategy script on the Character gameobject
		//DISPLAYING should live in a MoveFeedback script on the Character gameobject
		//REQUESTING should live in a MoveInput script on the Character gameobject
		//MOVING should live in a MoveExecutor script on the Character gameobject
		//continuous movement (e.g. via keyboard) could be a MoveInput with a trivial Executor (pos=dest) and a collision-detecting strategy and any type of MoveFeedback
		//  if it's timed continuous movement, the Scheduler might halt the move by its own prerogative.

		//MoveStrategy concerns the rules regarding "If I am character X in state Y at position Z, what tiles T[] can I reach?"
		//  It also validates the move and returns a success status, for use in the MoveExecutor (consider FFT's "Teleport", which may fail)
		//MoveIO concerns the rules and processes and state that display:
			//"Reachable Tiles" T[], provided by MoveStrategy
			//"Candidate Tiles" Tc[], optionally requested by MoveInput
			//"Decided Tiles" Td[], optionally requested by MoveExecutor. May or may not hide T[] and/or Tc[].
		//MoveIO also interprets player input (by mouse/keyboard/etc), optionally requests the temporary display of Tc[] during the decision-making process, and once finished requests that the MoveExecutor perform the actual move.
		//MoveExecutor optionally sets Td[] on the MoveFeedback while performing the actual move (pursuant to MoveStrategy's success flag). 
		//  MoveExecutors may be reversible.
		//  This is where the character's Unity transform is manipulated and the map is updated to reflect the character's new position.
		
		//For example, an FFT character would have a MoveStrategy that did a flood-fill on the map with Move and Jump calculations (or, if they had Fly or Teleport, other MoveStrategies), a MoveIO script using an Overlay and an update loop that's click-based, and a MoveExecutor that animates from pos to dest, taking jumps and so on into account -- all this wrapped in a CT-based scheduler.
		//Valkyria Chronicles would use a phase-based scheduler with user-decided turns based on resource expenditure (rather than just "use all units"), a movement strategy that offers a circular radius around the player, movement feedback that shows that radius (or shows, in lighter tint, the terrain reachable before the player's action points expire; with a darker tint, the unreachable terrain), MoveInput that works off of keyboard, mouse, or controller, and a MoveExecutor that merely updates the character's position and lets the map know about it.
		//This system is opt-in. Valkyria movement could work simply with a Scheduler and a custom script that moves the character according to keyboard input while it's active, updating the map (if any) as needed.
	}	
	void OnDestroy() {
		if(map != null && map.scheduler != null) {
			map.scheduler.RemoveCharacter(this);
		}
	}
	
	void MakeStatsIfNecessary() {
		if(stats == null) {
			stats = new Dictionary<string, Formula>();
			for(int i = 0; i < statNames.Count; i++) {
				stats.Add(statNames[i].NormalizeName(), statValues[i]);
			}
		}
	}
	
	public Skill[] Skills { get {
		//TODO: cache
		//first off, replace any skills that have been replaced
		Skill[] allSkills = GetComponentsInChildren<Skill>();
		return allSkills.Where(delegate(Skill x) {
			string replPath = x.skillGroup+"//"+x.skillName;
			int replPri = x.replacementPriority;
			return !allSkills.Any(y => 
				y.replacedSkill == replPath && 
				y.replacementPriority > replPri);
		}).ToArray();
	} }
	public Equipment[] Equipment { get {
		//TODO: cache
		return GetComponentsInChildren<Equipment>();
	} }
	
	public bool HasStat(string statName) {
		MakeStatsIfNecessary();
		return stats.ContainsKey(statName);
	}
	
	public float GetBaseStat(string statName) {
		MakeStatsIfNecessary();
		return stats[statName].GetCharacterValue(this);	
	}

	public void SetBaseStat(string statName, float amt) {
		MakeStatsIfNecessary();
		Formula f = stats[statName];
		if(f.formulaType == FormulaType.Constant) {
			f.constantValue = amt;
		} else {
			Debug.LogError("Can't set value of non-constant base stat "+statName);
		}
	}

	public void AdjustBaseStat(string statName, float amt) {
		MakeStatsIfNecessary();
		Formula f = stats[statName];
		if(f.formulaType == FormulaType.Constant) {
			f.constantValue += amt;
		} else {
			Debug.LogError("Can't adjust value of non-constant base stat "+statName);
		}
	}
	
	public float GetStat(string statName) {
		float stat = GetBaseStat(statName);
		foreach(Equipment e in Equipment) {
			if(e.passiveEffects.Length != 0) {
				foreach(StatEffect se in e.passiveEffects) {
					if(se.statName == statName) {
						stat = se.ModifyStat(stat, null, this, e);
					}
				}
			}
		}
		foreach(Skill s in Skills) {
			if(s.passiveEffects.Length != 0) {
				foreach(StatEffect se in s.passiveEffects) {
					if(se.statName == statName) {
						stat = se.ModifyStat(stat, s, this, null);
					}
				}
			}
		}
		return stat;
	}
	
	public bool IsEquipmentSlotFull(int equipmentSlot) {
		return Equipment.Any(e => e.equippedSlots.Contains(equipmentSlot));
	}
	
	public void EmptyEquipmentSlot(int slot) {
		if(IsEquipmentSlotFull(slot)) {
			foreach(Equipment e in Equipment.Where(eq => eq.equippedSlots.Contains(slot))) {
				e.Unequip();
			}
		}	
	}
	
	public int GetEmptyEquipmentSlot(string slotType, List<int> exceptSlots) {
		int first = -1;
		int firstEmpty = -1;
		for(int i = 0; i < equipmentSlots.Length; i++) {
			if(exceptSlots.Contains(i)) { continue; }
			if(equipmentSlots[i] == slotType) {
 				if(!IsEquipmentSlotFull(i)) {
					if(firstEmpty == -1) { firstEmpty = i; }
				}
				if(first == -1) { first = i; }
			}
		}
		if(firstEmpty != -1) {
			return firstEmpty;
		} else if(first != -1) {
			EmptyEquipmentSlot(first);
			return first;
		} else {
			return -1;
		}
	}
	public void Equip(Equipment e, int slot=-1) {
		string[] neededSlots = e.equipmentSlots;
		//free up the desired slot
		if(slot != -1) {
			EmptyEquipmentSlot(slot);
		}
		List<int> usedSlots = new List<int>();
		//free up other slots
		//FIXME: 2h weapons unequip themselves! pass usedSlots into GetEmpty...
		foreach(string s in neededSlots) {
			if(slot != -1 && equipmentSlots[slot] == s && !usedSlots.Contains(slot)) {
				usedSlots.Add(slot);
			} else {
				int foundSlot = GetEmptyEquipmentSlot(s, usedSlots);
				usedSlots.Add(foundSlot);
			}
		}
		e.EquipOn(this, usedSlots.ToArray());
	}
}
