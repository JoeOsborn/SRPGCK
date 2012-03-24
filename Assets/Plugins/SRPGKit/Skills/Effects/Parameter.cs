using UnityEngine;

[System.Serializable]
public class Parameter {
	public string name;
	public Formula formula;

	public bool limitMinimum;
	public Formula minF;

	public bool limitMaximum;
	public Formula maxF;

	public string Name { get { return name; } set { name = value.NormalizeName(); } }
	public Formula Formula { get { return formula; } set { formula = value; } }
	public Parameter(string n, Formula f) {
		name = n;
		formula = f;
	}

	public float ConstrainCharacterValue(Formulae fdb, Character c, float amt, float prevAmt) {
		if(limitMinimum && amt <= prevAmt) {
			float limit = minF.GetCharacterValue(fdb, c);
			// if(Name != "ct" && Name != "facing") { Debug.Log("minlimit: "+amt+" from "+prevAmt+" to > "+limit); }
			if(amt < limit) { amt = Mathf.Min(prevAmt,limit); }
		}
		if(limitMaximum && amt >= prevAmt) {
			float limit = maxF.GetCharacterValue(fdb, c);
			// if(Name != "ct" && Name != "facing") { Debug.Log("maxlimit: "+amt+" from "+prevAmt+" to < "+limit); }
			if(amt > limit) { amt = Mathf.Max(prevAmt,limit); }
		}
		return amt;
	}

	public float GetCharacterValue(Formulae fdb, Character c) {
		return this.Formula.GetCharacterValue(fdb, c);
	}
	public float SetCharacterValue(Formulae fdb, Character c, float amt, bool constrain=true) {
		Formula f = this.Formula;
		if(f.formulaType == FormulaType.Constant) {
			float givenAmt = amt, nowAmt = f.constantValue;
			amt = constrain ? ConstrainCharacterValue(fdb, c, amt, nowAmt) : amt;
			f.constantValue = amt;
			// if(Name != "ct" && Name != "facing") { Debug.Log("set "+Name+" to "+amt+" given "+givenAmt+" prev "+nowAmt); }
			return f.constantValue;
		} else {
			Debug.LogError("Can't set value of non-constant base stat "+Name);
			return -1;
		}
	}
}