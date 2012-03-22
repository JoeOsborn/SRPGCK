using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(MoveSkill))]
public class MoveSkillEditor : ActionSkillEditor {
	protected override SkillDef MakeEmptySkillDef() {
		return ScriptableObject.CreateInstance<MoveSkillDef>();
	}
	protected override void ConvertSkill() {
		EnsurePath("Assets/SRPGCK Data/Skills/Move");
		MoveSkillDef def = ScriptableObjectUtility.CreateAsset<MoveSkillDef>(
			s.skillName,
			"Assets/SRPGCK Data/Skills/Move",
			false
		);
		CopyFieldsTo<MoveSkill, MoveSkillDef>(s as MoveSkill, def);
		s.def = def;
		(s.def as MoveSkillDef).io = SRPGCKSettings.Settings.defaultMoveIO;
	}
}