using UnityEngine;

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
	//4 arguments: if 0 is comparison-type 1, 2; else 3
	Equal,
	NotEqual,
	GreaterThan,
	GreaterThanOrEqual,
	LessThan,
	LessThanOrEqual,
	//any number of arguments
	Any,
	//2 arguments: if true, 0; else 1
	IfLookupSuccessful,
	//up to 6 arguments, of which any can be null (in which case this returns arg[5] or 0 if arg[5] is null)
	//front, sides, back, left, right, default
	BranchApplierSide,
	BranchAppliedSide
}

public enum LookupType {
	Undefined,
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
	//lookupReference is the stat name
	ReactedEffectType,
	//lookupReference is the effect type
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

[System.Serializable]
public class Formula {
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
	//reacted skill effect lookup; could use lookupReference for stat name, or could leave it blank
	public string[] reactableCategories;
	public StatChangeType reactedStatChange;
	
	//everything else
	public Formula[] arguments; //x+y+z or x*y*z or x^y (y default 2) or y√x (y default 2)
	
	public static Formula Constant(float c) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Constant;
		f.constantValue = c;
		return f;
	}
	public static Formula Lookup(string n, LookupType type) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Lookup;
		f.lookupReference = n;
		f.lookupType = type;
		return f;
	}
	
	public float GetCharacterValue(Character ccontext) {
		return GetValue(null, ccontext);
	}
	
	bool firstTime = true;
	public float GetValue(Skill scontext=null, Character ccontext=null, Equipment econtext=null) {
		if(firstTime) {
			lookupReference = lookupReference.NormalizeName();
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
				result = 
					arguments[0].GetValue(scontext, ccontext, econtext) == arguments[1].GetValue(scontext, ccontext, econtext) ?
					 arguments[2].GetValue(scontext, ccontext, econtext) : arguments[3].GetValue(scontext, ccontext, econtext);
				break;
			case FormulaType.NotEqual:
				result = 
					arguments[0].GetValue(scontext, ccontext, econtext) != arguments[1].GetValue(scontext, ccontext, econtext) ?
					 arguments[2].GetValue(scontext, ccontext, econtext) : arguments[3].GetValue(scontext, ccontext, econtext);
				break;
		  case FormulaType.GreaterThan:
		  	result = 
		  		arguments[0].GetValue(scontext, ccontext, econtext) > arguments[1].GetValue(scontext, ccontext, econtext) ?
		  		 arguments[2].GetValue(scontext, ccontext, econtext) : arguments[3].GetValue(scontext, ccontext, econtext);
		  	break;
		  case FormulaType.GreaterThanOrEqual:
		  	result = 
		  		arguments[0].GetValue(scontext, ccontext, econtext) >= arguments[1].GetValue(scontext, ccontext, econtext) ?
		  		 arguments[2].GetValue(scontext, ccontext, econtext) : arguments[3].GetValue(scontext, ccontext, econtext);
		  	break;
			case FormulaType.LessThan:
				result = 
					arguments[0].GetValue(scontext, ccontext, econtext) < arguments[1].GetValue(scontext, ccontext, econtext) ?
					 arguments[2].GetValue(scontext, ccontext, econtext) : arguments[3].GetValue(scontext, ccontext, econtext);
				break;
			case FormulaType.LessThanOrEqual:
				result = 
					arguments[0].GetValue(scontext, ccontext, econtext) <= arguments[1].GetValue(scontext, ccontext, econtext) ?
					 arguments[2].GetValue(scontext, ccontext, econtext) : arguments[3].GetValue(scontext, ccontext, econtext);
				break;
			case FormulaType.Any:
				result = arguments[Random.Range(0, arguments.Length)].GetValue(scontext, ccontext, econtext);
				break;
				//2 arguments: if true, 0; else 1
			case FormulaType.IfLookupSuccessful:
				bool lookupSuccessful = Formulae.CanLookup(lookupReference, lookupType, scontext, ccontext, econtext, this);
				if(lookupSuccessful) {
					result = arguments[0].GetValue(scontext, ccontext, econtext);
				} else {
					result = arguments[1].GetValue(scontext, ccontext, econtext);
				}
				break;
			case FormulaType.BranchApplierSide:
				result = FacingSwitch(StatEffectTarget.Applied, scontext, ccontext, econtext);
				break;
			case FormulaType.BranchAppliedSide:
				result = FacingSwitch(StatEffectTarget.Applier, scontext, ccontext, econtext);
				break;
		}
		return result;
	}
	
	enum CharacterPointing {
		Front,
		Back,
		Left,
		Right,
		Away
	};
		
	protected float FacingSwitch(StatEffectTarget target, Skill scontext, Character ccontext, Equipment econtext) {
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
		//front, left, right, back, away, sides, default
		//must have null entries
		if(arguments.Length != 7) {
			Debug.Log("Bad facing switch in skill "+scontext.skillName);
		}
		if(pointing == CharacterPointing.Front && arguments[0] != null) {
			return arguments[0].GetValue(scontext, ccontext, econtext);
		} else if(pointing == CharacterPointing.Left && arguments[1] != null) {
			return arguments[1].GetValue(scontext, ccontext, econtext);
		} else if(pointing == CharacterPointing.Right && arguments[2] != null) {
			return arguments[2].GetValue(scontext, ccontext, econtext);
		} else if(pointing == CharacterPointing.Back && arguments[3] != null) {
			return arguments[3].GetValue(scontext, ccontext, econtext);
		} else if(pointing == CharacterPointing.Away && arguments[4] != null) {
			return arguments[4].GetValue(scontext, ccontext, econtext);
		} else if((pointing == CharacterPointing.Left || pointing == CharacterPointing.Right) && arguments[5] != null) {
			return arguments[5].GetValue(scontext, ccontext, econtext);
		} else if(arguments[6] != null) {
			return arguments[6].GetValue(scontext, ccontext, econtext);
		} else {
			Debug.LogError("No valid branch for pointing "+pointing+" in skill "+scontext.skillName);
			return -1;
		}
	}
}