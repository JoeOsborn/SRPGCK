using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("SRPGCK/Character/Skills/Generic")]
public class Skill : MonoBehaviour {
	[HideInInspector]
	public bool isActive=false;
	public string skillName;
	public string skillGroup = "";
	public int skillSorting = 0;

	public bool replacesSkill = false;
	public string replacedSkill = "";
	public int replacementPriority=0;

	virtual public bool isPassive { get { return true; } }

	public bool deactivatesOnApplication=true;

	public StatEffect[] passiveEffects;

	public List<Parameter> parameters;

	protected Dictionary<string, Formula> runtimeParameters;

	//only relevant to targeted skills, sadly
	[HideInInspector]
	public List<Character> targetCharacters;
	[HideInInspector]
	public Character currentTargetCharacter;
	[HideInInspector]
	public int currentHitType;

	//reaction
	public bool reactionSkill=false;
	public string[] reactionTypesApplied, reactionTypesApplier;
	public StatChange[] reactionStatChangesApplied, reactionStatChangesApplier;
	//tile validation region (line/range/cone/etc)
	public Region reactionTargetRegion, reactionEffectRegion;
	public StatEffectGroup[] reactionEffects;
	public StatEffectGroup reactionApplicationEffects;

	[HideInInspector]
	public Skill currentReactedSkill = null;
	[HideInInspector]
	public StatEffectRecord currentReactedEffect = null;

	[HideInInspector]
	public List<StatEffectRecord> lastEffects;

	public virtual void Start() {
		if(reactionTargetRegion!=null) {
			reactionTargetRegion.Owner = this;
		}
		if(reactionEffectRegion!=null) {
			reactionEffectRegion.Owner = this;
		}
	}
	public virtual void ActivateSkill() {
		if(isPassive) { return; }
		isActive = true;
		if(reactionTargetRegion != null) {
			reactionTargetRegion.Owner = this;
		}
		if(reactionEffectRegion != null) {
			reactionEffectRegion.Owner = this;
		}
	}
	public virtual void DeactivateSkill() {
		if(isPassive) { return; }
		isActive = false;
		map.BroadcastMessage("SkillDeactivated", this, SendMessageOptions.DontRequireReceiver);
		targetCharacters = null;
		currentTargetCharacter = null;
	}
	public virtual void Update() {
		if(reactionTargetRegion != null) {
			reactionTargetRegion.Owner = this;
		}
		if(reactionEffectRegion != null) {
			reactionEffectRegion.Owner = this;
		}
	}
	public virtual void Cancel() {
		if(isPassive) { return; }
		DeactivateSkill();
	}
	public virtual void ResetSkill() {

	}
	public virtual void Reset() {
		ResetSkill();
	}
	public virtual void ApplySkill() {
		map.BroadcastMessage("SkillApplied", this, SendMessageOptions.DontRequireReceiver);
		if(deactivatesOnApplication && isActive) {
			DeactivateSkill();
		}
	}

	public virtual bool ReactionTypesMatch(StatEffectRecord se) {
		string[] reactionTypes = se.effect.target == StatEffectTarget.Applied ? reactionTypesApplied : reactionTypesApplier;
		StatChange[] reactionStatChanges = se.effect.target == StatEffectTarget.Applied ? reactionStatChangesApplied : reactionStatChangesApplier;
		return se.Matches(reactionStatChanges, reactionTypes);
	}

	public virtual bool ReactsAgainst(Skill s, StatEffectRecord se) {
		return s != this && //don't react against your own application
			s.character != character && //don't react against your own character's skills
			s.currentTargetCharacter == character && //only react to skills used against our character
			reactionSkill && //only react if you're a reaction skill
			!s.reactionSkill && //don't react against reaction skills
			s is ActionSkill && //only react against targeted skills
			ReactionTypesMatch(se); //only react if masks match
	}
	protected void ClearLastEffects() {
		if(lastEffects == null) {
			lastEffects = new List<StatEffectRecord>();
		} else {
			lastEffects.Clear();
		}
	}
	public virtual PathNode[] PathNodesForTarget(Target t, Region tr, Region efr, Vector3 pos, Quaternion q) {
		if(t.subregion != -1) {
			return efr.GetValidTiles(tr.ActualTilesForTargetedTiles(tr.regions[t.subregion].GetValidTiles(pos, q)), q);
		} else if(t.path != null) {
			return efr.GetValidTiles(tr.ActualTilesForTargetedTiles(new PathNode[]{t.path}), q);
		} else if(t.character != null) {
			return efr.GetValidTiles(tr.ActualTilesForTargetedTiles(new PathNode[]{new PathNode(t.character.TilePosition, null, 0)}), q);
		} else if(t.facing != null) {
			return efr.GetValidTiles(pos, t.facing.Value);
		}
		Debug.LogError("Invalid target");
		return null;
	}


	protected virtual void SkillApplied(Skill s) {
		//react against each effect
		currentReactedSkill = s;
		List<StatEffectRecord> fx = s.lastEffects;
		bool reacts = false;
		foreach(StatEffectRecord se in fx) {
			currentReactedEffect = se;
			if(ReactsAgainst(s, se)) {
				reacts = true;
				break;
			}
		}
		if(reacts) {
			ClearLastEffects();
			currentTargetCharacter = s.character;
			Target target = (new Target()).Character(s.character);
			reactionTargetRegion.Owner = this;
			reactionEffectRegion.Owner = this;
			PathNode[] reactionTiles = PathNodesForTarget(target, reactionTargetRegion, reactionEffectRegion, character.TilePosition, Quaternion.Euler(0,character.Facing,0));
			targetCharacters = new List<Character>(){s.character};
			SetArgsFromTarget(target, null, "");
			targetCharacters = reactionEffectRegion.CharactersForTargetedTiles(reactionTiles);
			if(targetCharacters.Contains(currentTargetCharacter)) {
				ApplyPerApplicationEffectsTo(reactionApplicationEffects.effects, new List<Character>(){currentTargetCharacter});
				if(reactionEffects.Length > 0) {
					ApplyEffectsTo(target, null, reactionEffects, targetCharacters, "reaction.hitType");
				}
				map.BroadcastMessage("SkillApplied", this, SendMessageOptions.DontRequireReceiver);
			}
		}
		currentReactedSkill = null;
		currentReactedEffect = null;
	}

	void MakeParametersIfNecessary() {
		if(runtimeParameters == null) {
			runtimeParameters = new Dictionary<string, Formula>();
			if(parameters == null) { parameters = new List<Parameter>(); }
			for(int i = 0; i < parameters.Count; i++) {
				runtimeParameters.Add(parameters[i].Name.NormalizeName(), parameters[i].Formula);
			}
		}
	}

	public bool HasParam(string pname) {
		MakeParametersIfNecessary();
		return runtimeParameters.ContainsKey(pname);
	}

	public Formulae fdb { get {
		if(character != null) { return character.fdb; }
		return Formulae.DefaultFormulae;
	} }

	public float GetParam(string pname, float fallback=float.NaN) {
		MakeParametersIfNecessary();
		//TODO: let all other equipment and skills modulate this param?
		if(!HasParam(pname)) {
			if(float.IsNaN(fallback)) {
				Debug.LogError("No fallback for missing param "+pname);
			}
/*			Debug.Log("using fallback "+fallback+" for "+pname);*/
			return fallback;
		}
		return runtimeParameters[pname].GetValue(fdb, this, null);
	}

	public void SetParam(string pname, float value) {
		MakeParametersIfNecessary();
		if(!HasParam(pname)) {
			runtimeParameters[pname] = Formula.Constant(value);
		} else {
			Formula f = runtimeParameters[pname];
			if(f.formulaType == FormulaType.Constant) {
				f.constantValue = value;
			} else {
				Debug.LogError("Can't set value of non-constant param "+pname);
			}
		}
	}

	public void AddParam(string pname, Formula f) {
		MakeParametersIfNecessary();
		runtimeParameters[pname] = f;
		parameters = parameters.Concat(new Parameter[]{new Parameter(pname, f)}).ToList();
	}

	protected virtual void SetArgsFrom(Vector3 ttp, Quaternion? facing=null, string prefix="") {
		Vector3 ctp = character.TilePosition;
		float distance = Vector3.Distance(ttp, ctp);
		float angle = facing != null ? facing.Value.eulerAngles.y : character.Facing;
		if(facing == null &&
		   (!Mathf.Approximately(ttp.y,ctp.y) ||
		    !Mathf.Approximately(ttp.x,ctp.x))) {
			angle = Mathf.Atan2(ttp.y-ctp.y, ttp.x-ctp.x)*Mathf.Rad2Deg;
		}
		string infix = (prefix??"")+((prefix==""||prefix==null)?"":".");
		SetParam("arg."+infix+"distance", distance);
		SetParam("arg."+infix+"mdistance", Mathf.Abs(ttp.x-ctp.x)+Mathf.Abs(ttp.y-ctp.y)+Mathf.Abs(ttp.z-ctp.z));
		SetParam("arg."+infix+"mdistance.xy", Mathf.Abs(ttp.x-ctp.x)+Mathf.Abs(ttp.y-ctp.y));
		SetParam("arg."+infix+"dx", Mathf.Abs(ttp.x-ctp.x));
		SetParam("arg."+infix+"dy", Mathf.Abs(ttp.y-ctp.y));
		SetParam("arg."+infix+"dz", Mathf.Abs(ttp.z-ctp.z));
		SetParam("arg."+infix+"angle.xy", angle);
	}
	public virtual void SetArgsFromTarget(Target t, TargetSettings ts, string prefix) {
		TargetingMode tm = TargetingMode.Custom;
		if(ts != null) {
			tm = ts.targetingMode;
		} else {
			if(t.path != null) { tm = TargetingMode.Pick; }
			else if(t.character != null) { tm = TargetingMode.Pick; }
			else if(t.facing != null) { tm = TargetingMode.Radial; }
			else if(t.subregion != -1) { tm = TargetingMode.SelectRegion; }
		}
		Vector3 pos = character.TilePosition;
		switch(tm) {
			case TargetingMode.Self:
			case TargetingMode.Pick:
			case TargetingMode.Path:
				pos = t.Position;
				SetArgsFrom(pos, t.facing, prefix);
				break;
			case TargetingMode.Cardinal:
			case TargetingMode.Radial:
			//FIXME: wrong for selectRegion
			case TargetingMode.SelectRegion:
				if(t.character != null) {
					pos = t.character.TilePosition;
				} else if(t.path != null) {
					pos = t.Position;
				}
				SetArgsFrom(pos, t.facing, prefix);
				break;
			default:
				Debug.LogError("Unrecognized targeting mode");
				break;
		}
	}

	protected virtual void ApplyPerApplicationEffectsTo(StatEffect[] effects, List<Character> targs) {
		Character targ = (targs == null || targs.Count == 0) ? null : targs[0];
		if(targ == null) {
			foreach(StatEffect se in effects) {
				if(se.target == StatEffectTarget.Applied) {
					Debug.LogError("Applied-facing ability used without target");
					return;
				}
			}
		}
		foreach(StatEffect se in effects) {
			lastEffects.Add(se.Apply(
				this,
				character,
				targ
			));
		}
	}
	protected virtual void ApplyEffectsTo(Target t, TargetSettings ts, StatEffectGroup[] effectGroups, List<Character> targs, string htp) {
		foreach(Character c in targs) {
			currentTargetCharacter = c;
			int hitType = (int)GetParam(htp, 0);
			currentHitType = hitType;
			if(currentHitType < effectGroups.Length) {
				StatEffect[] effects = effectGroups[hitType].effects;
				SetArgsFromTarget(t, ts, "");
				foreach(StatEffect se in effects) {
					lastEffects.Add(se.Apply(this, character, currentTargetCharacter));
				}
			} else {
				Debug.LogError("Skill "+skillName+" has too few effect groups for hit type "+currentHitType);
			}
		}
	}

	public static Vector2 TransformKeyboardAxes(float h, float v, bool switchXY=true) {
		//use the camera and the map's own rotation
		Transform cam = Camera.main.transform;
		//h*right+v*forward
		Vector3 xp = cam.TransformDirection(new Vector3(1, 0, 0));
		xp.y = 0;
		xp = xp.normalized;
		Vector3 yp = new Vector3(-xp.z, 0, xp.x);
		Vector3 result = h*xp + v*yp;
		if(switchXY) {
			return new Vector2(-result.z, result.x);
		} else {
			return new Vector2(result.x, result.z);
		}
	}

	public Vector3 transformOffset { get {
		return character.transformOffset;
	} }
	Character _character;
	public Character character { get {
		if(_character == null) {
			Transform t = transform;
			while(t != null) {
				Character c = t.GetComponent<Character>();
				if(c != null) { _character = c; break; }
				t = t.parent;
			}
		}
		return _character;
	} }
	Map _map;
	public Map map { get {
		if(_map == null) { _map = character.transform.parent.GetComponent<Map>(); }
		return _map;
	} }
	public Scheduler scheduler { get { return this.map.scheduler; } }
	public Arbiter arbiter { get { return this.map.arbiter; } }
}