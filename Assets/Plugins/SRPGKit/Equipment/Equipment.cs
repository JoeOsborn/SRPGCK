using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Equipment : MonoBehaviour {
	[HideInInspector]
	[System.NonSerialized]
	public Character wielder;
	
	public string equipmentName;
	public string[] equipmentSlots;
	public int[] equippedSlots;

	public string[] equipmentCategories;
	
	public StatEffect[] passiveEffects;
	public List<string> parameterNames;
	public List<Formula> parameterFormulae;

	Dictionary<string, Formula> runtimeParameters;
	
	public void Start() {
		for(int i = 0; i < equipmentSlots.Length; i++) {
			equipmentSlots[i] = equipmentSlots[i].NormalizeName();
		}
		FindWielder();
	}
	
	void FindWielder() {
		if(wielder == null && transform.parent != null) {
			wielder = transform.parent.GetComponent<Character>();
			if(equippedSlots == null || equippedSlots.Length == 0) {
				wielder.Equip(this);
			}
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
	
	public void EquipOn(Character c, int[] slots) {
		wielder = c;
		equippedSlots = slots;
		transform.parent = c.transform;
	}
	
	public void Unequip() {
		wielder = null;
		Destroy(gameObject);
	}
	
	public bool Matches(string[] slots, string[] types) {
		bool slotsOK = slots == null || slots.Length == 0 || slots.Any(s => equipmentSlots.Contains(s));
		bool typesOK = types == null || types.Length == 0 || types.All(t => equipmentCategories.Contains(t));
		return slotsOK && typesOK;
	}
}