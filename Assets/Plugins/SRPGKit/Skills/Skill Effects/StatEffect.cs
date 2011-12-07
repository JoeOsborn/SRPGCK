using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum StatEffectTarget {
	Applier,
	Applied
};

public enum StatEffectType {
	Augment,
	Multiply,
	Replace
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
}

[System.Serializable]
public class StatEffectRecord {
	public StatEffect effect;
	public float value;
	public StatEffectRecord(StatEffect e, float v) {
		effect = e;
		value = v;
	}
	
	public bool Matches(string statName, StatChangeType change, string[] reactableTypes) {
		bool statNameOK = statName == null || statName == "" || statName == effect.statName;
		bool changeOK = false;
		switch(change) {
			case StatChangeType.Any: changeOK = true; break;
			case StatChangeType.NoChange: changeOK = value == 0; break;
			case StatChangeType.Change: changeOK = value != 0; break;
			case StatChangeType.Increase: changeOK = value > 0; break;
			case StatChangeType.Decrease: changeOK = value < 0; break;
		}
		bool typesOK = reactableTypes == null || reactableTypes.Length == 0 || reactableTypes.All(t => effect.reactableTypes.Contains(t));
		return statNameOK && changeOK && typesOK;
	}
	public bool Matches(StatChange[] changes, string[] reactableTypes) {
		if(changes == null || changes.Length == 0) {
			return Matches(null, StatChangeType.Any, reactableTypes);
		}
		return changes.Any(c => Matches(c.statName, c.changeType, reactableTypes));
	}
}

