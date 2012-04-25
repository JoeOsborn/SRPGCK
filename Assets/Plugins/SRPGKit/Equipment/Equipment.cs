using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("SRPGCK/Item/Equipment")]
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
	[System.NonSerialized]
	public Item baseItem;
	
	public void Start() {
		for(int i = 0; i < equipmentSlots.Length; i++) {
			equipmentSlots[i] = equipmentSlots[i].NormalizeName();
		}
		FindWielder();
	}
	
	void FindWielder() {
		if(wielder == null && transform.parent != null) {
			Transform t = transform.parent;
			while(t != null && wielder == null) {
				wielder = t.GetComponent<Character>();
				if(wielder == null) { t = t.parent; }
			}
			InstantiatedItem iit = GetComponent<InstantiatedItem>();
			baseItem = iit != null ? iit.item : null;
			if(wielder == null) { Debug.LogError("No wielder"); }
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

	public bool HasOwnParam(string pname) {
		MakeParametersIfNecessary();
		return runtimeParameters.ContainsKey(pname);
	}
	
	public bool HasParam(string pname) {
		return HasOwnParam(pname) || (baseItem != null && baseItem.HasParam(pname));
	}
	
	public Formulae fdb { get {
		if(wielder != null) { return wielder.fdb; }
		return Formulae.DefaultFormulae;
	} }
	
	public float GetParam(string pname, SkillDef scontext=null, float fallback=float.NaN) {
		MakeParametersIfNecessary();
		FindWielder();
		if(!HasOwnParam(pname)) {
			if(baseItem != null) {
				return baseItem.GetParam(pname, scontext, fallback);
			}
			if(float.IsNaN(fallback)) {
				Debug.LogError("No fallback for missing equipment param "+pname);
			}
			return fallback;
		}
		return runtimeParameters[pname].GetValue(fdb, scontext, null, null, this);
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