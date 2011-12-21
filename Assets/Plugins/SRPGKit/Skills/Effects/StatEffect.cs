using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum StatEffectContext {
	Normal,
	Action
};

public enum StatEffectTarget {
	Applier,
	Applied
};

public enum StatEffectType {
	Augment,
	Multiply,
	Replace,
	
	ChangeFacing,
	
	EndTurn
};

public enum StatChangeType {
	Any,
	Change,
	Increase,
	Decrease,
	NoChange
}
[System.Serializable]
public struct StatChange {
	public string statName;
	public StatChangeType changeType;
	public StatChange(string stat, StatChangeType change) {
		statName = stat;
		changeType = change;
	}
}

[System.Serializable]
public class StatEffect {
	public string statName;
	public StatEffectType effectType=StatEffectType.Augment;
	public Formula value;
	
	//these two are only relevant for stateffects used in action and reaction skills
	public string[] reactableTypes;
	//for characters, equipment, and passive skills, "self", "wielder", or "character" is implicit
	public StatEffectTarget target = StatEffectTarget.Applied;
	
	public float ModifyStat(float stat, Skill scontext, Character ccontext, Equipment econtext, out StatEffectRecord rec) {
		float finalValue=value.GetValue(scontext, ccontext, econtext);
		rec = new StatEffectRecord(this, stat);
		switch(effectType) {
			case StatEffectType.Augment: return stat+finalValue;
			case StatEffectType.Multiply: return stat*finalValue;
			case StatEffectType.Replace: return finalValue;
		}
		Debug.LogError("unknown stat effect");
		return -1;
	}
	public float ModifyStat(float stat, Skill scontext, Character ccontext, Equipment econtext) {
		StatEffectRecord ignore;
		return ModifyStat(stat, scontext, ccontext, econtext, out ignore);
	}
	
	public StatEffectRecord Apply(Skill skill, Character character, Character targ) {
		StatEffectRecord effect=null;
		Character actualTarget=null;
		switch(target) {
			case StatEffectTarget.Applier:
				actualTarget = character;
				break;
			case StatEffectTarget.Applied:
				actualTarget = targ;
				break;
		}
		switch(effectType) {
			case StatEffectType.Augment:
			case StatEffectType.Multiply:
			case StatEffectType.Replace:
				actualTarget.SetBaseStat(
					statName, 
					ModifyStat(actualTarget.GetStat(statName), skill, null, null, out effect)
				);
				Debug.Log("hit "+actualTarget+", new "+statName+" "+actualTarget.GetStat(statName));
				break;
			case StatEffectType.ChangeFacing:
				float angle = value.GetValue(skill, targ, null);
				actualTarget.Facing = angle;
				effect = new StatEffectRecord(this, angle);
				break;
			case StatEffectType.EndTurn:
				effect = new StatEffectRecord(this, 0);
				skill.scheduler.Deactivate(actualTarget, this);
				break;
		}
		return effect;
	}
	
	//editor only
	public bool editorShowsReactableTypes=true;
}

[System.Serializable]
public class StatEffectRecord {
	public StatEffect effect;
	public float value;
	public StatEffectRecord(StatEffect e, float v) {
		effect = e;
		value = v;
	}
	
	public bool Matches(string[] statNames, StatChangeType[] changes, string[] reactableTypes) {
		bool statNameOK = statNames == null || statNames.Length == 0 || statNames.Contains(effect.statName);
		bool changeOK = changes == null || changes.Length == 0 || changes.Any(delegate(StatChangeType c) {
			switch(c) {
				case StatChangeType.Any: return true;
				case StatChangeType.NoChange: return value == 0;
				case StatChangeType.Change: return value != 0;
				case StatChangeType.Increase: return value > 0;
				case StatChangeType.Decrease: return value < 0;
			}
			return false;
		});
		bool typesOK = reactableTypes == null || reactableTypes.Length == 0 || reactableTypes.All(t => effect.reactableTypes.Contains(t));
		return statNameOK && changeOK && typesOK;
	}
	public bool Matches(string statName, StatChangeType change, string[] reactableTypes) {
		return Matches(new string[]{statName}, new StatChangeType[]{change}, reactableTypes);
	}
	public bool Matches(StatChange[] changes, string[] reactableTypes) {
		if(changes == null || changes.Length == 0) {
			return Matches(null, StatChangeType.Any, reactableTypes);
		}
		return changes.Any(c => Matches(c.statName, c.changeType, reactableTypes));
	}
}

