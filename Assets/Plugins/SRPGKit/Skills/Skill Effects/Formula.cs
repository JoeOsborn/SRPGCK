using UnityEngine;

[System.Serializable]
public class Formula {
	public enum Type {
		Constant,
		Lookup, //variable, argument, formula, or stat
		//everything else
		Add,
		Subtract,
		Multiply,
		Divide,
		Exponent,
		Root
	}
	public Type formulaType;
	
	//constant
	public float constantValue;
	
	//lookup
	public string lookupReference;
	
	//everything else
	public Formula[] arguments; //x+y+z or x*y*z or x^y (y default 2) or y√x (y default 2)
	
	public float GetValue(Skill context) {
		float result=-1;
		switch(formulaType) {
			case Type.Constant: 
				result = constantValue;
				break;
			case Type.Lookup: 
				result = Formulae.Lookup(lookupReference, context);
				break;
			case Type.Add: 
				result = arguments[0].GetValue(context);
				for(int i = 1; i < arguments.Length; i++) {
					result += arguments[i].GetValue(context);
				}
				break;
			case Type.Subtract: 
				result = arguments[0].GetValue(context);
				for(int i = 1; i < arguments.Length; i++) {
					result -= arguments[i].GetValue(context);
				}
				break;
			case Type.Multiply: 
			  result = arguments[0].GetValue(context);
			  for(int i = 1; i < arguments.Length; i++) {
			  	result *= arguments[i].GetValue(context);
			  }
				break;
			case Type.Divide: 
			  result = arguments[0].GetValue(context);
			  for(int i = 1; i < arguments.Length; i++) {
			  	result /= arguments[i].GetValue(context);
			  }
				break;
			case Type.Exponent: 
			  result = arguments[0].GetValue(context);
				if(arguments.Length == 1) {
					result = result * result;
				} else {
			  	for(int i = 1; i < arguments.Length; i++) {
			  		result = Mathf.Pow(result, arguments[i].GetValue(context));
			  	}
				}
				break;
			case Type.Root: 
			  result = arguments[0].GetValue(context);
				if(arguments.Length == 1) {
					result = Mathf.Sqrt(result);
				} else {
			  	for(int i = 1; i < arguments.Length; i++) {
			  		result = Mathf.Pow(result, 1.0f/arguments[i].GetValue(context));
			  	}
				}
				break;
		}
		return result;
	}	
}