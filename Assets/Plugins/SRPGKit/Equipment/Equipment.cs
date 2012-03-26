using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("SRPGCK/Character/Equipment")]
public class Equipment : MonoBehaviour {
	[System.NonSerialized]
	public Character wielder;
	
	public string equipmentName;
	public string[] equipmentSlots;
	public int[] equippedSlots;

	public string[] equipmentCategories;
	
	public StatEffect[] passiveEffects;

	public List<Parameter> parameters;
	
	public StatusEffect[] statusEffectPrefabs;

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
			for(int i = 0; i < parameters.Count; i++) {
				runtimeParameters.Add(parameters[i].Name.NormalizeName(), parameters[i].Formula);
			}
		}
	}
	
	public bool HasParam(string pname) {
		MakeParametersIfNecessary();
		return runtimeParameters.ContainsKey(pname);
	}
	
	public Formulae fdb { get {
		if(wielder != null) { return wielder.fdb; }
		return Formulae.DefaultFormulae;
	} }
	
	public float GetParam(string pname) {
		MakeParametersIfNecessary();
		FindWielder();
		return runtimeParameters[pname].GetValue(fdb, null, null, null, this);
	}
	
	public void EquipOn(Character c, int[] slots) {
		wielder = c;
		equippedSlots = slots;
		transform.parent = c.transform;
		foreach(StatusEffect st in statusEffectPrefabs) {
			StatusEffect se = Instantiate(st) as StatusEffect;
			se.transform.parent = transform;
		}
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