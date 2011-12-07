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

[System.Serializable]
public class StatEffect {
	public string statName;
	public StatEffectType effectType=StatEffectType.Augment;
	public Formula value;

	//only relevant for stateffects used in action and reaction skills; 
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

public class StatEffectRecord {
	public StatEffect effect;
	public float value;
	public StatEffectRecord(StatEffect e, float v) {
		effect = e;
		value = v;
	}
}