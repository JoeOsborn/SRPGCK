using UnityEngine;
using System.Collections.Generic;

public class Formulae : MonoBehaviour {
	[HideInInspector]
	public static Formulae instance;
	
	public List<string> formulaNames;
	public List<Formula> formulae;
	
	Dictionary<string, Formula> runtimeFormulae;
	
	void MakeFormulaeIfNecessary() {
		if(runtimeFormulae == null) {
			runtimeFormulae = new Dictionary<string, Formula>();
			for(int i = 0; i < formulaNames.Count; i++) {
				runtimeFormulae.Add(formulaNames[i], formulae[i]);
			}
		}
	}
	
	public void Awake() {
		if(instance != null) { Destroy(this); }
		else { instance = this; }
	}

	public static float Lookup(string fname, Skill context) {
		if(instance == null) { return -1; }
		if(context.character.HasStat(fname)) {
			return context.character.GetStat(fname);
/*		} else if(context.HasParameter(fname)) {
			return context.GetParameter(fname);
*/		} else {
			return instance.LookupFormula(fname, context).GetValue(context);
		}
	}
	
	public Formula LookupFormula(string fname, Skill context) {
		MakeFormulaeIfNecessary();
		return runtimeFormulae[fname];
	}
}