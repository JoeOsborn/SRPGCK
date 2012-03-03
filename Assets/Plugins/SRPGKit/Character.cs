using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Character : MonoBehaviour {
	Map _map;
	public Map map { get {
		if(_map == null) {
			_map = transform.parent == null ? null : transform.parent.GetComponent<Map>();
		}
		return _map;
	} }

	MoveExecutor _specialMoveExecutor;
	protected MoveExecutor specialMoveExecutor { get {
		if(_specialMoveExecutor == null) {
			_specialMoveExecutor = new MoveExecutor();
			_specialMoveExecutor.lockToGrid = true;
			_specialMoveExecutor.character = this;
			_specialMoveExecutor.map = map;
		}
		return _specialMoveExecutor;
	} }

	public string specialMoveType=null;

	//I believe this is a stored property here in character,
	//not merely a query to scheduler for whether active==this
	//(what if you can have multiple active dudes?)
	[HideInInspector]
	public bool isActive=false;

	public string characterName;
	public int teamID;
	public Vector3 transformOffset = new Vector3(0, 5, 0);

	public List<Parameter> stats;

	public string[] equipmentSlots;

	[HideInInspector]
	public Dictionary<string, Formula> runtimeStats;

	[HideInInspector]
	public Formulae fdb { get {
		return (map != null && map.arbiter != null && map.arbiter.formulae != null) ? map.arbiter.formulae : Formulae.DefaultFormulae;
	} }

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

	public override string ToString() {
		return characterName;
	}

	void Awake () {
		for(int i = 0; i < equipmentSlots.Length; i++) {
			equipmentSlots[i] = equipmentSlots[i].NormalizeName();
		}
	}

	public virtual void Reset() {
		characterName = name;
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
		if(this.moveSkill != null && this.moveSkill.isActive) {
			this.moveSkill.Cancel();
		}
		if(this.waitSkill != null && this.waitSkill.isActive) {
			//just let waitSkill get to its natural conclusion
			//this.waitSkill.Cancel();
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

	public void SpecialMove(int amount, float direction, string moveType, float specialMoveSpeedXY, float specialMoveSpeedZ, bool canCrossWalls, bool canCrossCharacters, bool canGlide, float zUpMax, float zDownMax, FacingLock lockType, Skill cause) {
		//for now, assume grid lock
		//build a path to the thing we should hit (ledge or character) or until we run out of stuff
		specialMoveType = moveType;
		specialMoveExecutor.XYSpeed = specialMoveSpeedXY;
		specialMoveExecutor.ZSpeedDown = specialMoveSpeedZ;
		Vector3 start = TilePosition;
		Vector3 here = start;
		PathNode cur = new PathNode(here, null, 0);
		Vector2 offset = Vector2.zero;
		LockedFacing f = SRPGUtil.LockFacing(direction, lockType);
		switch(f) {
			case LockedFacing.XP  : offset = new Vector2( 1, 0); break;
			case LockedFacing.YP  : offset = new Vector2( 0, 1); break;
			case LockedFacing.XN  : offset = new Vector2(-1, 0); break;
			case LockedFacing.YN  : offset = new Vector2( 0,-1); break;
			case LockedFacing.XPYP: offset = new Vector2( 1, 1); break;
			case LockedFacing.XPYN: offset = new Vector2( 1,-1); break;
			case LockedFacing.XNYP: offset = new Vector2(-1, 1); break;
			case LockedFacing.XNYN: offset = new Vector2(-1,-1); break;
			default:
				float fRad = ((float)f)*Mathf.Deg2Rad;
				offset = new Vector2(Mathf.Cos(fRad), Mathf.Sin(fRad));
				break;
		}
		float maxDrop = here.z;
		int soFar = 0;
		float dropDistance = 0;
		List<Character> collidedCharacters = new List<Character>();
		while(soFar < amount) {
			Vector3 lastHere = here;
			here.x += offset.x;
			here.y += offset.y;
			MapTile hereT = map.TileAt(here);
			Debug.Log("knock into "+here+"?");
			if(hereT == null) {
				//try to fall
				Debug.Log("no tile!");
				int lower = map.PrevZLevel((int)here.x, (int)here.y, (int)here.z);
				if(here.x < 0 || here.y < 0 || here.x >= map.size.x || here.y >= map.size.y) {
					Debug.Log("Edge of map!");
					here = lastHere;
					break;
				} else if(canGlide && (soFar+1) < amount) {
					//FIXME: it's the client's responsibility to ensure that gliders
					//don't end up falling into a bottomless pit or onto a character
					cur = new PathNode(here, cur, cur.distance+1-0.01f*maxDrop);
					Debug.Log("glide over "+here+"!");
				} else if(lower == -1) {
					//bottomless pit, treat it like a wall
					Debug.Log("bottomless pit!");
					here = lastHere;
					break;
				} else {
					//drop down
					SpecialMoveFall(ref here, zDownMax, maxDrop, ref cur, collidedCharacters, ref dropDistance);
				}
			} else {
				//is it a wall? if so, break
				//FIXME: will not work properly with ramps
				int nextZ = map.NextZLevel((int)here.x, (int)here.y, (int)here.z);
				MapTile t = map.TileAt((int)here.x, (int)here.y, nextZ);
				if(t != null && t.z > here.z+1) {
					//this tile is above us, sure, but it's way above
					nextZ = (int)here.z;
					t = map.TileAt(here);
				} else {
					here.z = nextZ;
				}
				Character hereChar = map.CharacterAt(here);
				if(hereChar != null && hereChar != this &&
				   !collidedCharacters.Contains(hereChar)) {
					collidedCharacters.Add(hereChar);
				}
				Debug.Log("tile z "+nextZ);
				if(!canCrossWalls && nextZ > zUpMax) {
					//FIXME: it's the client's responsibility to ensure that wall-crossers
					//don't end up stuck in a wall
					//it's a wall, break
					Debug.Log("wall!");
					here = lastHere;
					break;
				} else if(!canCrossCharacters && collidedCharacters.Count > 0) {
					//break
					//FIXME: it's the client's responsibility to ensure that character-crossers
					//don't end up stuck in a character
					Debug.Log("character "+collidedCharacters[0].name);
					here = lastHere;
					break;
				} else {
					//keep going
					Debug.Log("keep going");
					cur = new PathNode(here, cur, cur.distance+1-0.01f*maxDrop);
				}
			}
			Debug.Log("tick forward once");
			soFar++;
		}
		if(canGlide && map.TileAt(here) == null) {
			//fall
			Debug.Log("it's all over, end glide!");
			SpecialMoveFall(ref here, zDownMax, maxDrop, ref cur, collidedCharacters, ref dropDistance);
		}
		//move executor special move (path)
		specialMoveExecutor.Activate();
		CharacterSpecialMoveReport rep = new CharacterSpecialMoveReport(this, moveType, cause, start, amount, direction, canCrossWalls, canCrossCharacters, canGlide, zUpMax, zDownMax, cur, amount-soFar, dropDistance, collidedCharacters);
		map.BroadcastMessage("WillSpecialMoveCharacter", rep, SendMessageOptions.DontRequireReceiver);
		specialMoveExecutor.SpecialMoveTo(cur, (src, endNode, finishedNicely) => {
			//broadcast message with remaining velocity, incurred fall distance, collided character if any
			Debug.Log("specially moved character "+this.name+" by "+moveType+" into "+(collidedCharacters.Count==0?"nobody":""+collidedCharacters.Count+" folks")+" left over "+(amount-soFar)+" dropped "+dropDistance);
			// if(moveType == "knockback") {
			// 	this.Facing = 180+this.Facing;
			// }
			map.BroadcastMessage("DidSpecialMoveCharacter", rep, SendMessageOptions.DontRequireReceiver);
			specialMoveExecutor.Deactivate();
		});
	}
	protected bool SpecialMoveFall(ref Vector3 here, float zDownMax, float maxDrop, ref PathNode cur, List<Character> collidedCharacters, ref float dropDistance) {
		//FIXME: will not work properly with ramps
		int lower = map.PrevZLevel((int)here.x, (int)here.y, (int)here.z);
		if(lower != -1) {
			if((cur.pos.z - lower) > zDownMax) {
				dropDistance += here.z - lower;
			}
			Vector3 oldHere = here;
			here.z = lower;
			cur = new PathNode(here, cur, cur.distance+1-0.01f*(maxDrop-(oldHere.z-lower)));
			Character c = map.CharacterAt(here);
			if(c != null) {
				collidedCharacters.Add(c);
			}
			Debug.Log("fall down to "+here+"!");
			return true;
		}
		Debug.Log("trying to fall, but it's a bottomless pit!");
		return false;
	}

	public virtual float Facing {
		get {
			//subtract from 360 to make it counter-clockwise starting at x+
			return 360-transform.rotation.eulerAngles.y;
		}
		set {
			//subtract from 360 to make it clockwise starting at x+
			transform.rotation = Quaternion.Euler(0, 360-value, 0);
			SetBaseStat("facing", value);
			SendMessage("UseFacing", value, SendMessageOptions.DontRequireReceiver);
		}
	}

	void FixedUpdate () {
		if(map == null) {
			Debug.Log("Characters must be children of Map objects!");
			return;
		}
		if(!map.scheduler.ContainsCharacter(this)) {
			map.scheduler.AddCharacter(this);
		}
	}
	public void Update() {
		if(specialMoveExecutor.isActive) {
			specialMoveExecutor.Update();
		}
	}
	void OnDestroy() {
		if(map != null && map.scheduler != null) {
			map.scheduler.RemoveCharacter(this);
		}
	}

	void MakeStatsIfNecessary() {
		if(runtimeStats == null) {
			runtimeStats = new Dictionary<string, Formula>();
			for(int i = 0; i < stats.Count; i++) {
				runtimeStats.Add(stats[i].Name, stats[i].Formula);
			}
		}
	}
	IEnumerable<Skill> skills;
	IEnumerable<Equipment> equipment;
	IEnumerable<StatusEffect> statusEffects;
	public void ApplyStatusEffect(StatusEffect se) {
		se.transform.parent = transform; //??
		InvalidateStatusEffects();
	}
	public void InvalidateSkills() {
		skills = null;
	}
	public void InvalidateEquipment() {
		equipment = null;
		InvalidateStatusEffects();
	}
	public void InvalidateStatusEffects() {
		statusEffects = null;
	}
	public IEnumerable<Skill> Skills { get {
		if(skills == null) {
			//replace any skills that need replacing
			Skill[] allSkills = GetComponentsInChildren<Skill>();
			skills = allSkills.Where(delegate(Skill x) {
				string replPath = x.skillGroup+"//"+x.skillName;
				int replPri = x.replacementPriority;
				return !allSkills.Any(y =>
					y != x &&
					y.replacesSkill &&
					y.replacedSkill == replPath &&
					y.replacementPriority > replPri);
			}).ToArray().AsEnumerable();
		}
		return skills;
	} }
	public IEnumerable<Equipment> Equipment { get {
		if(equipment == null) {
			equipment = GetComponentsInChildren<Equipment>().AsEnumerable();
		}
		return equipment;
	} }
	public IEnumerable<StatusEffect> StatusEffects { get {
		if(statusEffects == null) {
			StatusEffect[] allEffects = GetComponentsInChildren<StatusEffect>();
			statusEffects = allEffects.Where(delegate(StatusEffect x) {
				string replPath = x.effectType;
				int replPri = x.priority;
				return !allEffects.Any(y =>
					y != x &&
					y.effectType == replPath &&
					y.replaces &&
					y.priority > replPri);
			}).ToArray().AsEnumerable();
		}
		return statusEffects;
	} }

	public bool HasStat(string statName) {
		MakeStatsIfNecessary();
		return runtimeStats.ContainsKey(statName);
	}

	public bool HasStatusEffect(string statName) {
		return StatusEffects.Any(se => se.effectType == statName);
	}

	public float GetBaseStat(string statName, float fallback=-1) {
		MakeStatsIfNecessary();
		if(!HasStat(statName)) {
			if(fallback == -1) {
				Debug.LogError("No fallback for missing stat "+statName);
			}
			return fallback;
		}
		return runtimeStats[statName].GetCharacterValue(fdb, this);
	}

	public void SetBaseStat(string statName, float amt) {
		MakeStatsIfNecessary();
		if(!HasStat(statName)) {
			runtimeStats[statName] = Formula.Constant(amt);
		} else {
			Formula f = runtimeStats[statName];
			if(f.formulaType == FormulaType.Constant) {
				f.constantValue = amt;
			} else {
				Debug.LogError("Can't set value of non-constant base stat "+statName);
			}
		}
	}

	public void AdjustBaseStat(string statName, float amt) {
		MakeStatsIfNecessary();
		Formula f = runtimeStats[statName];
		if(f.formulaType == FormulaType.Constant) {
			f.constantValue += amt;
		} else {
			Debug.LogError("Can't adjust value of non-constant base stat "+statName);
		}
	}

	public float GetStat(string statName, float fallback=-1) {
		float stat = GetBaseStat(statName, fallback);
/*		Debug.Log("base "+statName+":"+stat);*/
		foreach(Equipment e in Equipment) {
			foreach(StatEffect se in e.passiveEffects) {
				if(se.statName == statName) {
					stat = se.ModifyStat(stat, null, null, e);
/*					Debug.Log("equip modify to "+stat);*/
				}
			}
		}
		foreach(Skill s in Skills) {
			foreach(StatEffect se in s.passiveEffects) {
				if(se.statName == statName) {
					stat = se.ModifyStat(stat, s, null, null);
/*					Debug.Log("skill modify to "+stat);*/
				}
			}
		}
		foreach(StatusEffect s in StatusEffects) {
			foreach(StatEffect se in s.passiveEffects) {
				if(se.statName == statName) {
					//todo: pass status effect as context
					stat = se.ModifyStat(stat, null, this, null);
/*					Debug.Log("status modify to "+stat);*/
				}
			}
		}
//		Debug.Log("final "+statName+":"+stat);
		return stat;
	}

	public bool IsEquipmentSlotFull(int equipmentSlot) {
		return Equipment.Any(e => e.equippedSlots.Contains(equipmentSlot));
	}

	public void EmptyEquipmentSlot(int slot) {
		if(IsEquipmentSlotFull(slot)) {
			foreach(Equipment e in Equipment.Where(eq => eq.equippedSlots.Contains(slot))) {
				e.Unequip();
				InvalidateSkills();
				InvalidateEquipment();
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
		foreach(string s in neededSlots) {
			if(slot != -1 && equipmentSlots[slot] == s && !usedSlots.Contains(slot)) {
				usedSlots.Add(slot);
			} else {
				int foundSlot = GetEmptyEquipmentSlot(s, usedSlots);
				usedSlots.Add(foundSlot);
			}
		}
		e.EquipOn(this, usedSlots.ToArray());
		InvalidateSkills();
		InvalidateEquipment();
	}
}
