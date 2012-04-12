using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

[AddComponentMenu("SRPGCK/Item/Item")]
public class Item : MonoBehaviour {
	Arbiter _arbiter;
	protected Arbiter arbiter { get {
		if(_arbiter == null) {
			Transform t = transform;
			while(t != null) {
				_arbiter = t.GetComponent<Arbiter>();
				if(_arbiter != null) { break; }
				t = t.parent;
			}
		}
		return _arbiter;
	} }
	public Formulae fdb { get {
		if(arbiter != null) { return arbiter.fdb; }
		return Formulae.DefaultFormulae;
	} }
	
	public List<Parameter> parameters;
	Dictionary<string, Formula> runtimeParameters;

	void MakeParametersIfNecessary() {
		if(runtimeParameters == null) {
			runtimeParameters = new Dictionary<string, Formula>();
			for(int i = 0; i < parameters.Count; i++) {
				runtimeParameters.Add(parameters[i].Name.NormalizeName(), parameters[i].Formula);
			}
		}
	}

	public bool HasParam(string pname) {
		MakeParametersIfNecessary();
		return runtimeParameters.ContainsKey(pname);
	}

	public float GetParam(string pname, SkillDef scontext=null) {
		MakeParametersIfNecessary();
		return runtimeParameters[pname].GetValue(fdb, scontext, null, null, null, this);
	}

}
