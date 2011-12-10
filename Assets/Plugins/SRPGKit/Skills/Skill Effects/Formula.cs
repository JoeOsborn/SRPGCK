using UnityEngine;
using System.Linq;

public enum FormulaType {
	Constant,
	Lookup, //variable, argument, formula, or stat
	ReactedEffectValue,
	//any number of arguments
	Add,
	Subtract,
	Multiply,
	Divide,
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
	Undefined
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
	TargetStatusEffect
}

public enum FormulaMergeMode {
	First,
	Last,
	Min,
	Max,
	Mean,
	Sum
}

public interface IFormulaElement {
	
}

[System.Serializable]
public class Formula : IFormulaElement {
	//only used in editor:
	public string text="", name="";
	public string compilationError="";
	
	//normal vars from here on

	public FormulaType formulaType;
	
	//constant
	public float constantValue;
	
	//lookup
	public string lookupReference;
	public LookupType lookupType;
	 //if a lookup returns multiple results
	public FormulaMergeMode mergeMode;
	//eq
	public string[] equipmentSlots, equipmentCategories;
	//effect lookup (e.g. "this reaction skill recovers MP equal to the amount used by the attacker")
	public string[] searchReactedStatNames; //we ignore lookupRef in this case
	public StatChangeType[] searchReactedStatChanges;
	public string[] searchReactedEffectCategories;
	
	//everything else
	public Formula[] arguments; //x+y+z or x*y*z or x^y (y default 2) or y√x (y default 2)
	
	public void CopyFrom(Formula f) {
/*		Debug.Log("copy from "+f);*/
		if(f == null) { return; }
		formulaType = f.formulaType;
		constantValue = f.constantValue;
		lookupReference = f.lookupReference;
		lookupType = f.lookupType;
		mergeMode = f.mergeMode;
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
		return f;
	}
	public static Formula Lookup(string n, LookupType type=LookupType.Auto) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Lookup;
		f.lookupReference = n;
		f.lookupType = type;
		return f;
	}
	
	public float GetCharacterValue(Character ccontext) {
		return GetValue(null, ccontext, null);
	}
	
	bool firstTime = true;
	public float GetValue(Skill scontext=null, Character ccontext=null, Equipment econtext=null) {
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
				result = Formulae.Lookup(lookupReference, lookupType, scontext, ccontext, econtext, this);
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
			case FormulaType.Add: 
				result = arguments[0].GetValue(scontext, ccontext, econtext);
				for(int i = 1; i < arguments.Length; i++) {
					result += arguments[i].GetValue(scontext, ccontext, econtext);
				}
				break;
			case FormulaType.Subtract: 
				result = arguments[0].GetValue(scontext, ccontext, econtext);
				for(int i = 1; i < arguments.Length; i++) {
					result -= arguments[i].GetValue(scontext, ccontext, econtext);
				}
				break;
			case FormulaType.Multiply: 
			  result = arguments[0].GetValue(scontext, ccontext, econtext);
			  for(int i = 1; i < arguments.Length; i++) {
			  	result *= arguments[i].GetValue(scontext, ccontext, econtext);
			  }
				break;
			case FormulaType.Divide: 
			  result = arguments[0].GetValue(scontext, ccontext, econtext);
			  for(int i = 1; i < arguments.Length; i++) {
			  	result /= arguments[i].GetValue(scontext, ccontext, econtext);
			  }
				break;
			case FormulaType.Exponent: 
			  result = arguments[0].GetValue(scontext, ccontext, econtext);
				if(arguments.Length == 1) {
					result = result * result;
				} else {
			  	for(int i = 1; i < arguments.Length; i++) {
			  		result = Mathf.Pow(result, arguments[i].GetValue(scontext, ccontext, econtext));
			  	}
				}
				break;
			case FormulaType.Root: 
			  result = arguments[0].GetValue(scontext, ccontext, econtext);
				if(arguments.Length == 1) {
					result = Mathf.Sqrt(result);
				} else {
			  	for(int i = 1; i < arguments.Length; i++) {
			  		result = Mathf.Pow(result, 1.0f/arguments[i].GetValue(scontext, ccontext, econtext));
			  	}
				}
				break;
			case FormulaType.Mean: 
				result = arguments.Sum(a => a.GetValue(scontext, ccontext, econtext)) / arguments.Count();
				break;
			case FormulaType.Min: 
				result = arguments.Min(a => a.GetValue(scontext, ccontext, econtext));
				break;
			case FormulaType.Max: 
				result = arguments.Max(a => a.GetValue(scontext, ccontext, econtext));
				break;
			case FormulaType.RandomRange: {
				float low=0, high=1;
				if(arguments.Length >= 1) {
					low = arguments[0].GetValue(scontext, ccontext, econtext);
				}
				if(arguments.Length >= 2) {
					high = arguments[1].GetValue(scontext, ccontext, econtext);
				}
				result = Random.Range(low, high);
				break;
			}
			case FormulaType.ClampRange: {
				float r = arguments[0].GetValue(scontext, ccontext, econtext);
				float low=0, high=1;
				if(arguments.Length >= 2) {
					low = arguments[1].GetValue(scontext, ccontext, econtext);
				}
				if(arguments.Length >= 3) {
					high = arguments[2].GetValue(scontext, ccontext, econtext);
				}
				result = Mathf.Clamp(r, low, high);
				break;
			}
			case FormulaType.RoundDown: 
				result = Mathf.Floor(arguments[0].GetValue(scontext, ccontext, econtext));
				break;
			case FormulaType.RoundUp: 
				result = Mathf.Ceil(arguments[0].GetValue(scontext, ccontext, econtext));
				break;
			case FormulaType.Round: 
				result = Mathf.Round(arguments[0].GetValue(scontext, ccontext, econtext));
				break;
			case FormulaType.AbsoluteValue: 
				result = Mathf.Abs(arguments[0].GetValue(scontext, ccontext, econtext));
				break;
			case FormulaType.Negate:
				result = -1 * arguments[0].GetValue(scontext, ccontext, econtext);
				break;
			case FormulaType.Equal:
				result = arguments[0].GetValue(scontext, ccontext, econtext) == arguments[1].GetValue(scontext, ccontext, econtext) ? 1 : 0;
				break;
			case FormulaType.NotEqual:
				result = arguments[0].GetValue(scontext, ccontext, econtext) != arguments[1].GetValue(scontext, ccontext, econtext) ? 1 : 0;
				break;
		  case FormulaType.GreaterThan:
				result = arguments[0].GetValue(scontext, ccontext, econtext) > arguments[1].GetValue(scontext, ccontext, econtext) ? 1 : 0;
		  	break;
		  case FormulaType.GreaterThanOrEqual:
				result = arguments[0].GetValue(scontext, ccontext, econtext) >= arguments[1].GetValue(scontext, ccontext, econtext) ? 1 : 0;
		  	break;
			case FormulaType.LessThan:
				result = arguments[0].GetValue(scontext, ccontext, econtext) < arguments[1].GetValue(scontext, ccontext, econtext) ? 1 : 0;
				break;
			case FormulaType.LessThanOrEqual:
				result = arguments[0].GetValue(scontext, ccontext, econtext) <= arguments[1].GetValue(scontext, ccontext, econtext) ? 1 : 0;
				break;
			case FormulaType.Any:
				result = arguments[Random.Range(0, arguments.Length)].GetValue(scontext, ccontext, econtext);
				break;
			case FormulaType.LookupSuccessful:
				result = Formulae.CanLookup(lookupReference, lookupType, scontext, ccontext, econtext, this) ? 1 : 0;
				break;
			case FormulaType.BranchIfNotZero:
				result = arguments[0].GetValue(scontext, ccontext, econtext) != 0 ?
				 	arguments[1].GetValue(scontext, ccontext, econtext) : 
					arguments[2].GetValue(scontext, ccontext, econtext);
				break;
			case FormulaType.BranchApplierSide:
				result = FacingSwitch(StatEffectTarget.Applied, scontext, ccontext, econtext);
				break;
			case FormulaType.BranchAppliedSide:
				result = FacingSwitch(StatEffectTarget.Applier, scontext, ccontext, econtext);
				break;
			case FormulaType.BranchPDF: 
				result = -1;
				float rval = Random.value;
				float val = 0;
				int halfLen = arguments.Length/2;
				for(int i = 0; i < halfLen; i++) {
					val += arguments[i].GetValue(scontext, ccontext, econtext);
					if(val >= rval) {
						result = arguments[i+halfLen].GetValue(scontext, ccontext, econtext);
						break;
					}
				}
				if(result == -1) {
					Debug.LogError("PDF adds up to less than 1");
				}
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
		
	protected float FacingSwitch(StatEffectTarget target, Skill scontext, Character ccontext, Equipment econtext) {
		if(scontext == null) { 
			Debug.LogError("Relative facing not available for non-attack/reaction skill effects."); 
			return -1;
		}
		Character applier = scontext.character;
		Character applied = scontext.currentTarget;
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
		float yAngle = y.FacingZ;
		//is theta(y,x) within 45 of yAngle?
		if(Mathf.Abs(Vector2.Angle(new Vector2(yp.x, yp.y), new Vector2(xp.x, xp.y))-yAngle) < 45) {
			//next, get the quadrant
			//quadrant ~~ theta (target -> other)
			float xyAngle = Mathf.Atan2(yp.x-xp.x, yp.y-xp.y)*Mathf.Rad2Deg + x.FacingZ;
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
		if(arguments.Length != 8) {
			Debug.Log("Bad facing switch in skill "+scontext.skillName);
		}
		if(pointing == CharacterPointing.Front && arguments[0] != null) {
			//front
			return arguments[0].GetValue(scontext, ccontext, econtext);
		} else if(pointing == CharacterPointing.Left && arguments[1] != null) {
			//left
			return arguments[1].GetValue(scontext, ccontext, econtext);
		} else if(pointing == CharacterPointing.Right && arguments[2] != null) {
			//right
			return arguments[2].GetValue(scontext, ccontext, econtext);
		} else if(pointing == CharacterPointing.Back && arguments[3] != null) {
			//back
			return arguments[3].GetValue(scontext, ccontext, econtext);
		} else if(pointing == CharacterPointing.Away && arguments[4] != null) {
			//away
			return arguments[4].GetValue(scontext, ccontext, econtext);
		} else if((pointing == CharacterPointing.Left || pointing == CharacterPointing.Right) && arguments[5] != null) {
			//sides
			return arguments[5].GetValue(scontext, ccontext, econtext);
		} else if((pointing != CharacterPointing.Away) && arguments[6] != null) {
			//towards
			return arguments[6].GetValue(scontext, ccontext, econtext);
		} else if(arguments[7] != null) {
			//default
			return arguments[7].GetValue(scontext, ccontext, econtext);
		} else {
			Debug.LogError("No valid branch for pointing "+pointing+" in skill "+scontext.skillName);
			return -1;
		}
	}
}