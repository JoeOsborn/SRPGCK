using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Skill : MonoBehaviour {
	[HideInInspector]
	public bool isActive=false;
	public string skillName;
	public string skillGroup="";
	public int skillSorting = 0;

	public bool replacesSkill=false;
	public string replacedSkill = "";
	public int replacementPriority=0;

	virtual public bool isPassive { get { return true; } }

	public bool deactivatesOnApplication=true;

	public StatEffect[] passiveEffects;

	public List<Parameter> parameters;

	protected Dictionary<string, Formula> runtimeParameters;

	//only relevant to targeted skills, sadly
	[HideInInspector]
	public List<Character> targets;
	[HideInInspector]
	public Character currentTarget;
	[HideInInspector]
	public int currentHitType;

	//reaction
	public bool reactionSkill=false;
	public string[] reactionTypesApplied, reactionTypesApplier;
	public StatChange[] reactionStatChangesApplied, reactionStatChangesApplier;
	//tile validation region (line/range/cone/etc)
	public Region reactionTargetRegion, reactionEffectRegion;
	public StatEffectGroup[] reactionEffects;

	[HideInInspector]
	public Skill currentReactedSkill = null;
	[HideInInspector]
	public StatEffectRecord currentReactedEffect = null;

	[HideInInspector]
	public List<StatEffectRecord> lastEffects;

	public virtual void Start() {
		if(reactionTargetRegion!=null) {
			reactionTargetRegion.owner = this;
		}
		if(reactionEffectRegion!=null) {
			reactionEffectRegion.owner = this;
		}
	}
	public virtual void ActivateSkill() {
		if(isPassive) { return; }
		isActive = true;
		if(reactionTargetRegion != null) {
			reactionTargetRegion.owner = this;
		}
		if(reactionEffectRegion != null) {
			reactionEffectRegion.owner = this;
		}
	}
	public virtual void DeactivateSkill() {
		if(isPassive) { return; }
		isActive = false;
		map.BroadcastMessage("SkillDeactivated", this, SendMessageOptions.DontRequireReceiver);
		targets = null;
		currentTarget = null;
	}
	public virtual void Update() {
		if(reactionTargetRegion != null) {
			reactionTargetRegion.owner = this;
		}
		if(reactionEffectRegion != null) {
			reactionEffectRegion.owner = this;
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
		if(deactivatesOnApplication) {
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
			s.currentTarget == character && //only react to skills used against our character
			reactionSkill && //only react if you're a reaction skill
			!s.reactionSkill && //don't react against reaction skills
			s is ActionSkill && //only react against targeted skills
			ReactionTypesMatch(se); //only react if masks match
	}
	protected virtual void SkillApplied(Skill s) {
		//react against each effect
		currentReactedSkill = s;
		List<StatEffectRecord> fx = s.lastEffects;

		foreach(StatEffectRecord se in fx) {
			currentReactedEffect = se;
			//TODO: should reactions get rolled up somehow so the skill doesn't get applied multiple times?
			if(ReactsAgainst(s, se)) {
				currentTarget = s.character;
				int hitType = (int)GetParam("reaction.hitType", 0);
				currentHitType = hitType;
				if(reactionEffects[hitType].Length > 0) {
					reactionTargetRegion.owner = this;
					reactionEffectRegion.owner = this;
					PathNode[] reactionTiles = reactionTargetRegion.GetValidTiles();
					reactionTiles = reactionTargetRegion.ActualTilesForTargetedTiles(reactionTiles);
					List<Character> tentativeTargets = reactionTargetRegion.CharactersForTargetedTiles(reactionTiles);
					if(tentativeTargets.Contains(s.character)) {
						reactionTiles = reactionEffectRegion.GetValidTiles(currentTarget.TilePosition);
						targets = reactionEffectRegion.CharactersForTargetedTiles(reactionTiles);
						ApplyEffectsTo(reactionEffects[hitType].effects, targets);
					}
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
			for(int i = 0; i < parameters.Count; i++) {
				runtimeParameters.Add(parameters[i].Name.NormalizeName(), parameters[i].Formula);
			}
		}
	}

	public bool HasParam(string pname) {
		MakeParametersIfNecessary();
		return runtimeParameters.ContainsKey(pname);
	}

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
		return runtimeParameters[pname].GetValue(this, null);
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

	protected virtual void ApplyEffectsTo(StatEffect[] effects, List<Character> targs) {
		if(lastEffects == null) {
			lastEffects = new List<StatEffectRecord>();
		} else {
			lastEffects.Clear();
		}
		foreach(Character c in targs) {
			currentTarget = c;
			Vector3 ttp = c.TilePosition;
			Vector3 ctp = character.TilePosition;
			float distance = Vector3.Distance(ttp, ctp);
			float angle = currentTarget == character ?
				character.Facing :
				Mathf.Atan2(ttp.y-ctp.y, ttp.x-ctp.x);
			SetParam("arg.distance", distance);
			SetParam("arg.mdistance", Mathf.Abs(ttp.x-ctp.x)+Mathf.Abs(ttp.y-ctp.y)+Mathf.Abs(ttp.z-ctp.z));
			SetParam("arg.mdistance.xy", Mathf.Abs(ttp.x-ctp.x)+Mathf.Abs(ttp.y-ctp.y));
			SetParam("arg.dx", Mathf.Abs(ttp.x-ctp.x));
			SetParam("arg.dy", Mathf.Abs(ttp.y-ctp.y));
			SetParam("arg.dz", Mathf.Abs(ttp.z-ctp.z));
			SetParam("arg.angle.xy", angle);
			foreach(StatEffect se in effects) {
				lastEffects.Add(se.Apply(this, character, currentTarget));
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
	public Character character { get {
		//TODO: cache
		Character c = GetComponent<Character>();
		if(c != null) { return c; }
		if(transform.parent != null) {
			return transform.parent.GetComponent<Character>();
		}
		return null;
	} }
	public Map map { get { return character.transform.parent.GetComponent<Map>(); } }
	public Scheduler scheduler { get { return this.map.scheduler; } }
	public Arbiter arbiter { get { return this.map.arbiter; } }
}