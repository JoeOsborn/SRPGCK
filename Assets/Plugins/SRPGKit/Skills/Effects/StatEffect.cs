using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum StatEffectContext {
	Normal,
	Action,
	Any
};

public enum StatEffectTarget {
	Applier,
	Applied
};

public enum StatEffectType {
	//active or passive
	Augment,
	Multiply,
	Replace,
	//active only
	ChangeFacing,
	EndTurn,
	SpecialMove
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
	public bool editorShow=false;

	public string statName;
	public StatEffectType effectType=StatEffectType.Augment;
	public Formula value;
	public bool respectLimits=true;
	public bool constrainValueToLimits=true;

	//these two are only relevant for stateffects used in action and reaction skills
	public string[] reactableTypes;
	//for characters, equipment, and passive skills, "self", "wielder", or "character" is implicit
	public StatEffectTarget target = StatEffectTarget.Applied;

	//these ones are only relevant for special moves
	public string specialMoveType="knockback";
	public float specialMoveSpeedXY=20, specialMoveSpeedZ=25;
	public bool specialMoveAnimateToStart=true;
	public Formula specialMoveGivenStartX, specialMoveGivenStartY, specialMoveGivenStartZ;
	public Region specialMoveLine;

	public float ModifyStat(float stat, SkillDef scontext, Character ccontext, Equipment econtext, ref float modValue) {
		Formulae fdb = scontext != null ? scontext.fdb : (ccontext != null ? ccontext.fdb : (econtext != null ? econtext.fdb : Formulae.DefaultFormulae));
		modValue=value.GetValue(fdb, scontext, ccontext, econtext);
		float modifiedValue = stat;
		switch(effectType) {
			case StatEffectType.Augment: modifiedValue = stat+modValue; break;
			case StatEffectType.Multiply: modifiedValue = stat*modValue; break;
			case StatEffectType.Replace: modifiedValue = modValue; break;
			default:
				Debug.LogError("improper stat effect type "+effectType);
				break;
		}
		return modifiedValue;
	}
	public float ModifyStat(float stat, SkillDef scontext, Character ccontext, Equipment econtext) {
		float ignore=0;
		return ModifyStat(stat, scontext, ccontext, econtext, ref ignore);
	}

	public StatEffectRecord Apply(SkillDef skill, Character character, Character targ) {
		Formulae fdb = skill != null ? skill.fdb : (character != null ? character.fdb : (targ != null ? targ.fdb : Formulae.DefaultFormulae));
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
				float modValue = 0;
				float oldStat = actualTarget.GetBaseStat(statName);
				float newStat = ModifyStat(oldStat, skill, null, null, ref modValue);
				// Debug.Log("base modvalue is "+modValue+", newstat is "+newStat);
				newStat = actualTarget.SetBaseStat(statName, newStat, respectLimits);
				if(constrainValueToLimits) {
					modValue = effectType == StatEffectType.Replace ? newStat : newStat - oldStat;
				}
				effect = new StatEffectRecord(this, oldStat, newStat, modValue);
				Debug.Log("hit "+actualTarget+"("+target+") for "+modValue+", new "+statName+" "+newStat);
				// Debug.Log("vtype "+value.formulaType+" ltype "+value.lookupType);
				break;
			case StatEffectType.ChangeFacing:
				float oldAngle = actualTarget.Facing;
				float angle = value.GetValue(fdb, skill, targ, null);
				actualTarget.Facing = angle;
				Debug.Log("set facing to "+angle);
				effect = new StatEffectRecord(this, oldAngle, angle, angle);
				break;
			case StatEffectType.EndTurn:
				effect = new StatEffectRecord(this);
				Debug.Log("end turn");
				skill.scheduler.DeactivateAfterSkillApplication(actualTarget, skill);
				break;
			case StatEffectType.SpecialMove: {
				if(specialMoveLine == null) {
					Debug.LogError("Undefined move line");
				}
				specialMoveLine.Owner = skill;
				Vector3 start = new Vector3(
					specialMoveGivenStartX.GetValue(fdb, skill, targ, null),
					specialMoveGivenStartY.GetValue(fdb, skill, targ, null),
					specialMoveGivenStartZ.GetValue(fdb, skill, targ, null)
				);
				effect = new StatEffectRecord(this, start);
				actualTarget.SpecialMove(
					start,
					specialMoveAnimateToStart,
					specialMoveType,
					specialMoveLine,
					specialMoveSpeedXY,
					specialMoveSpeedZ,
					skill
				);
				break;
			}
		}
		return effect;
	}
	//editor only
	public bool editorShowsReactableTypes=true;
}

[System.Serializable]
public class StatEffectRecord {
	public StatEffect effect;
	public float initialValue, finalValue, value;
	public Vector3 specialMoveStart;
	public StatEffectRecord(StatEffect e, float init=0, float endVal=0, float v=0) {
		effect = e;
		initialValue = init;
		finalValue = endVal;
		value = v;
	}
	public StatEffectRecord(StatEffect e, Vector3 start) {
		effect = e;
		specialMoveStart = start;
	}

	public bool Matches(string[] statNames, StatChangeType[] changes, string[] reactableTypes) {
		bool statNameOK = statNames == null || statNames.Length == 0 || statNames.Contains(effect.statName);
		bool changeOK = changes == null || changes.Length == 0 || changes.Any(delegate(StatChangeType c) {
			switch(c) {
				case StatChangeType.Any: return true;
				case StatChangeType.NoChange: return value == initialValue;
				case StatChangeType.Change: return value != initialValue;
				case StatChangeType.Increase: return value > initialValue;
				case StatChangeType.Decrease: return value < initialValue;
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

