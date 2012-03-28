using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public enum FormulaType {
	Constant,
	Lookup, //variable, argument, formula, or stat
	ReactedEffectValue,
	//any number of arguments
	Add,
	Subtract,
	Multiply,
	Divide,
	Remainder,
	Exponent,
	Root,
	Mean,
	Min,
	Max,
	//must be 2 arguments
	RandomRange,
	//must be 3 arguments
	ClampRange,
	//must be 1 argument
	RoundDown,
	RoundUp,
	Round,
	AbsoluteValue,
	Negate,
	//2 arguments
	Equal,
	NotEqual,
	GreaterThan,
	GreaterThanOrEqual,
	LessThan,
	LessThanOrEqual,
	BranchIfNotZero,
	//any number of arguments
	Any,
	//2 arguments: if true, 0; else 1
	LookupSuccessful,
	//up to 8 arguments, of which any can be null (in which case this returns last arg or fails if last arg is null)
	//front, left, right, back, away, sides, towards, default
	BranchApplierSide,
	BranchAppliedSide,
	//even number of arguments: cases, then formulae
	BranchPDF,
	Undefined,
		//...
	SkillEffectValue,
	Or,
	And,
	Not,
	TargetIsNull,
	TargetIsNotNull
}

public enum LookupType {
	Auto,
	//lookupReference is the skill param/character stat
	SkillParam,
	ActorStat,
	ActorEquipmentParam,
	ActorSkillParam,
	TargetStat,
	TargetEquipmentParam,
	TargetSkillParam,
	//lookupReference is the formula name
	NamedFormula,
	//lookupReference is the skill param
	ReactedSkillParam,
	//lookupReference is unused
	ReactedEffectType,
	//TODO: AppliedEffectType?
	//lookupReference is the status effect type
	ActorStatusEffect,
	TargetStatusEffect,
	//lookupReference is unused
	SkillEffectType
}

public enum FormulaMergeMode {
	First,
	Last,
	Min,
	Max,
	Mean,
	Sum,
	Nth
}

public interface IFormulaElement {

}

[System.Serializable]
public class Formula : IFormulaElement {
	//only used in editor:
	public string text="";
	public string compilationError="";

	//normal vars from here on
	public string name;
	public FormulaType formulaType;

	//constant
	public float constantValue;

	//lookup
	public string lookupReference;
	public LookupType lookupType;
	 //if a lookup returns multiple results
	public FormulaMergeMode mergeMode;
	//for Nth merge mode
	public int mergeNth=0;
	//eq
	public string[] equipmentSlots, equipmentCategories;
	//effect lookup (e.g. "this reaction skill recovers MP equal to the amount used by the attacker")
	//"reacted" is in the names, but it's also used for SkillEffectType.
	public string[] searchReactedStatNames; //we ignore lookupRef in this case
	public StatChangeType[] searchReactedStatChanges;
	public string[] searchReactedEffectCategories;

	//everything else
	public List<Formula> arguments; //x+y+z or x*y*z or x^y (y default 2) or y√x (y default 2)
	public Formula Clone() {
		Formula f = new Formula();
		f.CopyFrom(this);
		return f;
	}
	public void CopyFrom(Formula f) {
/*		Debug.Log("copy from "+f);*/
		if(f == null) { return; }
		name = f.name;
		text = f.text;
		formulaType = f.formulaType;
		constantValue = f.constantValue;
		lookupReference = f.lookupReference;
		lookupType = f.lookupType;
		mergeMode = f.mergeMode;
		mergeNth = f.mergeNth;
		equipmentSlots = f.equipmentSlots;
		equipmentCategories = f.equipmentCategories;
		searchReactedStatNames = f.searchReactedStatNames;
		searchReactedStatChanges = f.searchReactedStatChanges;
		searchReactedEffectCategories = f.searchReactedEffectCategories;
		arguments = f.arguments;
	}

	public static Formula Constant(float c) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Constant;
		f.constantValue = c;
		f.text = ""+c;
		return f;
	}
	public static Formula True() {
		Formula f = new Formula();
		f.formulaType = FormulaType.Constant;
		f.constantValue = 1;
		f.text = "true";
		return f;
	}
	public static Formula False() {
		Formula f = new Formula();
		f.formulaType = FormulaType.Constant;
		f.constantValue = 0;
		f.text = "false";
		return f;
	}

	public static Formula Lookup(string n, LookupType type=LookupType.Auto) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Lookup;
		f.lookupReference = n;
		f.lookupType = type;
		f.text = n;
		switch(type) {
			case LookupType.Auto:
				break;
			case LookupType.SkillParam:
				f.text = "skill."+f.text;
				break;
			case LookupType.ActorStat:
				f.text = "c."+f.text;
				break;
			case LookupType.ActorSkillParam:
				f.text = "c.skill."+f.text;
				break;
			case LookupType.TargetStat:
				f.text = "t."+f.text;
				break;
			case LookupType.TargetSkillParam:
				f.text = "t.skill."+f.text;
				break;
			case LookupType.NamedFormula:
				f.text = "f."+f.text;
				break;
			case LookupType.ReactedSkillParam:
				f.text = "reacted-skill."+f.text;
				break;
			//faked:...
			case LookupType.ActorEquipmentParam:
				f.text = "c.equip."+f.text;
				break;
			case LookupType.TargetEquipmentParam:
				f.text = "t.equip."+f.text;
				break;
			case LookupType.ReactedEffectType:
				f.text = "effect."+f.text;
				break;
			//TODO: AppliedEffectType?
			case LookupType.ActorStatusEffect:
				f.text = "c.status."+f.text;
				break;
			case LookupType.TargetStatusEffect:
				f.text = "t.status."+f.text;
				break;
		}
		return f;
	}

	public float GetCharacterValue(Formulae fdb, Character ccontext) {
		return GetValue(fdb, null, ccontext);
	}

	bool firstTime = true;
	public float GetValue(Formulae fdb, SkillDef scontext=null, Character ccontext=null, Character tcontext=null, Equipment econtext=null) {
		if(firstTime) {
			lookupReference = lookupReference == null ? "" : lookupReference.NormalizeName();
			firstTime = false;
		}
		float result=-1;
		switch(formulaType) {
			case FormulaType.Constant:
				result = constantValue;
				break;
			case FormulaType.Lookup:
				result = fdb.Lookup(lookupReference, lookupType, scontext, ccontext, tcontext, econtext, this);
				break;
			case FormulaType.ReactedEffectValue:
				if(scontext == null) {
					Debug.LogError("No skill context.");
					return -1;
				}
				if(scontext.currentReactedEffect == null) {
					Debug.LogError("Skill context is reacting to no particular effect.");
					return -1;
				}
				result = scontext.currentReactedEffect.value;
				break;
			case FormulaType.SkillEffectValue:
				if(scontext == null) {
					Debug.LogError("No skill context.");
					return -1;
				}
				if(scontext.lastEffects == null || scontext.lastEffects.Count == 0) {
					Debug.LogError("Skill context has no prior effects.");
					return -1;
				}
				result = scontext.lastEffects[scontext.lastEffects.Count-1].value;
				break;
			case FormulaType.Add:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext);
				for(int i = 1; i < arguments.Count; i++) {
					result += arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext);
				}
				break;
			case FormulaType.Subtract:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext);
				for(int i = 1; i < arguments.Count; i++) {
					result -= arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext);
				}
				break;
			case FormulaType.Multiply:
			  result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext);
			  for(int i = 1; i < arguments.Count; i++) {
			  	result *= arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext);
			  }
				// Debug.Log("multiplied to "+result);
				break;
			case FormulaType.Divide:
			  result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext);
			  for(int i = 1; i < arguments.Count; i++) {
			  	result /= arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext);
			  }
				// Debug.Log("divided to "+result);
				break;
			case FormulaType.And:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext);
				for(int i = 1; i < arguments.Count; i++) {
					result = ((result != 0) && (arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext) != 0)) ? 1 : 0;
				}
				break;
			case FormulaType.Or:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext);
				for(int i = 1; i < arguments.Count; i++) {
					result = ((result != 0) || (arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext) != 0)) ? 1 : 0;
				}
				break;
			case FormulaType.Not:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext) != 0 ? 0 : 1;
				break;
			case FormulaType.Remainder:
			  result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext);
			  for(int i = 1; i < arguments.Count; i++) {
			  	result = result % arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext);
			  }
				break;
			case FormulaType.Exponent:
			  result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext);
				if(arguments.Count == 1) {
					result = result * result;
				} else {
			  	for(int i = 1; i < arguments.Count; i++) {
			  		result = Mathf.Pow(result, arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext));
			  	}
				}
				break;
			case FormulaType.Root:
			  result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext);
				if(arguments.Count == 1) {
					result = Mathf.Sqrt(result);
				} else {
			  	for(int i = 1; i < arguments.Count; i++) {
			  		result = Mathf.Pow(result, 1.0f/arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext));
			  	}
				}
				break;
			case FormulaType.Mean:
				result = arguments.Sum(a => a.GetValue(fdb, scontext, ccontext, tcontext, econtext)) / arguments.Count();
				break;
			case FormulaType.Min:
				result = arguments.Min(a => a.GetValue(fdb, scontext, ccontext, tcontext, econtext));
				break;
			case FormulaType.Max:
				result = arguments.Max(a => a.GetValue(fdb, scontext, ccontext, tcontext, econtext));
				break;
			case FormulaType.RandomRange: {
				float low=0, high=1;
				if(arguments.Count >= 2) {
					low = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext);
					high = arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext);
				} else if(arguments.Count == 1) {
					high = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext);
				}
				result = Random.Range(low, high);
				break;
			}
			case FormulaType.ClampRange: {
				float r = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext);
				float low=0, high=1;
				if(arguments.Count >= 2) {
					low = arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext);
				}
				if(arguments.Count >= 3) {
					high = arguments[2].GetValue(fdb, scontext, ccontext, tcontext, econtext);
				}
				result = Mathf.Clamp(r, low, high);
				break;
			}
			case FormulaType.RoundDown:
				result = Mathf.Floor(arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext));
				break;
			case FormulaType.RoundUp:
				result = Mathf.Ceil(arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext));
				break;
			case FormulaType.Round:
				result = Mathf.Round(arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext));
				break;
			case FormulaType.AbsoluteValue:
				result = Mathf.Abs(arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext));
				break;
			case FormulaType.Negate:
				result = -1 * arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext);
				// Debug.Log("negated to "+result);
				break;
			case FormulaType.Equal:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext) == arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext) ? 1 : 0;
				break;
			case FormulaType.NotEqual:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext) != arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext) ? 1 : 0;
				break;
		  case FormulaType.GreaterThan:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext) > arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext) ? 1 : 0;
		  	break;
		  case FormulaType.GreaterThanOrEqual:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext) >= arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext) ? 1 : 0;
		  	break;
			case FormulaType.LessThan:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext) < arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext) ? 1 : 0;
				break;
			case FormulaType.LessThanOrEqual:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext) <= arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext) ? 1 : 0;
				break;
			case FormulaType.Any:
				result = arguments[Random.Range(0, arguments.Count)].GetValue(fdb, scontext, ccontext, tcontext, econtext);
				break;
			case FormulaType.LookupSuccessful:
				result = fdb.CanLookup(lookupReference, lookupType, scontext, ccontext, null, econtext, this) ? 1 : 0;
				break;
			case FormulaType.BranchIfNotZero:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext) != 0 ?
				 	arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext) :
					arguments[2].GetValue(fdb, scontext, ccontext, tcontext, econtext);
				break;
			case FormulaType.BranchApplierSide:
				result = FacingSwitch(fdb, StatEffectTarget.Applied, scontext, ccontext, tcontext, econtext);
				break;
			case FormulaType.BranchAppliedSide:
				result = FacingSwitch(fdb, StatEffectTarget.Applier, scontext, ccontext, tcontext, econtext);
				break;
			case FormulaType.BranchPDF:
				result = -1;
				float rval = Random.value;
				float val = 0;
				int halfLen = arguments.Count/2;
				for(int i = 0; i < halfLen; i++) {
					val += arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext);
					if(val >= rval) {
						result = arguments[i+halfLen].GetValue(fdb, scontext, ccontext, tcontext, econtext);
						break;
					}
				}
				if(result == -1) {
					Debug.LogError("PDF adds up to less than 1");
				}
				break;
			case FormulaType.TargetIsNull:
				result = (scontext != null ? scontext.currentTargetCharacter == null : tcontext == null) ? 1 : 0;
				break;
			case FormulaType.TargetIsNotNull:
				result = (scontext != null ? scontext.currentTargetCharacter != null : tcontext != null) ? 1 : 0;
				break;
		}
		return result;
	}

	enum CharacterPointing {
		Front,
		Back,
		Left,
		Right,
		Away,
		Towards
	};

	protected float FacingSwitch(
		Formulae fdb,
		StatEffectTarget target,
		SkillDef scontext,
		Character ccontext,
		Character tcontext,
		Equipment econtext
	) {
		if(scontext == null) {
			Debug.LogError("Relative facing not available for non-attack/reaction skill effects.");
			return -1;
		}
		Character applier = scontext != null ? scontext.character : ccontext;
		Character applied = scontext != null ? scontext.currentTargetCharacter : tcontext;
		CharacterPointing pointing = CharacterPointing.Front;
		Character x = null, y = null;
		if(target == StatEffectTarget.Applier) {
			x = applier;
			y = applied;
		} else if(target == StatEffectTarget.Applied) {
			x = applied;
			y = applier;
		}
		Vector3 xp = x.TilePosition;
		Vector3 yp = y.TilePosition;

		//see if y is facing towards x at all
		float yAngle = y.Facing;
		//is theta(y,x) within 45 of yAngle?
		if(Mathf.Abs(Vector2.Angle(new Vector2(yp.x, yp.y), new Vector2(xp.x, xp.y))-yAngle) < 45) {
			//next, get the quadrant
			//quadrant ~~ theta (target -> other)
			float xyAngle = Mathf.Atan2(yp.x-xp.x, yp.y-xp.y)*Mathf.Rad2Deg + x.Facing;
			while(xyAngle < 0) { xyAngle += 360; }
			while(xyAngle >= 360) { xyAngle -= 360; }
			if(xyAngle >= 45 && xyAngle < 135) {
				pointing = CharacterPointing.Left;
			} else if(xyAngle >= 135 && xyAngle < 225) {
				pointing = CharacterPointing.Back;
			} else if(xyAngle >= 225 && xyAngle < 315) {
				pointing = CharacterPointing.Right;
			} else {
				pointing = CharacterPointing.Front;
			}
		} else {
			pointing = CharacterPointing.Away;
		}

		//order:
		//front, left, right, back, away, sides, towards, default
		//must have null entries
		if(arguments.Count != 8) {
			Debug.Log("Bad facing switch in skill "+(scontext != null ? scontext.skillName : "none"));
		}
		if(pointing == CharacterPointing.Front && arguments[0] != null) {
			//front
			return arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext);
		} else if(pointing == CharacterPointing.Left && arguments[1] != null) {
			//left
			return arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext);
		} else if(pointing == CharacterPointing.Right && arguments[2] != null) {
			//right
			return arguments[2].GetValue(fdb, scontext, ccontext, tcontext, econtext);
		} else if(pointing == CharacterPointing.Back && arguments[3] != null) {
			//back
			return arguments[3].GetValue(fdb, scontext, ccontext, tcontext, econtext);
		} else if(pointing == CharacterPointing.Away && arguments[4] != null) {
			//away
			return arguments[4].GetValue(fdb, scontext, ccontext, tcontext, econtext);
		} else if((pointing == CharacterPointing.Left || pointing == CharacterPointing.Right) && arguments[5] != null) {
			//sides
			return arguments[5].GetValue(fdb, scontext, ccontext, tcontext, econtext);
		} else if((pointing != CharacterPointing.Away) && arguments[6] != null) {
			//towards
			return arguments[6].GetValue(fdb, scontext, ccontext, tcontext, econtext);
		} else if(arguments[7] != null) {
			//default
			return arguments[7].GetValue(fdb, scontext, ccontext, tcontext, econtext);
		} else {
			Debug.LogError("No valid branch for pointing "+pointing+" in skill "+(scontext != null ? scontext.skillName : "none"));
			return -1;
		}
	}
}