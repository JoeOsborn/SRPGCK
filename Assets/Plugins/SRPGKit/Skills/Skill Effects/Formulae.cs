using UnityEngine;
using System.Linq;
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
	
	protected static float LookupEquipmentParamOn(
		string fname, LookupType type, 
		Character ccontext, Formula f
	) {
		if(ccontext != null) {
			var equips = ccontext.Equipment.Where(eq => eq.Matches(f.equipmentSlots, f.equipmentCategories) && eq.HasParam(fname));
			if(equips.Count() == 0) { 
				Debug.LogError("No equipment with param "+fname);
				return -1; 
			}
			var results = equips.Select(eq => eq.GetParam(fname));
			switch(f.mergeMode) {
				case FormulaMergeMode.Sum:
					return results.Sum();
				case FormulaMergeMode.Mean:
					return results.Average(x => x);
				case FormulaMergeMode.Min:
					return results.Min(x => x);
				case FormulaMergeMode.Max:
					return results.Max(x => x);
				case FormulaMergeMode.First:
					return results.First();
				case FormulaMergeMode.Last:
					return results.Last();
			}
		}
		Debug.LogError("Cannot find matching equipment to get param "+fname);
		return -1;
	}

	protected static bool CanLookupEquipmentParamOn(
		string fname, LookupType type, 
		Character ccontext, Formula f
	) {
		if(ccontext != null) {
			var equips = ccontext.Equipment.Where(eq => 
				eq.Matches(f.equipmentSlots, f.equipmentCategories) &&
				eq.HasParam(fname)
			);
			return equips.Count() > 0;
		}
		return false;
	}
	
	public static bool CanLookup(
		string fname, LookupType type, 
		Skill scontext=null, Character ccontext=null, Equipment econtext=null,
		Formula f=null
	) {
		if(instance == null) { return false; }
		switch(type) {
			case LookupType.Undefined:
				return false;
			case LookupType.SkillParam:
				return scontext.HasParam(fname);
			case LookupType.ActorStat:
				if(scontext != null) { return scontext.character.HasStat(fname); }
				if(econtext != null) { return econtext.wielder.HasStat(fname); }
				if(ccontext != null) { return ccontext.HasStat(fname); }
				return false;
			case LookupType.ActorEquipmentParam:
				if(scontext != null) { 
					ccontext = scontext.character; 
				} else if(ccontext == null && econtext != null) {
					if(econtext.Matches(f.equipmentSlots, f.equipmentCategories)) {
						return econtext.HasParam(fname); 
					} else { 
						ccontext = econtext.wielder; 
					}
				}
				return CanLookupEquipmentParamOn(fname, type, ccontext, f);
			case LookupType.ActorSkillParam:
				if(scontext != null) { return scontext.HasParam(fname); }
				return false;
			case LookupType.ActorStatusEffect:
				if(scontext != null) { return scontext.character.HasStatusEffect(fname); }
				if(econtext != null) { return econtext.wielder.HasStatusEffect(fname); }
				if(ccontext != null) { return ccontext.HasStatusEffect(fname); }
				return false;
			case LookupType.TargetStat:
				if(scontext != null) { return scontext.currentTarget.HasStat(fname); }
				return false;
			case LookupType.TargetStatusEffect:
				if(scontext != null) { return scontext.currentTarget.HasStatusEffect(fname); }
				return false;
			case LookupType.TargetEquipmentParam:
				if(scontext != null) { 
					ccontext = scontext.currentTarget;
				} else {
					ccontext = null;
				}
				return CanLookupEquipmentParamOn(fname, type, ccontext, f);
			case LookupType.TargetSkillParam:
				return false;
			case LookupType.NamedFormula:
				return instance.HasFormula(fname);
			case LookupType.ReactedSkillParam:
				if(scontext != null) {
					return true;
				}
				return false;
			case LookupType.ReactedEffectType:
				if(scontext != null) {
					return scontext.currentReactedSkill.lastEffects.
						Where(fx => fx.Matches(fname, f.reactedStatChange, f.reactableCategories)).
						Count() > 0;
				}
				return false;
		}
		return false;
	}
	public static float Lookup(
		string fname, LookupType type, 
		Skill scontext=null, Character ccontext=null, Equipment econtext=null,
		Formula f=null
	) {
		if(instance == null) { return -1; }
		switch(type) {
			case LookupType.Undefined:
				Debug.LogError("Undefined lookup type for "+fname+". This will never work!");
				return -1;
			case LookupType.SkillParam:
			//TODO: look up skill by slot, name, type?
				return scontext.GetParam(fname);
			case LookupType.ActorStat:
				if(scontext != null) { return scontext.character.GetStat(fname); }
				if(econtext != null) { return econtext.wielder.GetStat(fname); }
				if(ccontext != null) { return ccontext.GetStat(fname); }
				Debug.LogError("Cannot find actor stat "+fname);
				return -1;
			case LookupType.ActorEquipmentParam:
				if(scontext != null) { 
					ccontext = scontext.character; 
				} else if(ccontext == null && econtext != null) {
					if(econtext.Matches(f.equipmentSlots, f.equipmentCategories)) {
						return econtext.GetParam(fname); 
					} else { 
						ccontext = econtext.wielder; 
					}
				}
				return LookupEquipmentParamOn(fname, type, ccontext, f);
			case LookupType.ActorStatusEffect:
				Debug.LogError("lookup semantics not defined for own status effect "+fname);
				return -1;
			case LookupType.ActorSkillParam:
				if(scontext != null) { return scontext.GetParam(fname); }
				Debug.LogError("Cannot find skill param "+fname);
				return -1;
			case LookupType.TargetStat:
				if(scontext != null) { return scontext.currentTarget.GetStat(fname); }
/*				if(econtext != null) { return econtext.wielder.GetStat(fname); }*/
/*				if(ccontext != null) { return ccontext.GetStat(fname); }*/
				Debug.LogError("Cannot find target stat "+fname);
				return -1;
			case LookupType.TargetEquipmentParam:
				if(scontext != null) { 
					ccontext = scontext.currentTarget;
				} else {
					ccontext = null;
				}
				return LookupEquipmentParamOn(fname, type, ccontext, f);
			case LookupType.TargetStatusEffect:
				Debug.LogError("lookup semantics not defined for target status effect "+fname);
				return -1;
			case LookupType.TargetSkillParam:
			//TODO: look up skill by slot, name, type?
				Debug.LogError("Cannot find target skill param "+fname);
				return -1;
			case LookupType.NamedFormula:
				return instance.LookupFormula(fname).GetValue(scontext, ccontext, econtext);
			case LookupType.ReactedSkillParam:
				if(scontext != null) {
					return scontext.currentReactedSkill.GetParam(fname);
				}
				Debug.LogError("Cannot find reacted skill for "+fname);
				return -1;
			case LookupType.ReactedEffectType:
				if(scontext != null) {
					var results = scontext.currentReactedSkill.lastEffects.
						Where(fx => fx.Matches(fname, f.reactedStatChange, f.reactableCategories)).
						Select(fx => fx.value);
					switch(f.mergeMode) {
						case FormulaMergeMode.Sum:
							return results.Sum();
						case FormulaMergeMode.Mean:
							return results.Average(x => x);
						case FormulaMergeMode.Min:
							return results.Min(x => x);
						case FormulaMergeMode.Max:
							return results.Max(x => x);
						case FormulaMergeMode.First:
							return results.First();
						case FormulaMergeMode.Last:
							return results.Last();
					}
				}
				Debug.LogError("Cannot find reacted effects for "+fname);
				return -1;
		}
		Debug.LogError("failed to look up "+type+" "+fname+" with context s:"+scontext+", c:"+ccontext+", e:"+econtext+" and formula "+f);
		return -1;
	}

	public bool HasFormula(string fname) {
		MakeFormulaeIfNecessary();
		return runtimeFormulae.ContainsKey(fname);
	}
	
	public Formula LookupFormula(string fname) {
		MakeFormulaeIfNecessary();
		return runtimeFormulae[fname];
	}
}