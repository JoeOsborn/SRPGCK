using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Skill : MonoBehaviour {
	public bool isPassive=false;
	[HideInInspector]
	public bool isActive=false;
	public string skillName;
	public string skillGroup="";
	public int skillSorting = 0;

	public string replacedSkill = "";
	public int replacementPriority=0;
		
	public StatEffect[] passiveEffects;
	
	public List<string> parameterNames;
	public List<Formula> parameterFormulae;

	Dictionary<string, Formula> runtimeParameters;
	
	public StatEffect[] targetEffects;
	
	//only relevant to targeted skills, sadly
	[HideInInspector]
	[SerializeField]
	protected List<Character> targets;
	[HideInInspector]
	[SerializeField]
	protected Character currentTarget;
	
	//reaction
	public bool reactionSkill=false;
	public string[] reactionTypes;
	//tile validation strategy (line/range/cone/etc)
	public ActionStrategy reactionStrategy;
	public StatEffect[] reactionEffects;
	//also the reaction.chance param

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
		DeactivateSkill();	
	}
	
	public virtual bool ReactionTypesMatch(Skill s) {
		if(!(s is AttackSkill)) { return false; }
		string[] actionTypes = (s as AttackSkill).actionTypes;
		return reactionTypes == null || 
		  reactionTypes.Length == 0 || 
		  actionTypes == null || 
		  actionTypes.Length == 0 || 
		  reactionTypes.Intersect(actionTypes).Count() > 0;
	}

	public virtual bool ReactsAgainst(Skill s) {
		return s != this && //don't react against your own application
					 s.character != character && //don't react against your own character's skills
			 		 s.currentTarget == character && //only react to skills used against our character
					 reactionSkill && //only react if you're a reaction skill
					 !s.reactionSkill && //don't react against reaction skills
			 		 ReactionTypesMatch(s); //only react if masks match
	}
	public virtual void SkillApplied(Skill s) {
		if(ReactsAgainst(s)) {
			currentTarget = s.character;				
			float v = GetParam("reaction.chance");
			if(Random.value < v) {
				reactionStrategy.owner = this;
				reactionStrategy.zRangeUpMin = GetParam("reaction.range.z.up.min");
				reactionStrategy.zRangeUpMax = GetParam("reaction.range.z.up.max");
				reactionStrategy.zRangeDownMin = GetParam("reaction.range.z.down.min");
				reactionStrategy.zRangeDownMax = GetParam("reaction.range.z.down.max");
				reactionStrategy.xyRangeMin = GetParam("reaction.range.xy.min");
				reactionStrategy.xyRangeMax = GetParam("reaction.range.xy.max");

				reactionStrategy.zRadiusUp = GetParam("reaction.radius.z.up");
				reactionStrategy.zRadiusDown = GetParam("reaction.radius.z.down");
				reactionStrategy.xyRadius = GetParam("reaction.radius.xy");
				
				PathNode[] reactionTiles = reactionStrategy.GetReactionTiles(currentTarget.TilePosition);
				targets = new List<Character>();
				foreach(PathNode pn in reactionTiles) {
					Character c = map.CharacterAt(pn.pos);
					if(c != null) {
						targets.Add(c);
					}
				}
				foreach(Character c in targets) {
					currentTarget = c;
					foreach(StatEffect se in reactionEffects) {
						//TODO: associated equipment?
						currentTarget.SetBaseStat(se.statName, se.ModifyStat(c.GetStat(se.statName), this, currentTarget, null));
						Debug.Log("hit "+currentTarget+", new health "+currentTarget.GetStat("health"));
					}
				}
				map.BroadcastMessage("SkillApplied", this, SendMessageOptions.DontRequireReceiver);
			}
		}
	}
	
	void MakeParametersIfNecessary() {
		if(runtimeParameters == null) {
			runtimeParameters = new Dictionary<string, Formula>();
			for(int i = 0; i < parameterNames.Count; i++) {
				runtimeParameters.Add(parameterNames[i].NormalizeName(), parameterFormulae[i]);
			}
		}
	}
	
	public bool HasParam(string pname) {
		MakeParametersIfNecessary();
		return runtimeParameters.ContainsKey(pname);
	}
	
	public float GetParam(string pname) {
		MakeParametersIfNecessary();
		return runtimeParameters[pname].GetValue(this, currentTarget);
	}
	
	public void AddParam(string pname, Formula f) {
		MakeParametersIfNecessary();
		runtimeParameters[pname] = f;
		parameterNames = parameterNames.Concat(new string[]{pname}).ToList();
		parameterFormulae = parameterFormulae.Concat(new Formula[]{f}).ToList();
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