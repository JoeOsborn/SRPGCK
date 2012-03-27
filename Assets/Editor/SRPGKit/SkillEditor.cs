using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

[CustomEditor(typeof(Skill))]
public class SkillEditor : SRPGCKEditor {
	protected Skill s;
  public override void OnEnable() {
		useFormulae = false;
		s = target as Skill;
		base.OnEnable();
		name = "Skill";
	}

	public override void OnSRPGCKInspectorGUI () {
		//treat poorly-defined as equivalent to null for UI purposes
		s.def = EditorGUIExt.PickAssetGUI<SkillDef>(
			"Skill Definition",
			s.def
		);

		if(s.def == null || !s.def.reallyDefined) {
			if(GUILayout.Button("Convert to Skill Definition")) {
				ConvertSkill();
				s.def.reallyDefined = true;
				EditorUtility.SetDirty(s.def);
				EditorUtility.SetDirty(s);
			}
		}
	}

	protected virtual SkillDef MakeEmptySkillDef() {
		return ScriptableObject.CreateInstance<SkillDef>();
	}

	protected virtual void ConvertSkill() {
		EnsurePath("Assets/SRPGCK Data/Skills/Generic");
		SkillDef def = ScriptableObjectUtility.CreateAsset<SkillDef>(
			s.skillName,
			"Assets/SRPGCK Data/Skills/Generic",
			false
		);
		CopyFieldsTo<Skill, SkillDef>(s, def);
		s.def = def;
		s.def.isEnabledF = Formula.True();
	}
}

