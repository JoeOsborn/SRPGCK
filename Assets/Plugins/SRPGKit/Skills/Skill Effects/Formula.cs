using UnityEngine;

[System.Serializable]
public class Formula {
	public enum Type {
		Constant,
		Lookup, //variable, argument, formula, or stat
		TargetLookup, //variable, argument, formula, or stat
		//everything else
		Add,
		Subtract,
		Multiply,
		Divide,
		Exponent,
		Root,
		//must be 2 elements
		RandomRange,
		//must be 3 elements
		ClampRange
	}
	public Type formulaType;
	
	//constant
	public float constantValue;
	
	//lookup
	public string lookupReference;
	
	//everything else
	public Formula[] arguments; //x+y+z or x*y*z or x^y (y default 2) or y√x (y default 2)
	
	public float GetCharacterValue(Character ccontext) {
		return GetValue(null, ccontext);
	}
	
	bool firstTime = true;
	public float GetValue(Skill scontext=null, Character ccontext=null) {
		if(firstTime) {
			lookupReference = lookupReference.NormalizeName();
			firstTime = false;
		}
		float result=-1;
		switch(formulaType) {
			case Type.Constant: 
				result = constantValue;
				break;
			case Type.Lookup: 
				//HACK: don't provide the target if this is supposed to be a self lookup
				if(scontext != null) {
					result = Formulae.Lookup(lookupReference, scontext, null);
				} else {
					result = Formulae.Lookup(lookupReference, scontext, ccontext);
				}
				break;
			case Type.TargetLookup: 
				result = Formulae.Lookup(lookupReference, scontext, ccontext);
				break;
			case Type.Add: 
				result = arguments[0].GetValue(scontext, ccontext);
				for(int i = 1; i < arguments.Length; i++) {
					result += arguments[i].GetValue(scontext, ccontext);
				}
				break;
			case Type.Subtract: 
				result = arguments[0].GetValue(scontext, ccontext);
				for(int i = 1; i < arguments.Length; i++) {
					result -= arguments[i].GetValue(scontext, ccontext);
				}
				break;
			case Type.Multiply: 
			  result = arguments[0].GetValue(scontext, ccontext);
			  for(int i = 1; i < arguments.Length; i++) {
			  	result *= arguments[i].GetValue(scontext, ccontext);
			  }
				break;
			case Type.Divide: 
			  result = arguments[0].GetValue(scontext, ccontext);
			  for(int i = 1; i < arguments.Length; i++) {
			  	result /= arguments[i].GetValue(scontext, ccontext);
			  }
				break;
			case Type.Exponent: 
			  result = arguments[0].GetValue(scontext, ccontext);
				if(arguments.Length == 1) {
					result = result * result;
				} else {
			  	for(int i = 1; i < arguments.Length; i++) {
			  		result = Mathf.Pow(result, arguments[i].GetValue(scontext, ccontext));
			  	}
				}
				break;
			case Type.Root: 
			  result = arguments[0].GetValue(scontext, ccontext);
				if(arguments.Length == 1) {
					result = Mathf.Sqrt(result);
				} else {
			  	for(int i = 1; i < arguments.Length; i++) {
			  		result = Mathf.Pow(result, 1.0f/arguments[i].GetValue(scontext, ccontext));
			  	}
				}
				break;
			case Type.RandomRange: {
				float low=0, high=1;
				if(arguments.Length >= 1) {
					low = arguments[0].GetValue(scontext, ccontext);
				}
				if(arguments.Length >= 2) {
					high = arguments[1].GetValue(scontext, ccontext);
				}
				result = Random.Range(low, high);
				break;
			}
			case Type.ClampRange: {
				float r = arguments[0].GetValue(scontext, ccontext);
				float low=0, high=1;
				if(arguments.Length >= 2) {
					low = arguments[1].GetValue(scontext, ccontext);
				}
				if(arguments.Length >= 3) {
					high = arguments[2].GetValue(scontext, ccontext);
				}
				result = Mathf.Clamp(r, low, high);
				break;
			}
		}
		return result;
	}	
}