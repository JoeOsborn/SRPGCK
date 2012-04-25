using UnityEngine;
using System.Linq;
using System.Collections.Generic;

using UnityEditor;

public class Formulae : ScriptableObject {
	public List<Formula> formulae;

	Dictionary<string, Formula> runtimeFormulae;

	void MakeFormulaeIfNecessary() {
		if(runtimeFormulae == null) {
			runtimeFormulae = new Dictionary<string, Formula>();
			for(int i = 0; i < formulae.Count; i++) {
				formulae[i].name = formulae[i].name.NormalizeName();
				if(runtimeFormulae.ContainsKey(formulae[i].name)) {
					Debug.Log("Duplicate formula "+formulae[i].name);
				}
				runtimeFormulae[formulae[i].name] = formulae[i];
			}
		}
	}

	public static Formulae DefaultFormulae { get {
		//load up the asset
		SRPGCKSettings settings = SRPGCKSettings.Settings;
		if(settings.defaultFormulae == null) {
			Formulae fdb = Resources.Load("Formulae") as Formulae;
			if(fdb != null) { settings.defaultFormulae = fdb; }
			else { Debug.LogError("Please create a default formulae DB and assign it to the global SRPGCKSettings asset."); }
		}
		return settings.defaultFormulae;
	} }

	public void AddFormula(Formula f, string name) {
		MakeFormulaeIfNecessary();
		runtimeFormulae[name] = f;
		f.name = name;
		if(formulae.Contains(f)) { formulae.Remove(f); }
		formulae.Add(f);
	}
	public void RemoveFormula(int i) {
		formulae.RemoveAt(i);
	}
	public void RemoveFormula(string name) {
		MakeFormulaeIfNecessary();
		runtimeFormulae.Remove(name ?? "");
		for(int i = 0; i < formulae.Count; i++) {
			if(formulae[i].name == name) {
				formulae.RemoveAt(i);
				return;
			}
		}
	}

	protected float LookupEquipmentParamOn(
		string fname, LookupType type,
		Character ccontext, Formula f,
		SkillDef scontext
	) {
		if(ccontext != null) {
			var equips = ccontext.Equipment.Where(eq => eq.Matches(f.equipmentSlots, f.equipmentCategories) && eq.HasParam(fname));
			if(equips.Count() == 0) {
				Debug.LogError("No equipment with param "+fname);
				return float.NaN;
			}
			var results = equips.Select(eq => eq.GetParam(fname, scontext));
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
				case FormulaMergeMode.Nth:
					return results.ElementAt(f.mergeNth);
				default:
					Debug.LogError("Unrecognized merge mode "+f.mergeMode);
					return float.NaN;
			}
		}
		Debug.LogError("No ccontext "+ccontext+" given scontext "+scontext+"; Cannot find matching equipment to get param "+fname);
		return float.NaN;
	}

	protected bool CanLookupEquipmentParamOn(
		string fname, LookupType type,
		Character ccontext, Formula f
	) {
		if(ccontext != null) {
			var equips = ccontext.Equipment.Where(eq =>
				eq.Matches(f.equipmentSlots, f.equipmentCategories) &&
				(fname == null || eq.HasParam(fname))
			);
			return equips.Count() > 0;
		}
		return false;
	}

	public bool CanLookup(
		string fname, LookupType type,
		SkillDef scontext=null,
		Character ccontext=null,
		Character tcontext=null,
		Equipment econtext=null,
		Item icontext=null,
		Formula f=null
	) {
		switch(type) {
			case LookupType.Auto:
				return (icontext != null && icontext.HasParam(fname)) ||
							 (econtext != null && econtext.HasParam(fname)) ||
							 (scontext != null && scontext.HasParam(fname)) ||
							 (ccontext != null && ccontext.HasStat(fname)) ||
 							 (tcontext != null && tcontext.HasStat(fname));
			case LookupType.SkillParam:
				return scontext.HasParam(fname);
			case LookupType.ItemParam: {
				if(icontext == null && scontext != null) {
					icontext = scontext.InvolvedItem;
				}
				if(icontext == null && econtext != null) {
					icontext = econtext.baseItem;
				}
				return icontext != null && icontext.HasParam(fname);
			}
			case LookupType.ReactedItemParam: {
				icontext = scontext.currentReactedSkill.InvolvedItem;
				return icontext != null && icontext.HasParam(fname);
			}
			case LookupType.ActorStat:
				if(scontext != null) { return scontext.character.HasStat(fname); }
				if(econtext != null) { return econtext.wielder.HasStat(fname); }
				if(ccontext != null) { return ccontext.HasStat(fname); }
				return false;
			case LookupType.ActorMountStat: {
				Character m = null;
				if(scontext != null) { m = scontext.currentTargetCharacter.mountedCharacter; }
				if(econtext != null) { m = econtext.wielder.mountedCharacter; }
				if(tcontext != null) { m = tcontext.mountedCharacter; }
				if(m != null) { return m.HasStat(fname); }
				// Debug.Log("can lookup "+fname+" on mount "+m+" ? "+(m != null && m.HasStat(fname)));
				return false;
			}
			case LookupType.ActorMounterStat: {
				Character m = null;
				if(scontext != null) { m = scontext.currentTargetCharacter.mountingCharacter; }
				if(econtext != null) { m = econtext.wielder.mountingCharacter; }
				if(tcontext != null) { m = tcontext.mountingCharacter; }
				if(m != null) { return m.HasStat(fname); }
				return false;
			}
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
			case LookupType.ActorMountEquipmentParam:
				if(scontext != null) {
					ccontext = scontext.character.mountedCharacter;
				} else if(ccontext == null && econtext != null) {
					if(econtext.Matches(f.equipmentSlots, f.equipmentCategories)) {
						return econtext.HasParam(fname);
					} else {
						ccontext = econtext.wielder.mountedCharacter;
					}
				}
				return CanLookupEquipmentParamOn(fname, type, ccontext, f);
			case LookupType.ActorMounterEquipmentParam:
				if(scontext != null) {
					ccontext = scontext.character.mountingCharacter;
				} else if(ccontext == null && econtext != null) {
					if(econtext.Matches(f.equipmentSlots, f.equipmentCategories)) {
						return econtext.HasParam(fname);
					} else {
						ccontext = econtext.wielder.mountingCharacter;
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
			case LookupType.ActorMountStatusEffect: {
				Character m = null;
				if(scontext != null) { m = scontext.character.mountedCharacter; }
				if(econtext != null) { m = econtext.wielder.mountedCharacter; }
				if(ccontext != null) { m = ccontext.mountedCharacter; }
				return m != null && m.HasStatusEffect(fname);
			}
			case LookupType.ActorMounterStatusEffect: {
				Character m = null;
				if(scontext != null) { m = scontext.character.mountingCharacter; }
				if(econtext != null) { m = econtext.wielder.mountingCharacter; }
				if(ccontext != null) { m = ccontext.mountingCharacter; }
				return m != null && m.HasStatusEffect(fname);
			}
			case LookupType.TargetStat:
				if(scontext != null) { return scontext.currentTargetCharacter.HasStat(fname); }
				if(tcontext != null) { return tcontext.HasStat(fname); }
				return false;
			case LookupType.TargetMountStat: {
				Character m = null;
				if(scontext != null) { m = scontext.currentTargetCharacter.mountedCharacter; }
				if(tcontext != null) { m = tcontext.mountedCharacter; }
				if(m != null) { return m.HasStat(fname); }
				return false;
			}
			case LookupType.TargetMounterStat: {
				Character m = null;
				if(scontext != null) { m = scontext.currentTargetCharacter.mountingCharacter; }
				if(tcontext != null) { m = tcontext.mountingCharacter; }
				if(m != null) { return m.HasStat(fname); }
				return false;
			}
			case LookupType.TargetStatusEffect:
				if(scontext != null) { return scontext.currentTargetCharacter.HasStatusEffect(fname); }
				if(tcontext != null) { return tcontext.HasStatusEffect(fname); }
				return false;
			case LookupType.TargetMountStatusEffect: {
				Character m = null;
				if(scontext != null) { m = scontext.currentTargetCharacter.mountedCharacter; }
				if(tcontext != null) { m = tcontext.mountedCharacter; }
				return m != null && m.HasStatusEffect(fname);
			}
			case LookupType.TargetMounterStatusEffect: {
				Character m = null;
				if(scontext != null) { m = scontext.currentTargetCharacter.mountingCharacter; }
				if(tcontext != null) { m = tcontext.mountingCharacter; }
				return m != null && m.HasStatusEffect(fname);
			}
			case LookupType.TargetEquipmentParam:
				if(scontext != null) {
					ccontext = scontext.currentTargetCharacter;
				} else if(tcontext != null) {
					ccontext = tcontext;
				} else {
					ccontext = null;
				}
				return CanLookupEquipmentParamOn(fname, type, ccontext, f);
			case LookupType.TargetMountEquipmentParam:
				if(scontext != null) {
					ccontext = scontext.currentTargetCharacter.mountedCharacter;
				} else if(tcontext != null) {
					ccontext = tcontext.mountedCharacter;
				} else {
					ccontext = null;
				}
				return CanLookupEquipmentParamOn(fname, type, ccontext, f);
			case LookupType.TargetMounterEquipmentParam:
				if(scontext != null) {
					ccontext = scontext.currentTargetCharacter.mountingCharacter;
				} else if(tcontext != null) {
					ccontext = tcontext.mountingCharacter;
				} else {
					ccontext = null;
				}
				return CanLookupEquipmentParamOn(fname, type, ccontext, f);
			case LookupType.TargetSkillParam:
			case LookupType.ActorMountSkillParam:
			case LookupType.ActorMounterSkillParam:
			case LookupType.TargetMountSkillParam:
			case LookupType.TargetMounterSkillParam:
				return false;
			case LookupType.NamedFormula:
				return HasFormula(fname);
			case LookupType.ReactedSkillParam:
				if(scontext != null) {
					return true;
				}
				return false;
			case LookupType.ReactedEffectType:
				if(scontext != null) {
					string[] fnames = f.searchReactedStatNames;
					return scontext.currentReactedSkill.lastEffects.
						Where(fx => fx.Matches(fnames, f.searchReactedStatChanges, f.searchReactedEffectCategories)).
						Count() > 0;
				}
				return false;
			case LookupType.SkillEffectType:
				if(scontext != null) {
					string[] fnames = f.searchReactedStatNames;
					return scontext.lastEffects.
						Where(fx => fx.Matches(fnames, f.searchReactedStatChanges, f.searchReactedEffectCategories)).
						Count() > 0;
				}
				return false;
		}
		return false;
	}
	public float Lookup(
		string fname, LookupType type,
		SkillDef scontext=null,
		Character ccontext=null,
		Character tcontext=null,
		Equipment econtext=null,
		Item icontext=null,
		Formula f=null
	) {
		switch(type) {
			case LookupType.Auto: {
				float ret = 
					(icontext != null ? icontext.GetParam(fname) :
					(econtext != null ? econtext.GetParam(fname) :
					(scontext != null ? scontext.GetParam(fname) :
					(ccontext != null ? ccontext.GetStat(fname) :
					(tcontext != null ? tcontext.GetStat(fname) :
					(HasFormula(fname) ? LookupFormula(fname).GetValue(this, scontext, ccontext, tcontext, econtext, icontext) : float.NaN))))));
				if(float.IsNaN(ret)) {
					Debug.LogError("auto lookup failed for "+fname);
				}
				return ret;
			}
			case LookupType.SkillParam:
				return scontext.GetParam(fname);
			case LookupType.ItemParam: {
				if(icontext == null && scontext != null) {
					icontext = scontext.InvolvedItem;
				}
				if(icontext == null && econtext != null) {
					icontext = econtext.baseItem;
				}
				return icontext.GetParam(fname, scontext);
			}
			case LookupType.ReactedItemParam: {
				icontext = scontext.currentReactedSkill.InvolvedItem;
				return icontext.GetParam(fname, scontext);
			}
			case LookupType.ActorStat:
				if(scontext != null) { return scontext.character.GetStat(fname); }
				if(econtext != null) { return econtext.wielder.GetStat(fname); }
				if(ccontext != null) { return ccontext.GetStat(fname); }
				Debug.LogError("Cannot find actor stat "+fname);
				return float.NaN;
			case LookupType.ActorMountStat: {
				Character m = null;
				if(scontext != null) { m = scontext.character.mountedCharacter; }
				if(econtext != null) { m = econtext.wielder.mountedCharacter; }
				if(ccontext != null) { m = ccontext.mountedCharacter; }
				// Debug.Log("lookup "+fname+" on mount "+m+" ? "+(m != null ? m.GetStat(fname) : 0));
				if(m != null) {
					return m.GetStat(fname);
				}
				Debug.LogError("Cannot find actor mount stat "+fname);
				return float.NaN;
			}
			case LookupType.ActorMounterStat: {
				Character m = null;
				if(scontext != null) { m = scontext.character.mountingCharacter; }
				if(econtext != null) { m = econtext.wielder.mountingCharacter; }
				if(ccontext != null) { m = ccontext.mountingCharacter; }
				if(m != null) {
					return m.GetStat(fname);
				}
				Debug.LogError("Cannot find actor mounter stat "+fname);
				return float.NaN;
			}
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
				return LookupEquipmentParamOn(fname, type, ccontext, f, scontext);
			case LookupType.ActorMountEquipmentParam:
				if(scontext != null) {
					ccontext = scontext.character.mountedCharacter;
				} else if(ccontext == null && econtext != null) {
					if(econtext.Matches(f.equipmentSlots, f.equipmentCategories)) {
						return econtext.GetParam(fname);
					} else {
						ccontext = econtext.wielder.mountedCharacter;
					}
				}
				return LookupEquipmentParamOn(fname, type, ccontext, f, scontext);
			case LookupType.ActorMounterEquipmentParam:
				if(scontext != null) {
					ccontext = scontext.character.mountingCharacter;
				} else if(ccontext == null && econtext != null) {
					if(econtext.Matches(f.equipmentSlots, f.equipmentCategories)) {
						return econtext.GetParam(fname);
					} else {
						ccontext = econtext.wielder.mountingCharacter;
					}
				}
				return LookupEquipmentParamOn(fname, type, ccontext, f, scontext);
			case LookupType.ActorStatusEffect:
			case LookupType.ActorMountStatusEffect:
			case LookupType.ActorMounterStatusEffect:
				Debug.LogError("lookup semantics not defined for own status effect "+fname);
				return float.NaN;
			case LookupType.ActorSkillParam:
				//TODO: look up skill by slot, name, type?
				if(scontext != null) { return scontext.GetParam(fname); }
				Debug.LogError("Cannot find skill param "+fname);
				return float.NaN;
			case LookupType.TargetStat:
				if(scontext != null) { return scontext.currentTargetCharacter.GetStat(fname); }
				if(tcontext != null) { return tcontext.GetStat(fname); }
				Debug.LogError("Cannot find target stat "+fname);
				return float.NaN;
			case LookupType.TargetMountStat: {
				Character m = null;
				if(scontext != null) { m = scontext.currentTargetCharacter.mountedCharacter; }
				if(tcontext != null) { m = tcontext.mountedCharacter; }
				if(m != null) { return m.GetStat(fname); }
				Debug.LogError("Cannot find target stat "+fname);
				return float.NaN;
			}
			case LookupType.TargetMounterStat: {
				Character m = null;
				if(scontext != null) { m = scontext.currentTargetCharacter.mountingCharacter; }
				if(tcontext != null) { m = tcontext.mountingCharacter; }
				if(m != null) { return m.GetStat(fname); }
				Debug.LogError("Cannot find target stat "+fname);
				return float.NaN;
			}
			case LookupType.TargetEquipmentParam:
				if(scontext != null) {
					ccontext = scontext.currentTargetCharacter;
				} else if(tcontext != null) {
					ccontext = tcontext;
				} else {
					ccontext = null;
				}
				return LookupEquipmentParamOn(fname, type, ccontext, f, scontext);
			case LookupType.TargetMountEquipmentParam:
				if(scontext != null) {
					ccontext = scontext.currentTargetCharacter.mountedCharacter;
				} else if(tcontext != null) {
					ccontext = tcontext.mountedCharacter;
				} else {
					ccontext = null;
				}
				return LookupEquipmentParamOn(fname, type, ccontext, f, scontext);
			case LookupType.TargetMounterEquipmentParam:
				if(scontext != null) {
					ccontext = scontext.currentTargetCharacter.mountingCharacter;
				} else if(tcontext != null) {
					ccontext = tcontext.mountingCharacter;
				} else {
					ccontext = null;
				}
				return LookupEquipmentParamOn(fname, type, ccontext, f, scontext);
			case LookupType.TargetStatusEffect:
			case LookupType.TargetMountStatusEffect:
			case LookupType.TargetMounterStatusEffect:
				Debug.LogError("lookup semantics not defined for target status effect "+fname);
				return float.NaN;
			case LookupType.TargetSkillParam:
			case LookupType.ActorMountSkillParam:
			case LookupType.ActorMounterSkillParam:
			case LookupType.TargetMountSkillParam:
			case LookupType.TargetMounterSkillParam:
			//TODO: look up skill by slot, name, type?
				Debug.LogError("Cannot find "+type+" "+fname);
				return float.NaN;
			case LookupType.NamedFormula:
				if(!HasFormula(fname)) {
					Debug.LogError("Missing formula "+fname);
					return float.NaN;
				}
				// Debug.Log("F:"+LookupFormula(fname));
				return LookupFormula(fname).GetValue(this, scontext, ccontext, tcontext, econtext, icontext);
			case LookupType.ReactedSkillParam:
				if(scontext != null) {
					return scontext.currentReactedSkill.GetParam(fname);
				}
				Debug.LogError("Cannot find reacted skill for "+fname);
				return float.NaN;
			case LookupType.ReactedEffectType:
				if(scontext != null) {
					//ignore lookupRef
					string[] fnames = f.searchReactedStatNames;
					var results = scontext.currentReactedSkill.lastEffects.
						Where(fx => fx.Matches(fnames, f.searchReactedStatChanges, f.searchReactedEffectCategories)).
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
						case FormulaMergeMode.Nth:
							return results.ElementAt(f.mergeNth);
					}
				} else {
					Debug.LogError("Skill effect lookups require a skill context.");
					return float.NaN;
				}
				Debug.LogError("Cannot find reacted effects for "+f);
				return float.NaN;
			case LookupType.SkillEffectType:
				if(scontext != null) {
					string[] fnames = f.searchReactedStatNames;
					//ignore lookupRef
					var results = scontext.lastEffects.
						Where(fx => fx.Matches(fnames, f.searchReactedStatChanges, f.searchReactedEffectCategories)).
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
						case FormulaMergeMode.Nth:
							Debug.Log("nth "+f.mergeNth+" is "+results.ElementAt(f.mergeNth));
							return results.ElementAt(f.mergeNth);
					}
				} else {
					Debug.LogError("Skill effect lookups require a skill context.");
					return float.NaN;
				}
				Debug.LogError("Cannot find effects for "+f);
				return float.NaN;
		}
		Debug.LogError("failed to look up "+type+" "+fname+" with context s:"+scontext+", c:"+ccontext+", e:"+econtext+", i:"+icontext+" and formula "+f);
		return float.NaN;
	}

	public bool HasFormula(string fname) {
		if(fname == null) { return false; }
		MakeFormulaeIfNecessary();
		return runtimeFormulae.ContainsKey(fname);
	}

	public Formula LookupFormula(string fname) {
		MakeFormulaeIfNecessary();
		if(!HasFormula(fname)) { return null; }
		return runtimeFormulae[fname];
	}
}