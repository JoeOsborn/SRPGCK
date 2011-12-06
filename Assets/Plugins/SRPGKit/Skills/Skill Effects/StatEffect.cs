using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class StatEffect {
	public string statName;
	public enum EffectType {
		Augment,
		Multiply,
		Replace
	};
	public EffectType effectType=EffectType.Augment;
	public Formula value;
	
	public float ModifyStat(float stat, Skill scontext, Character ccontext) {
		switch(effectType) {
			case EffectType.Augment: return stat+value.GetValue(scontext, ccontext);
			case EffectType.Multiply: return stat*value.GetValue(scontext, ccontext);
			case EffectType.Replace: return value.GetValue(scontext, ccontext);
		}
		Debug.LogError("unknown stat effect");
		return -1;
	}
}