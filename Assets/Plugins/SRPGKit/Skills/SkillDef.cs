using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SkillDef : ScriptableObject {
	public string skillName;
	public string skillGroup = "";
	public int skillSorting = 0;

	public bool replacesSkill = false;
	public string replacedSkill = "";
	public int replacementPriority=0;

	public bool deactivatesOnApplication=true;

	public StatEffect[] passiveEffects;

	public List<Parameter> parameters;

	//reaction
	public bool reactionSkill=false;
	public string[] reactionTypesApplied, reactionTypesApplier;
	public StatChange[] reactionStatChangesApplied, reactionStatChangesApplier;
	//tile validation region (line/range/cone/etc)
	public Region reactionTargetRegion, reactionEffectRegion;
	public StatEffectGroup[] reactionEffects;
	public StatEffectGroup reactionApplicationEffects;

	//for overriding
	virtual public bool isPassive { get { return true; } }

	public bool reallyDefined=false;

	public bool HasParam(string pname) {
		if(parameters == null) { return false; }
		string npname = pname.NormalizeName();
		return parameters.Any(p => p.Name == npname);
	}
}