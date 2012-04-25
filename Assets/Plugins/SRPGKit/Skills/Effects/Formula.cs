using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public enum FormulaType {
	Constant,
	Lookup, //variable, argument, formula, or stat
	ReactedEffectValue,
	//any number of arguments
	Add,
	Subtract,
	Multiply,
	Divide,
	Remainder,
	Exponent,
	Root,
	Mean,
	Min,
	Max,
	//must be 2 arguments
	RandomRange,
	//must be 3 arguments
	ClampRange,
	//must be 1 argument
	RoundDown,
	RoundUp,
	Round,
	AbsoluteValue,
	Negate,
	//2 arguments
	Equal,
	NotEqual,
	GreaterThan,
	GreaterThanOrEqual,
	LessThan,
	LessThanOrEqual,
	BranchIfNotZero,
	//any number of arguments
	Any,
	//2 arguments: if true, 0; else 1
	LookupSuccessful,
	//up to 8 arguments, of which any can be null (in which case this returns last arg or fails if last arg is null)
	//front, left, right, back, away, sides, towards, default
	BranchApplierSide,
	BranchAppliedSide,
	//even number of arguments: cases, then formulae
	BranchPDF,
	Undefined,
		//...
	SkillEffectValue,
	Or,
	And,
	Not,
	TargetIsNull,
	TargetIsNotNull,
	BranchCond,
	IntDivide,
	Trunc,
	NullFormula,
	BranchSwitch,
	LookupOrElse
}

public enum LookupType {
	Auto,
	//lookupReference is the skill param/character stat
	SkillParam,
	ActorStat,
	ActorEquipmentParam,
	ActorSkillParam,
	TargetStat,
	TargetEquipmentParam,
	TargetSkillParam,
	//lookupReference is the formula name
	NamedFormula,
	//lookupReference is the skill param
	ReactedSkillParam,
	//lookupReference is unused
	ReactedEffectType,
	//TODO: AppliedEffectType?
	//lookupReference is the status effect type
	ActorStatusEffect,
	TargetStatusEffect,
	//lookupReference is unused
	SkillEffectType,
	//lookupReference is the skill param/character stat
	ActorMountStat,
	ActorMounterStat,
	ActorMountEquipmentParam,
	ActorMounterEquipmentParam,
	ActorMountSkillParam,
	ActorMounterSkillParam,
	//lookupReference is the status effect type
	ActorMountStatusEffect,
	ActorMounterStatusEffect,
	//lookupReference is the skill param/character stat
	TargetMountStat,
	TargetMounterStat,
	TargetMountEquipmentParam,
	TargetMounterEquipmentParam,
	TargetMountSkillParam,
	TargetMounterSkillParam,
	//lookupReference is the status effect type
	TargetMountStatusEffect,
	TargetMounterStatusEffect,
	ItemParam,
	ReactedItemParam
}

public enum FormulaMergeMode {
	First,
	Last,
	Min,
	Max,
	Mean,
	Sum,
	Nth
}

public interface IFormulaElement {

}

[System.Serializable]
public class Formula : IFormulaElement {
	//only used in editor:
	public string text="";
	public string compilationError="";
	public bool editorIsNamedFormula=false;

	//normal vars from here on
	public string name;
	public FormulaType formulaType;

	//constant
	public float constantValue;

	//lookup
	public string lookupReference;
	public LookupType lookupType;
	 //if a lookup returns multiple results
	public FormulaMergeMode mergeMode;
	//for Nth merge mode
	public int mergeNth=0;
	//eq
	public string[] equipmentSlots, equipmentCategories;
	//effect lookup (e.g. "this reaction skill recovers MP equal to the amount used by the attacker")
	//"reacted" is in the names, but it's also used for SkillEffectType.
	public string[] searchReactedStatNames; //we ignore lookupRef in this case
	public StatChangeType[] searchReactedStatChanges;
	public string[] searchReactedEffectCategories;

	//everything else
	public List<Formula> arguments; //x+y+z or x*y*z or x^y (y default 2) or y√x (y default 2)

	//runtime
	[System.NonSerialized]
	public float lastValue=float.NaN;

	public Formula Clone() {
		Formula f = new Formula();
		f.CopyFrom(this);
		return f;
	}
	public void CopyFrom(Formula f) {
/*		Debug.Log("copy from "+f);*/
		if(NullFormula(f)) { return; }
		name = f.name;
		text = f.text;
		formulaType = f.formulaType;
		constantValue = f.constantValue;
		lookupReference = f.lookupReference;
		lookupType = f.lookupType;
		mergeMode = f.mergeMode;
		mergeNth = f.mergeNth;
		equipmentSlots = f.equipmentSlots;
		equipmentCategories = f.equipmentCategories;
		searchReactedStatNames = f.searchReactedStatNames;
		searchReactedStatChanges = f.searchReactedStatChanges;
		searchReactedEffectCategories = f.searchReactedEffectCategories;
		arguments = f.arguments;
	}
	public static Formula Null() {
		Formula f = new Formula();
		f.formulaType = FormulaType.NullFormula;
		f.text = "(null)";
		return f;
	}
	public static Formula Constant(float c) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Constant;
		f.constantValue = c;
		f.text = ""+c;
		return f;
	}
	public static Formula True() {
		Formula f = new Formula();
		f.formulaType = FormulaType.Constant;
		f.constantValue = 1;
		f.text = "true";
		return f;
	}
	public static Formula False() {
		Formula f = new Formula();
		f.formulaType = FormulaType.Constant;
		f.constantValue = 0;
		f.text = "false";
		return f;
	}

	public static Formula Lookup(string n, LookupType type=LookupType.Auto) {
		Formula f = new Formula();
		f.formulaType = FormulaType.Lookup;
		f.lookupReference = n;
		f.lookupType = type;
		f.text = n;
		switch(type) {
			case LookupType.Auto:
				break;
			case LookupType.SkillParam:
				f.text = "skill."+f.text;
				break;
			case LookupType.ActorStat:
				f.text = "c."+f.text;
				break;
			case LookupType.ActorSkillParam:
				f.text = "c.skill."+f.text;
				break;
			case LookupType.TargetStat:
				f.text = "t."+f.text;
				break;
			case LookupType.TargetSkillParam:
				f.text = "t.skill."+f.text;
				break;
			case LookupType.NamedFormula:
				f.text = "f."+f.text;
				break;
			case LookupType.ReactedSkillParam:
				f.text = "reacted-skill."+f.text;
				break;
			//faked:...
			case LookupType.ActorEquipmentParam:
				f.text = "c.equip."+f.text;
				break;
			case LookupType.TargetEquipmentParam:
				f.text = "t.equip."+f.text;
				break;
			case LookupType.ReactedEffectType:
				f.text = "effect."+f.text;
				break;
			//TODO: AppliedEffectType?
			case LookupType.ActorStatusEffect:
				f.text = "c.status."+f.text;
				break;
			case LookupType.TargetStatusEffect:
				f.text = "t.status."+f.text;
				break;
			case LookupType.ActorMountStat:
				f.text = "c.mount."+f.text;
				break;
			case LookupType.ActorMountSkillParam:
				f.text = "c.mount.skill."+f.text;
				break;
			case LookupType.ActorMountEquipmentParam:
				f.text = "c.mount.equip."+f.text;
				break;
			case LookupType.ActorMountStatusEffect:
				f.text = "c.mount.status."+f.text;
				break;
			case LookupType.ActorMounterStat:
				f.text = "c.mounter."+f.text;
				break;
			case LookupType.ActorMounterSkillParam:
				f.text = "c.mounter.skill."+f.text;
				break;
			case LookupType.ActorMounterEquipmentParam:
				f.text = "c.mounter.equip."+f.text;
				break;
			case LookupType.ActorMounterStatusEffect:
				f.text = "c.mounter.status."+f.text;
				break;
			case LookupType.TargetMountStat:
				f.text = "t.mount."+f.text;
				break;
			case LookupType.TargetMountSkillParam:
				f.text = "t.mount.skill."+f.text;
				break;
			case LookupType.TargetMountEquipmentParam:
				f.text = "t.mount.equip."+f.text;
				break;
			case LookupType.TargetMountStatusEffect:
				f.text = "t.mount.status."+f.text;
				break;
			case LookupType.TargetMounterStat:
				f.text = "t.mounter."+f.text;
				break;
			case LookupType.TargetMounterSkillParam:
				f.text = "t.mounter.skill."+f.text;
				break;
			case LookupType.TargetMounterEquipmentParam:
				f.text = "t.mounter.equip."+f.text;
				break;
			case LookupType.TargetMounterStatusEffect:
				f.text = "t.mounter.status."+f.text;
				break;
		}
		return f;
	}

	public float GetCharacterValue(Formulae fdb, Character ccontext) {
		return GetValue(fdb, null, ccontext);
	}

	bool firstTime = true;
	public float GetValue(
		Formulae fdb, 
		SkillDef scontext=null, 
		Character ccontext=null, 
		Character tcontext=null, 
		Equipment econtext=null, 
		Item icontext=null
	) {
		if(firstTime) {
			lookupReference = lookupReference == null ? "" : lookupReference.NormalizeName();
			firstTime = false;
		}
		// if(scontext != null && scontext.currentTargetCharacter != null) {
			// Debug.Log("get value from "+this);
		// }
		float result=float.NaN;
		switch(formulaType) {
			case FormulaType.Constant:
				result = constantValue;
				break;
			case FormulaType.Lookup:
				result = fdb.Lookup(lookupReference, lookupType, scontext, ccontext, tcontext, econtext, icontext, this);
				break;
			case FormulaType.ReactedEffectValue:
				if(scontext == null) {
					Debug.LogError("No skill context.");
					return float.NaN;
				}
				if(scontext.currentReactedEffect == null) {
					Debug.LogError("Skill context is reacting to no particular effect.");
					return float.NaN;
				}
				result = scontext.currentReactedEffect.value;
				break;
			case FormulaType.SkillEffectValue:
				if(scontext == null) {
					Debug.LogError("No skill context.");
					return float.NaN;
				}
				if(scontext.lastEffects == null || scontext.lastEffects.Count == 0) {
					Debug.LogError("Skill context has no prior effects.");
					return float.NaN;
				}
				result = scontext.lastEffects[scontext.lastEffects.Count-1].value;
				break;
			case FormulaType.Add:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				for(int i = 1; i < arguments.Count; i++) {
					result += arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				}
				break;
			case FormulaType.Subtract:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				for(int i = 1; i < arguments.Count; i++) {
					result -= arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				}
				break;
			case FormulaType.Multiply:
				if(arguments == null) {
					Debug.LogError("no args at all! "+name+":"+text);
				}
				if(arguments[0] == null) {
					if(arguments[1] != null) {
						Debug.Log("a1 was ok, it's "+arguments[1].name+":"+arguments[1].text+" "+arguments[1].formulaType+"."+arguments[1].lookupType+" => "+arguments[1].lookupReference);
					}
					Debug.LogError("nm "+name+" txt "+text+" args "+arguments.Count);
				}
			  result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
			  for(int i = 1; i < arguments.Count; i++) {
			  	result *= arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
			  }
				// Debug.Log("multiplied to "+result);
				break;
			case FormulaType.Divide:
			  result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
			  for(int i = 1; i < arguments.Count; i++) {
			  	result /= arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
			  }
				// Debug.Log("divided to "+result);
				break;
			case FormulaType.IntDivide:
			  result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
			  for(int i = 1; i < arguments.Count; i++) {
			  	result = (float)((int)(result/arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext)));
			  }
				result = (float)((int)result);
				// Debug.Log("int divided to "+result);
				break;
			case FormulaType.Trunc:
			  result = (float)((int)arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext));
				break;
			case FormulaType.And:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				for(int i = 1; i < arguments.Count; i++) {
					result = ((result != 0) && (arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) != 0)) ? 1 : 0;
				}
				break;
			case FormulaType.Or:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				for(int i = 1; i < arguments.Count; i++) {
					result = ((result != 0) || (arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) != 0)) ? 1 : 0;
				}
				break;
			case FormulaType.Not:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) != 0 ? 0 : 1;
				break;
			case FormulaType.Remainder:
			  result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
			  for(int i = 1; i < arguments.Count; i++) {
			  	result = result % arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
			  }
				break;
			case FormulaType.Exponent:
			  result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				if(arguments.Count == 1) {
					result = result * result;
				} else {
			  	for(int i = 1; i < arguments.Count; i++) {
			  		result = Mathf.Pow(result, arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext));
			  	}
				}
				break;
			case FormulaType.Root:
			  result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				if(arguments.Count == 1) {
					result = Mathf.Sqrt(result);
				} else {
			  	for(int i = 1; i < arguments.Count; i++) {
			  		result = Mathf.Pow(result, 1.0f/arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext));
			  	}
				}
				break;
			case FormulaType.Mean:
				result = arguments.Sum(a => a.GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext)) / arguments.Count();
				break;
			case FormulaType.Min:
				result = arguments.Min(a => a.GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext));
				break;
			case FormulaType.Max:
				result = arguments.Max(a => a.GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext));
				break;
			case FormulaType.RandomRange: {
				float low=0, high=1;
				if(arguments.Count >= 2) {
					low = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
					high = arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				} else if(arguments.Count == 1) {
					high = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				}
				result = Random.Range(low, high);
				break;
			}
			case FormulaType.ClampRange: {
				float r = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				float low=0, high=1;
				if(arguments.Count >= 2) {
					low = arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				}
				if(arguments.Count >= 3) {
					high = arguments[2].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				}
				result = Mathf.Clamp(r, low, high);
				break;
			}
			case FormulaType.RoundDown:
				result = Mathf.Floor(arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext));
				break;
			case FormulaType.RoundUp:
				result = Mathf.Ceil(arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext));
				break;
			case FormulaType.Round:
				result = Mathf.Round(arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext));
				break;
			case FormulaType.AbsoluteValue:
				result = Mathf.Abs(arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext));
				break;
			case FormulaType.Negate:
				result = -1 * arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				// Debug.Log("negated to "+result);
				break;
			case FormulaType.Equal:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) == arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) ? 1 : 0;
				break;
			case FormulaType.NotEqual:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) != arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) ? 1 : 0;
				break;
		  case FormulaType.GreaterThan:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) > arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) ? 1 : 0;
		  	break;
		  case FormulaType.GreaterThanOrEqual:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) >= arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) ? 1 : 0;
		  	break;
			case FormulaType.LessThan:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) < arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) ? 1 : 0;
				break;
			case FormulaType.LessThanOrEqual:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) <= arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) ? 1 : 0;
				break;
			case FormulaType.Any:
				result = arguments[Random.Range(0, arguments.Count)].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				break;
			case FormulaType.LookupSuccessful:
				result = fdb.CanLookup(lookupReference, lookupType, scontext, ccontext, null, econtext, icontext, this) ? 1 : 0;
				break;
			case FormulaType.LookupOrElse:
				if(fdb.CanLookup(lookupReference, lookupType, scontext, ccontext, null, econtext, icontext, this)) {
					result = fdb.Lookup(lookupReference, lookupType, scontext, ccontext, tcontext, econtext, icontext, this);
				} else {
					result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				}
				break;
			case FormulaType.BranchIfNotZero:
				result = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) != 0 ?
				 	arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) :
					arguments[2].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				break;
			case FormulaType.BranchApplierSide:
				result = FacingSwitch(fdb, StatEffectTarget.Applier, scontext, ccontext, tcontext, econtext, icontext);
				break;
			case FormulaType.BranchAppliedSide:
				result = FacingSwitch(fdb, StatEffectTarget.Applied, scontext, ccontext, tcontext, econtext, icontext);
				break;
			case FormulaType.BranchPDF: {
				result = float.NaN;
				float rval = Random.value;
				float val = 0;
				int halfLen = arguments.Count/2;
				for(int i = 0; i < halfLen; i++) {
					val += arguments[i].GetValue(
						fdb,
						scontext,
						ccontext,
						tcontext,
						econtext, 
						icontext
					);
					// Debug.Log("branch cond check "+val+" against "+rval);
					if(val >= rval) {
						result = arguments[i+halfLen].GetValue(
							fdb,
							scontext,
							ccontext,
							tcontext,
							econtext,
							icontext
						);
						// Debug.Log("got "+result);
						break;
					}
				}
				if(float.IsNaN(result)) {
					Debug.LogError("PDF adds up to less than 1");
				}
				break;
			}
			case FormulaType.BranchCond: {
				result = float.NaN;
				int halfLen = arguments.Count/2;
				for(int i = 0; i < halfLen; i++) {
					if(arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) != 0) {
						result = arguments[i+halfLen].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
						break;
					}
				}
				if(float.IsNaN(result)) {
					Debug.LogError("No cond branch applied");
				}
				break;
			}
			case FormulaType.BranchSwitch: {
				result = float.NaN;
				float val = arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
				int halfLen = (arguments.Count-1)/2;
				for(int i = 1; (i-1) < halfLen; i++) {
					if(!NullFormula(arguments[i]) &&
					   arguments[i].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext) == val) {
						result = arguments[i+halfLen].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
						break;
					}
				}
				if(float.IsNaN(result)) {
					for(int i = 1; (i-1) < halfLen; i++) {
						if(NullFormula(arguments[i]) && !NullFormula(arguments[i+halfLen])) {
							result = arguments[i+halfLen].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
							break;
						}
					}
				}
				if(float.IsNaN(result)) {
					Debug.LogError("No cond branch applied");
				}
				break;
			}
			case FormulaType.TargetIsNull:
				result = (scontext != null ? scontext.currentTargetCharacter == null : tcontext == null) ? 1 : 0;
				break;
			case FormulaType.TargetIsNotNull:
				result = (scontext != null ? scontext.currentTargetCharacter != null : tcontext != null) ? 1 : 0;
				break;
		}
		lastValue = result;
		return result;
	}

	enum CharacterPointing {
		Front,
		Back,
		Left,
		Right,
		Away,
		Towards
	};

	protected float FacingSwitch(
		Formulae fdb,
		StatEffectTarget target,
		SkillDef scontext,
		Character ccontext,
		Character tcontext,
		Equipment econtext,
		Item icontext
	) {
		if(scontext == null) {
			Debug.LogError("Relative facing not available for non-attack/reaction skill effects.");
			return float.NaN;
		}
		Character applier = scontext != null ?
			scontext.character : ccontext;
		Character applied = scontext != null ?
			scontext.currentTargetCharacter : tcontext;
		CharacterPointing pointing = CharacterPointing.Front;
		Character x = null, y = null;
		if(target == StatEffectTarget.Applied) {
			x = applier;
			y = applied;
		} else if(target == StatEffectTarget.Applier) {
			x = applied;
			y = applier;
		}
		Vector3 xp = x.TilePosition;
		Vector3 yp = y.TilePosition;
		//see if y is facing towards x at all
		float xAngle = SRPGUtil.WrapAngle(x.Facing);
		float yAngle = SRPGUtil.WrapAngle(y.Facing);
		float interAngle = Mathf.Atan2(yp.y-xp.y, yp.x-xp.x)*Mathf.Rad2Deg;
		float relativeYAngle = SRPGUtil.WrapAngle(yAngle - xAngle);
		bool towards = Mathf.Abs(Mathf.DeltaAngle(interAngle, xAngle)) < 45;
		//is theta(y,x) within 45 of yAngle?
		// Debug.Log("xang "+xAngle);
		// 		Debug.Log("yang "+yAngle);
		// 		Debug.Log("interang "+interAngle);
		// 		Debug.Log("towardsang "+Mathf.Abs(Mathf.DeltaAngle(xAngle, interAngle)));
		// 		Debug.Log("relY "+relativeYAngle);
		if(towards) {
			//next, get the quadrant
			//quadrant ~~ theta (target -> other)
			if(relativeYAngle >= 45 && relativeYAngle < 135) {
				pointing = CharacterPointing.Left;
			} else if(relativeYAngle >= 135 && relativeYAngle < 225) {
				pointing = CharacterPointing.Front;
			} else if(relativeYAngle >= 225 && relativeYAngle < 315) {
				pointing = CharacterPointing.Right;
			} else {
				pointing = CharacterPointing.Back;
			}
		} else {
			pointing = CharacterPointing.Away;
		}
		// Debug.Log("pt "+pointing);

		//order:
		//front, left, right, back, away, sides, towards, default
		//must have null entries
		if(arguments.Count != 8) {
			Debug.Log("Bad facing switch in skill "+(scontext != null ? scontext.skillName : "none"));
		}
		if(pointing == CharacterPointing.Front && NotNullFormula(arguments[0])) {
			//front
			//Debug.Log("ft");
			return arguments[0].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
		} else if(pointing == CharacterPointing.Left && NotNullFormula(arguments[1])) {
			//left
			//Debug.Log("lt");
			return arguments[1].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
		} else if(pointing == CharacterPointing.Right && NotNullFormula(arguments[2])) {
			//right
			//Debug.Log("rt");
			return arguments[2].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
		} else if(pointing == CharacterPointing.Back && NotNullFormula(arguments[3])) {
			//back
			//Debug.Log("bk");
			return arguments[3].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
		} else if(pointing == CharacterPointing.Away && NotNullFormula(arguments[4])) {
			//away
			//Debug.Log("away");
			return arguments[4].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
		} else if((pointing == CharacterPointing.Left || pointing == CharacterPointing.Right) && NotNullFormula(arguments[5])) {
			//sides
			// Debug.Log("sides");
			return arguments[5].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
		} else if((pointing != CharacterPointing.Away) && NotNullFormula(arguments[6])) {
			//towards
			// Debug.Log("twds");
			return arguments[6].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
		} else if(NotNullFormula(arguments[7])) {
			//default
			// Debug.Log("default");
			return arguments[7].GetValue(fdb, scontext, ccontext, tcontext, econtext, icontext);
		} else {
			Debug.LogError("No valid branch for pointing "+pointing+" in skill "+(scontext != null ? scontext.skillName : "none"));
			return float.NaN;
		}
	}

	public static bool NotNullFormula(Formula f) {
		return f != null && f.formulaType != FormulaType.NullFormula;
	}

	public static bool NullFormula(Formula f) {
		return !NotNullFormula(f);
	}

	public override string ToString() {
		// if(text != "" && text != null) { return text; }
		try {
			return FormulaToString();
		} catch(System.Exception e) {
			Debug.Log("broken formula "+e);
			return "[[broken formula "+formulaType+":"+lookupReference+"("+constantValue+")]]";
		}
	}
	public string FormulaToString() {
		switch(formulaType) {
			case FormulaType.Constant:
				return constantValue.ToString();
			case FormulaType.Lookup:
				return LookupToString();
			case FormulaType.ReactedEffectValue:
				return "reacted-skill.effect";
			case FormulaType.SkillEffectValue:
				return "effect";
			case FormulaType.Add:
				if(arguments.Count == 2) {
					return "("+arguments[0].ToString()+") + ("+arguments[1].ToString()+")";
				}
				return "sum("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.Subtract:
				if(arguments.Count == 2) {
					return "("+arguments[0].ToString()+") - ("+arguments[1].ToString()+")";
				}
				return "difference("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.Multiply:
				if(arguments.Count == 2) {
					return "("+arguments[0].ToString()+") * ("+arguments[1].ToString()+")";
				}
				return "product("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.Divide:
				if(arguments.Count == 2) {
					return "("+arguments[0].ToString()+") / ("+arguments[1].ToString()+")";
				}
				return "quotient("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.IntDivide:
				if(arguments.Count == 2) {
					return "("+arguments[0].ToString()+") div ("+arguments[1].ToString()+")";
				}
				return "iquotient("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.Trunc:
				return "trunc("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.And:
				if(arguments.Count == 2) {
					return "("+arguments[0].ToString()+") and ("+arguments[1].ToString()+")";
				}
				return "and("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.Or:
				if(arguments.Count == 2) {
					return "("+arguments[0].ToString()+") or ("+arguments[1].ToString()+")";
				}
				return "or("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.Not:
				return "not ("+arguments[0].ToString()+")";
			case FormulaType.Remainder:
				if(arguments.Count == 2) {
					return "("+arguments[0].ToString()+") % ("+arguments[1].ToString()+")";
				}
				return "rem("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.Exponent:
				if(arguments.Count == 2) {
					return "("+arguments[0].ToString()+") ^ ("+arguments[1].ToString()+")";
				}
				return "pow("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.Root:
				return "root("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.Mean:
				return "mean("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.Min:
				return "min("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.Max:
				return "max("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.RandomRange:
				return "random("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.ClampRange:
				return "clamp("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.RoundDown:
				return "floor("+arguments[0].ToString()+")";
			case FormulaType.RoundUp:
				return "ceil("+arguments[0].ToString()+")";
			case FormulaType.Round:
				return "round("+arguments[0].ToString()+")";
			case FormulaType.AbsoluteValue:
				return "abs("+arguments[0].ToString()+")";
			case FormulaType.Negate:
				return "(-("+arguments[0].ToString()+")"+")";
			case FormulaType.Equal:
				return "("+arguments[0].ToString()+" == "+arguments[1].ToString()+")";
			case FormulaType.NotEqual:
				return "("+arguments[0].ToString()+" != "+arguments[1].ToString()+")";
		  case FormulaType.GreaterThan:
				return "("+arguments[0].ToString()+" > "+arguments[1].ToString()+")";
		  case FormulaType.GreaterThanOrEqual:
				return "("+arguments[0].ToString()+" >= "+arguments[1].ToString()+")";
			case FormulaType.LessThan:
				return "("+arguments[0].ToString()+" < "+arguments[1].ToString()+")";
			case FormulaType.LessThanOrEqual:
				return "("+arguments[0].ToString()+" <= "+arguments[1].ToString()+")";
			case FormulaType.Any:
				return "any("+arguments.Select(a => a.ToString()).JoinStr(", ")+")";
			case FormulaType.LookupSuccessful:
				return "exists("+LookupToString()+")";
			case FormulaType.LookupOrElse:
				return "lookup("+LookupToString()+", "+arguments[0].ToString()+")";
			case FormulaType.BranchIfNotZero:
				return "(if ("+arguments[0].ToString()+"): "+arguments[1].ToString()+"; "+arguments[2].ToString()+")";
			case FormulaType.BranchApplierSide:
				return SideBranchToString("targeter-side");
			case FormulaType.BranchAppliedSide:
				return SideBranchToString("targeted-side");
			case FormulaType.BranchPDF: {
				string ret = "random {\n";
				int halfLen = arguments.Count/2;
				for(int i = 0; i < halfLen; i++) {
					ret += arguments[i].ToString()+": "+arguments[i+halfLen].ToString();
					if(i < halfLen - 1) { ret += ";\n"; }
				}
				ret += "\n}\n";
				return ret;
			}
			case FormulaType.BranchCond: {
				string ret = "cond {\n";
				int halfLen = arguments.Count/2;
				for(int i = 0; i < halfLen; i++) {
					ret += arguments[i].ToString()+": "+arguments[i+halfLen].ToString();
					if(i < halfLen - 1) { ret += ";\n"; }
				}
				ret += "\n}\n";
				return ret;
			}
			case FormulaType.BranchSwitch: {
				string ret = "switch ("+arguments[0].ToString()+") {\n";
				int halfLen = (arguments.Count-1)/2;
				for(int i = 1; (i-1) < halfLen; i++) {
					if(NullFormula(arguments[i])) {
						ret += "default: "+arguments[i+halfLen].ToString();
					} else {
						ret += arguments[i].ToString()+": "+arguments[i+halfLen].ToString();
					}
					if((i-1) < halfLen - 1) { ret += ";\n"; }
				}
				ret += "\n}\n";
				return ret;
			}
			case FormulaType.TargetIsNull:
				return "t.isNull";
			case FormulaType.TargetIsNotNull:
				return "t.isNotNull";
		}
		return "Unstringable formula of type "+formulaType;
	}
	public string SideBranchToString(string label) {
		string ret = label+" {\n";
		List<string> parts = new List<string>();
		if(NotNullFormula(arguments[0])) {
			//front
			parts.Add("front: "+arguments[0].ToString());
		}
		if(NotNullFormula(arguments[1])) {
			//left
			parts.Add("left: "+arguments[1].ToString());
		}
		if(NotNullFormula(arguments[2])) {
			//right
			parts.Add("right: "+arguments[2].ToString());
		}
		if(NotNullFormula(arguments[3])) {
			//back
			parts.Add("back: "+arguments[3].ToString());
		}
		if(NotNullFormula(arguments[4])) {
			//away
			parts.Add("away: "+arguments[4].ToString());
		}
		if(NotNullFormula(arguments[5])) {
			//sides
			parts.Add("sides: "+arguments[5].ToString());
		}
		if(NotNullFormula(arguments[6])) {
			//towards
			parts.Add("towards: "+arguments[6].ToString());
		}
		if(NotNullFormula(arguments[7])) {
			//default
			parts.Add("default: "+arguments[7].ToString());
		}
		ret += parts.JoinStr(";\n");
		ret += "\n}\n";
		return ret;
	}
	public string MergeModeToString() {
		switch(mergeMode) {
			case FormulaMergeMode.Nth:
				return "get "+mergeNth;
			case FormulaMergeMode.First:
				return "get first";
			case FormulaMergeMode.Last:
				return "get last";
			case FormulaMergeMode.Min:
				return "get min";
			case FormulaMergeMode.Max:
				return "get max";
			case FormulaMergeMode.Mean:
				return "get mean";
			case FormulaMergeMode.Sum:
				return "get sum";
		}
		return "";
	}
	public string LookupToString() {
		switch(lookupType) {
			case LookupType.Auto:
				return lookupReference;
			case LookupType.SkillParam:
				return "skill."+lookupReference;
			case LookupType.ActorStat:
				return "c."+lookupReference;
			case LookupType.ActorMountStat:
				return "c.mount."+lookupReference;
			case LookupType.ActorMounterStat:
				return "c.mounter."+lookupReference;
			case LookupType.ActorEquipmentParam:
				return "c.equip("+equipmentCategories.JoinStr(", ")+" "+(equipmentSlots.Length > 0 ? "in "+equipmentCategories.JoinStr(", ")+" ":"")+MergeModeToString()+")."+lookupReference;
			case LookupType.ActorMountEquipmentParam:
				return "c.mount.equip("+equipmentCategories.JoinStr(", ")+" "+(equipmentSlots.Length > 0 ? "in "+equipmentCategories.JoinStr(", ")+" ":"")+MergeModeToString()+")."+lookupReference;
			case LookupType.ActorMounterEquipmentParam:
				return "c.mounter.equip("+equipmentCategories.JoinStr(", ")+" "+(equipmentSlots.Length > 0 ? "in "+equipmentCategories.JoinStr(", ")+" ":"")+MergeModeToString()+")."+lookupReference;
			case LookupType.ActorStatusEffect:
				return "c.status."+lookupReference;
			case LookupType.ActorMountStatusEffect:
				return "c.mount.status."+lookupReference;
			case LookupType.ActorMounterStatusEffect:
				return "c.mounter.status."+lookupReference;
			case LookupType.ActorSkillParam:
				return "skill."+lookupReference;
			case LookupType.TargetStat:
				return "t."+lookupReference;
			case LookupType.TargetMountStat:
				return "t.mount."+lookupReference;
			case LookupType.TargetMounterStat:
				return "t.mounter."+lookupReference;
			case LookupType.TargetEquipmentParam:
				return "t.equip("+equipmentCategories.JoinStr(", ")+" "+(equipmentSlots.Length > 0 ? "in "+equipmentCategories.JoinStr(", ")+" ":"")+MergeModeToString()+")."+lookupReference;
			case LookupType.TargetMountEquipmentParam:
				return "t.mount.equip("+equipmentCategories.JoinStr(", ")+" "+(equipmentSlots.Length > 0 ? "in "+equipmentCategories.JoinStr(", ")+" ":"")+MergeModeToString()+")."+lookupReference;
			case LookupType.TargetMounterEquipmentParam:
				return "t.mounter.equip("+equipmentCategories.JoinStr(", ")+" "+(equipmentSlots.Length > 0 ? "in "+equipmentCategories.JoinStr(", ")+" ":"")+MergeModeToString()+")."+lookupReference;
			case LookupType.TargetStatusEffect:
				return "t.status."+lookupReference;
			case LookupType.TargetMountStatusEffect:
				return "t.mount.status."+lookupReference;
			case LookupType.TargetMounterStatusEffect:
				return "t.mounter.status."+lookupReference;
			case LookupType.TargetSkillParam:
				return "t.skill."+lookupReference;
			case LookupType.ActorMountSkillParam:
				return "c.mount.skill."+lookupReference;
			case LookupType.ActorMounterSkillParam:
				return "c.mounter.skill."+lookupReference;
			case LookupType.TargetMountSkillParam:
				return "t.mount.skill."+lookupReference;
			case LookupType.TargetMounterSkillParam:
				return "t.mounter.skill."+lookupReference;
			case LookupType.NamedFormula:
				return ""+lookupReference;
			case LookupType.ReactedSkillParam:
				return "reacted-skill."+lookupReference;
			case LookupType.ReactedEffectType:
				return "reacted-skill.effect("+searchReactedStatChanges.JoinStr(", ")+(searchReactedStatNames.Length > 0 ? " in "+searchReactedStatNames.JoinStr(", ")+" " : "")+(searchReactedEffectCategories.Length > 0 ? " by "+searchReactedEffectCategories.JoinStr(", ")+" " : "")+MergeModeToString()+")";
			case LookupType.SkillEffectType:
			return "effect("+searchReactedStatChanges.JoinStr(", ")+(searchReactedStatNames.Length > 0 ? " in "+searchReactedStatNames.JoinStr(", ")+" " : "")+(searchReactedEffectCategories.Length > 0 ? " by "+searchReactedEffectCategories.JoinStr(", ")+" " : "")+MergeModeToString()+")";
		}
		return "Unstringable lookup of type "+lookupType;
	}

}