using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(ActionSkill))]
public class ActionSkillEditor : SkillEditor {
	protected override SkillDef MakeEmptySkillDef() {
		return ScriptableObject.CreateInstance<ActionSkillDef>();
	}
	protected override void ConvertSkill() {
		EnsurePath("Assets/SRPGCK Data/Skills/Action");
		ActionSkillDef def = ScriptableObjectUtility.CreateAsset<ActionSkillDef>(
			s.skillName,
			"Assets/SRPGCK Data/Skills/Action",
			false
		);
		CopyFieldsTo<ActionSkill, ActionSkillDef>(s as ActionSkill, def);
		s.def = def;
		(s.def as ActionSkillDef).io = SRPGCKSettings.Settings.defaultActionIO;
	}
}