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

	public override void OnSRPGCKInspectorGUI() {
		//treat poorly-defined as equivalent to null for UI purposes
		s.def = EditorGUIExt.PickAssetGUI<SkillDef>(
			"Skill Definition",
			s.def
		);
	}
}

