using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

[AddComponentMenu("SRPGCK/Item/Item")]
public class Item : ScriptableObject {
	//editor only:
	public bool editorShowParameters=true;
	
	//normal fields:
	public string itemName;
	public float weight=0;
	public int stackSize=99;
	
	public Transform prefab;
	
	// TODO: tags
	public Formulae fdb;
	
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

	public float GetParam(string pname, SkillDef scontext=null, float fallback=float.NaN) {
		MakeParametersIfNecessary();
		if(!HasParam(pname)) { 
			if(float.IsNaN(fallback)) {
				Debug.LogError("No fallback for missing item param "+pname+" on "+this);
			}
			return fallback; 
		}
		return runtimeParameters[pname].GetValue(fdb, scontext, null, null, null, this);
	}
	
	public InstantiatedItem Instantiate(Vector3 where, Quaternion rot) {
		Transform t = Instantiate(prefab, where, rot) as Transform;
		InstantiatedItem iit = 
			t.GetComponent<InstantiatedItem>() ??
			t.gameObject.AddComponent<InstantiatedItem>();
		iit.item = this;
		return iit;
	}
}
