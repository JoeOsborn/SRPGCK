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
	Any
}

public enum LookupType {
	Undefined,
	SkillParam,
	ActorStat,
	ActorEquipmentParam,
	ActorSkillParam, 
	TargetStat,
	TargetEquipmentParam,
	TargetSkillParam,
	NamedFormula,
	ReactedSkillParam,
	ReactedEffectType
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
		}
		return result;
	}	
}