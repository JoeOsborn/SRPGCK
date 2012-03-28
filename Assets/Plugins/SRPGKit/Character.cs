using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("SRPGCK/Character/Character")]
public class Character : MonoBehaviour {
	//FIXME: deprecated, remove later
	public int teamID;

	Map _map;
	public Map map { get {
		if(_map == null) {
			_map = transform.parent == null ?
				null :
				transform.parent.GetComponent<Map>();
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

	//can't use transform hierarchy here because
	//that's already loaded with semantics
	//for skills, equipment, etc.
	public Formula isMountableF, canMountF;
	public Character mountingCharacter, mountedCharacter;
	public bool IsMounted { get {
		return mountingCharacter != null;
	} }
	public bool IsMounting { get {
		return mountedCharacter != null;
	} }
	public bool IsMountableBy(Character c) {
		return !IsMounted && isMountableF != null ?
			(isMountableF.GetValue(fdb, null, this, c, null) != 0) :
			false;
	}
	public bool CanMount(Character c) {
		return !IsMounting && canMountF != null ?
			(canMountF.GetValue(fdb, null, this, c, null) != 0) :
			false;
	}

	public bool IsTargetable { get {
		return (GetStat("isTargetable", 1) != 0) ||
			(mountedCharacter != null && mountedCharacter.GetStat("isTargetable", 1) != 0) ||
			(mountingCharacter != null && mountingCharacter.GetStat("isTargetable", 1) != 0);
	} }


	public void Mount(Character mount) {
		mountedCharacter = mount;
		mountedCharacter.Mounted(this);
	}
	protected void Mounted(Character c) {
		mountingCharacter = c;
	}

	public void Dismount() {
		mountedCharacter.Dismounted();
		mountedCharacter = null;
	}
	protected void Dismounted() {
		mountingCharacter = null;
	}

	public string specialMoveType=null;

	//I believe this is a stored property here in character,
	//not merely a query to scheduler for whether active==this
	//(what if you can have multiple active dudes?)
	public bool isActive=false;

	public string characterName;
	public Vector3 transformOffset = new Vector3(0, 5, 0);

	public List<Parameter> stats;

	public string[] equipmentSlots;

	public Dictionary<string, Parameter> runtimeStats;

	public Formulae fdb { get {
		return
			(map != null && map.arbiter != null && map.arbiter.formulae != null) ?
				map.arbiter.formulae :
				Formulae.DefaultFormulae;
	} }

	//skills are monobehaviors (though I wish I could make them components)
	//that are added/configured normally. skill instances can have a "path" that
	//denotes how to get to them via menus, but that's an application concern
	//a skill group is a gameobject that contains a bunch of skills with the right configurations,
	//and it can add (by duplication) or remove its component skills from a target gameobject.

	public string currentAnimation;

	public MoveSkillDef moveSkill { get {
		return skills.
			Where(s => s is MoveSkillDef).
			OrderBy(s => s.isActive ? 0 : 1).
			ThenBy(s => (s as MoveSkillDef).Executor.IsMoving ? 0 : 1).
			FirstOrDefault() as MoveSkillDef;
	} }

	public WaitSkillDef waitSkill { get {
		return skills.
			Where(s => s is WaitSkillDef).
			OrderBy(s => s.isActive ? 0 : 1).
			FirstOrDefault() as WaitSkillDef;
	} }

	public override string ToString() {
		return characterName;
	}

	void Awake () {
		for(int i = 0; i < equipmentSlots.Length; i++) {
			equipmentSlots[i] = equipmentSlots[i].NormalizeName();
		}
		Facing = transform.localRotation.eulerAngles.y;
	}

	public virtual void Reset() {
		characterName = name;
		equipmentSlots = new string[]{"hand", "hand", "head", "body", "accessory"};
	}

	//can be modulated by charm, etc
	public int EffectiveTeamID { get {
		return (int)GetStat("team", teamID);
	} }
	public int TeamID { get {
		return (int)GetBaseStat("team", teamID);
	} }

	public Vector3 TilePosition { get {
		return map == null ?
			Vector3.zero :
			map.InverseTransformPointWorld(transform.position-transformOffset);
	} }
	public Vector3 WorldPosition {
		get {
			return transform.position;
		}
		set {
			transform.position = value;
			//FIXME: position mods won't propagate past one level
			//of mounting, but more than one level is kind of nuts anyway
			if(IsMounted) {
				mountingCharacter.transform.position = value;
			}
			if(IsMounting) {
				mountedCharacter.transform.position = value;
			}
		}
	}

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
			SendMessage(
				"UseAnimation",
				animType,
				SendMessageOptions.DontRequireReceiver
			);
		}
	}

	public void SpecialMove(
		Vector3 start,
		bool animateMoveToStart,
		string moveType,
		Region lineMove,
		float specialMoveSpeedXY,
		float specialMoveSpeedZ,
		SkillDef cause
	) {
		specialMoveType = moveType;
		specialMoveExecutor.XYSpeed = specialMoveSpeedXY;
		specialMoveExecutor.ZSpeedDown = specialMoveSpeedZ;
		List<Character> collidedCharacters = new List<Character>();
		float direction=-1, amount=-1, remaining=-1, dropDistance=-1;
		PathNode movePath = lineMove.GetLineMove(
			out direction,
			out amount,
			out remaining,
			out dropDistance,
			collidedCharacters,
			this,
			start
		);
		//move executor special move (path)
		specialMoveExecutor.Activate();
		CharacterSpecialMoveReport rep = new CharacterSpecialMoveReport(
			this,
			moveType,
			lineMove,
			cause,
			start,
			movePath,
			direction,
			amount,
			remaining,
			dropDistance,
			collidedCharacters
		);
		map.BroadcastMessage(
			"WillSpecialMoveCharacter",
			rep,
			SendMessageOptions.DontRequireReceiver
		);
		MoveExecutor.MoveFinished movedToStart =
		(srcStart, srcEndNode, srcFinishedNicely) => {
			Debug.Log("src finished nicely? "+srcFinishedNicely);
			specialMoveExecutor.SpecialMoveTo(
				movePath,
				(src, endNode, finishedNicely) => {
					Debug.Log(
						"specially moved character "+this.name+
						" by "+moveType+
						" from "+start+
						" in dir "+direction+
						" node "+movePath+
						" into "+(
							collidedCharacters.Count==0 ?
								"nobody":
								""+collidedCharacters.Count+" folks"
						)+
						" left over "+remaining+
						" dropped "+dropDistance
					);
					map.BroadcastMessage(
						"DidSpecialMoveCharacter",
						rep,
						SendMessageOptions.DontRequireReceiver
					);
					specialMoveExecutor.Deactivate();
					var allChars = map.CharactersAt(endNode.pos);
					var otherChars = allChars.Where(c => c != this && c != mountedCharacter).ToArray();
					Character[] chars = null;
					if(otherChars.Length > 0) {
						//try to fix myself
						chars = new Character[]{IsMounting ? mountedCharacter : this};
					}
					if(chars != null && chars.Length > 0) {
					//if any collisions left over between me and somebody else...
						Debug.Log("fix collisions");
						bool success = false;
						if(!success) {
							Debug.Log("try straight");
							success = TrySpecialMoveResponse(direction, endNode, chars, lineMove, cause);
						}
						if(!success) {
							Debug.Log("try left");
							success = TrySpecialMoveResponse(SRPGUtil.WrapAngle(direction+90), endNode, chars, lineMove, cause);
						}
						if(!success) {
							Debug.Log("try right");
							success = TrySpecialMoveResponse(SRPGUtil.WrapAngle(direction-90), endNode, chars, lineMove, cause);
						}
						if(!success) {
							Debug.Log("try back");
							success = TrySpecialMoveResponse(SRPGUtil.WrapAngle(direction+180), endNode, chars, lineMove, cause);
						}
						if(!success) {
							Debug.LogError("Can't shove "+chars.Length+" chars out of the way!");
						}
					}
				}
			);
		};

		if(start != TilePosition) {
			if(animateMoveToStart) {
				specialMoveExecutor.SpecialMoveTo(
					new PathNode(start, null, 0),
					movedToStart,
					3.0f
				);
			} else {
				specialMoveExecutor.ImmediatelyMoveTo(
					new PathNode(start, null, 0),
					movedToStart,
					3.0f
				);
			}
		} else {
			movedToStart(start, new PathNode(start, null, 0), true);
		}
	}

	protected bool TrySpecialMoveResponse(float direction, PathNode endNode, Character[] chars, Region lineMove, SkillDef cause) {
		bool success=false;
		PathNode pn = endNode;
		var shoveChars = new List<Character>();
		shoveChars.AddRange(chars);
		int tries = 0;
		const int tryLimit = 20;
		do {
			pn = lineMove.GetNextLineMovePosition(this, pn, direction, 1, 1, 0);
			//hack: slide on through untargetable characters
			if(pn != null) {
				var hereChars = map.CharactersAt(pn.pos);
				var nextChars = hereChars.Where(c => !c.IsMounting && c.IsTargetable).ToArray();
				//don't push people onto a tile with untargetable characters
				if(nextChars.Length == 0 && hereChars.Where(c => !c.IsTargetable).Count() == 0) {
					success = true;
				} else {
					shoveChars.AddRange(nextChars);
				}
				// Debug.Log("ncs "+nextChars.Length);
			}
			// Debug.Log("pn "+pn);
			tries++;
		} while(pn != null && !success && tries < tryLimit);
		if(tries >= tryLimit) { Debug.LogError("too many tries"); return false; }
		if(success) {
			Region responseMove = new Region();
			responseMove.type = RegionType.LineMove;
			responseMove.interveningSpaceType = InterveningSpaceType.LineMove;
			responseMove.radiusMaxF = Formula.Constant(1);
			responseMove.xyDirectionF = Formula.Constant(direction);
			responseMove.canCrossWalls = false;
			responseMove.canCrossEnemies = false;
			responseMove.canHaltAtEnemies = false;
			responseMove.Owner = cause;
			responseMove.facingLock = FacingLock.Cardinal;
			for(int i = shoveChars.Count-1; i >= 0; i--) {
				Character sc = shoveChars[i];
				sc.SpecialMove(
					sc.TilePosition,
					false,
					"collision_response",
					responseMove,
					25,
					25,
					cause
				);
			}
		}
		return success;
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
			runtimeStats = new Dictionary<string, Parameter>();
			for(int i = 0; i < stats.Count; i++) {
				runtimeStats.Add(stats[i].Name, stats[i]);
			}
		}
	}
	IEnumerable<SkillDef> skills;
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
	public IEnumerable<SkillDef> Skills { get {
		if(skills == null) {
			//replace any skills that need replacing
			SkillDef[] allSkills = GetComponentsInChildren<Skill>().
				Select(s => s.def).
				ToArray();
			skills = allSkills.
				Where(delegate(SkillDef x) {
					string replPath = x.skillGroup+"//"+x.skillName;
					int replPri = x.replacementPriority;
					return !allSkills.Any(y =>
						y != x &&
						y.replacesSkill &&
						y.replacedSkill == replPath &&
						y.replacementPriority > replPri);
				}).
				ToArray().AsEnumerable();
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

	protected Parameter GetStatParam(string statName) {
		MakeStatsIfNecessary();
		Parameter p = null;
		runtimeStats.TryGetValue(statName, out p);
		return p;
	}

	public float GetBaseStat(string statName, float fallback=-1) {
		MakeStatsIfNecessary();
		if(statName == "position.x") {
			return TilePosition.x;
		} else if(statName == "position.y") {
			return TilePosition.y;
		} else if(statName == "position.z") {
			return TilePosition.z;
		} else if(statName == "isMounted") {
			return IsMounted ? 1 : 0;
		} else if(statName == "isMounting") {
			return IsMounting ? 1 : 0;
		}
		if(!HasStat(statName)) {
			if(fallback == -1) {
				Debug.LogError("No fallback for missing stat "+statName);
			}
			return fallback;
		}
		return runtimeStats[statName].GetCharacterValue(fdb, this);
	}

	public Formula EditorGetBaseStat(string statName) {
		string nStatName = statName.NormalizeName();
		Parameter p = stats.FirstOrDefault(pr => pr.Name == nStatName);
		if(p == null) {
			return null;
		} else {
			return p.Formula;
		}
	}

	public void EditorSetBaseStat(string statName, Formula f) {
		string nStatName = statName.NormalizeName();
		Parameter p = stats.FirstOrDefault(pr => pr.Name == nStatName);
		if(p == null) {
			stats.Add(new Parameter(statName.NormalizeName(), f));
		} else {
			p.Formula = f;
		}
	}

	public float SetBaseStat(string statName, float amt, bool constrain=true) {
		MakeStatsIfNecessary();
		if(statName == "position.x") {
			Debug.LogError("Setting position.x from stat effect not currently supported");
			return TilePosition.x;
		} else if(statName == "position.y") {
			Debug.LogError("Setting position.y from stat effect not currently supported");
			return TilePosition.y;
		} else if(statName == "position.z") {
			Debug.LogError("Setting position.z from stat effect not currently supported");
			return TilePosition.z;
		} else if(statName == "isMounted") {
			Debug.LogError("Setting isMounted from stat effect not currently supported");
			return IsMounted ? 1 : 0;
		} else if(statName == "isMounting") {
			Debug.LogError("Setting isMounting from stat effect not currently supported");
			return IsMounting ? 1 : 0;
		}
		if(!HasStat(statName)) {
			runtimeStats[statName] = new Parameter(statName, Formula.Constant(amt));
			return amt;
		} else {
			Parameter p = runtimeStats[statName];
			return p.SetCharacterValue(fdb, this, amt, constrain);
		}
	}

	public float AdjustBaseStat(string statName, float amt) {
		return SetBaseStat(statName, GetBaseStat(statName, 0)+amt);
	}

	public float GetStat(string statName, float fallback=-1) {
		float stat = GetBaseStat(statName, fallback);
		Parameter p = GetStatParam(statName);
		foreach(Equipment e in Equipment) {
			foreach(StatEffect se in e.passiveEffects) {
				if(se.statName == statName) {
					float lastStat = stat;
					stat = se.ModifyStat(stat, null, null, null, e);
					if(p != null && se.respectLimits) {
						stat = p.ConstrainCharacterValue(fdb, this, stat, lastStat);
					}
/*					Debug.Log("equip modify to "+stat);*/
				}
			}
		}
		foreach(SkillDef s in Skills) {
			foreach(StatEffect se in s.passiveEffects) {
				if(se.statName == statName) {
					float lastStat = stat;
					stat = se.ModifyStat(stat, s, null, null, null);
					if(p != null && se.respectLimits) {
						stat = p.ConstrainCharacterValue(fdb, this, stat, lastStat);
					}
/*					Debug.Log("skill modify to "+stat);*/
				}
			}
		}
		foreach(StatusEffect s in StatusEffects) {
			foreach(StatEffect se in s.passiveEffects) {
				if(se.statName == statName) {
					float lastStat = stat;
					//todo: pass status effect as context
					stat = se.ModifyStat(stat, null, this, null, null);
					if(p != null && se.respectLimits) {
						stat = p.ConstrainCharacterValue(fdb, this, stat, lastStat);
					}
/*					Debug.Log("status modify to "+stat);*/
				}
			}
		}
		return stat;
//		Debug.Log("final "+statName+":"+stat);
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
