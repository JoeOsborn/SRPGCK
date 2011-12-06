using UnityEngine;
using System.Collections.Generic;

public class Equipment : MonoBehaviour {
	Character wielder;
	
	public string equipmentName;
	public string[] equipmentSlots;
	
	public StatEffect[] passiveEffects;
	public List<string> parameterNames;
	public List<Formula> parameterFormulae;

	Dictionary<string, Formula> runtimeParameters;
	
	void FindWielder() {
		if(wielder == null) {
			wielder = transform.parent.GetComponent<Character>();
		}
	}

	void MakeParametersIfNecessary() {
		if(runtimeParameters == null) {
			runtimeParameters = new Dictionary<string, Formula>();
			for(int i = 0; i < parameterNames.Count; i++) {
				runtimeParameters.Add(parameterNames[i].NormalizeName(), parameterFormulae[i]);
			}
		}
	}
	
	public bool HasParam(string pname) {
		MakeParametersIfNecessary();
		return runtimeParameters.ContainsKey(pname);
	}
	
	public float GetParam(string pname) {
		MakeParametersIfNecessary();
		FindWielder();
		return runtimeParameters[pname].GetValue(null, wielder, this);
	}
	
	public void EquipOn(Character c) {
		wielder = c;
		transform.parent = c.transform;
	}
	
	public void Unequip() {
		wielder = null;
		Destroy(gameObject);
	}
}