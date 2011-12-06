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
				runtimeFormulae.Add(formulaNames[i].NormalizeName(), formulae[i]);
			}
		}
	}
	
	public void Awake() {
		if(instance != null) { Destroy(this); }
		else { instance = this; }
	}

	public static float Lookup(string fname, Skill scontext=null, Character ccontext=null) {
		if(instance == null) { return -1; }
		if(ccontext != null) {
			if(ccontext.HasStat(fname)) {
				return ccontext.GetStat(fname);
			}
		}
		if(scontext != null) {
			if(scontext.character.HasStat(fname)) {
				return scontext.character.GetStat(fname);
			} else if(scontext.HasParam(fname)) {
				return scontext.GetParam(fname);
			}
		}
		return instance.LookupFormula(fname).GetValue(scontext, ccontext);
	}
	
	public Formula LookupFormula(string fname) {
		MakeFormulaeIfNecessary();
		return runtimeFormulae[fname];
	}
}