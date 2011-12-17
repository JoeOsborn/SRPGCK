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
	//tile validation strategy (line/range/cone/etc)
	public ActionStrategy reactionStrategy;
	public StatEffectGroup[] reactionEffects;

	[HideInInspector]
	public Skill currentReactedSkill = null;
	[HideInInspector]
	public StatEffectRecord currentReactedEffect = null;

	[HideInInspector]
	public List<StatEffectRecord> lastEffects;

	public virtual void Start() {
		reactionStrategy.owner = this;	
	}
	public virtual void ActivateSkill() {
		if(isPassive) { return; }
		isActive = true;
		reactionStrategy.owner = this;	
	}
	public virtual void DeactivateSkill() {
		if(isPassive) { return; }
		isActive = false;
		map.BroadcastMessage("SkillDeactivated", this, SendMessageOptions.DontRequireReceiver);
		targets = null;
		currentTarget = null;
	}
	public virtual void Update() {
		reactionStrategy.owner = this;	
	}
	public virtual void Cancel() {
		if(isPassive) { return; }
		DeactivateSkill();
	}	
	public virtual void Reset() {

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
					reactionStrategy.owner = this;
					reactionStrategy.zRangeUpMin = GetParam("reaction.range.z.up.min", 0);
					reactionStrategy.zRangeUpMax = GetParam("reaction.range.z.up.max", 1);
					reactionStrategy.zRangeDownMin = GetParam("reaction.range.z.down.min", 0);
					reactionStrategy.zRangeDownMax = GetParam("reaction.range.z.down.max", 2);
					reactionStrategy.xyRangeMin = GetParam("reaction.range.xy.min", 1);
					reactionStrategy.xyRangeMax = GetParam("reaction.range.xy.max", 1);

					reactionStrategy.zRadiusUp = GetParam("reaction.radius.z.up", 0);
					reactionStrategy.zRadiusDown = GetParam("reaction.radius.z.down", 0);
					reactionStrategy.xyRadius = GetParam("reaction.radius.xy", 0);
					
					PathNode[] reactionTiles = reactionStrategy.GetReactionTiles(currentTarget.TilePosition);
					targets = reactionStrategy.CharactersForTargetedTiles(reactionTiles);
					ApplyEffectsTo(reactionEffects[hitType].effects, targets);
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
			float angle = Mathf.Atan2(ttp.x-ctp.x, ttp.y-ctp.y);
			SetParam("arg.distance", distance);
			SetParam("arg.mdistance", Mathf.Abs(ttp.x-ctp.x)+Mathf.Abs(ttp.y-ctp.y)+Mathf.Abs(ttp.z-ctp.z));
			SetParam("arg.mdistance.xy", Mathf.Abs(ttp.x-ctp.x)+Mathf.Abs(ttp.y-ctp.y));
			SetParam("arg.dx", Mathf.Abs(ttp.x-ctp.x));
			SetParam("arg.dy", Mathf.Abs(ttp.y-ctp.y));
			SetParam("arg.dz", Mathf.Abs(ttp.z-ctp.z));
			SetParam("arg.angle.xy", angle);
			foreach(StatEffect se in effects) {
				StatEffectRecord effect=null;
				switch(se.target) {
					case StatEffectTarget.Applier:
						character.SetBaseStat(se.statName, se.ModifyStat(character.GetStat(se.statName), this, null, null, out effect));
						Debug.Log("hit character, new "+se.statName+" "+character.GetStat(se.statName));
						break;
					case StatEffectTarget.Applied:
						c.SetBaseStat(se.statName, se.ModifyStat(c.GetStat(se.statName), this, null, null, out effect));
						Debug.Log("hit "+currentTarget+", new "+se.statName+" "+currentTarget.GetStat(se.statName));
						break;
				}
				lastEffects.Add(effect);
			}
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