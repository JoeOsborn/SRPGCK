using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(WaitSkill))]
public class WaitSkillEditor : SkillEditor {
	protected override SkillDef MakeEmptySkillDef() {
		return ScriptableObject.CreateInstance<WaitSkillDef>();
	}
	protected override void ConvertSkill() {
		EnsurePath("Assets/SRPGCK Data/Skills/Wait");
		WaitSkillDef def = ScriptableObjectUtility.CreateAsset<WaitSkillDef>(
			null,//s.skillName,
			"Assets/SRPGCK Data/Skills/Wait",
			false
		);
		CopyFieldsTo<WaitSkill, WaitSkillDef>(s as WaitSkill, def);
		s.def = def;
	}
}