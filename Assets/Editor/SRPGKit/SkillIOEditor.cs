using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(SkillIO))]

public class SkillIOEditor : SRPGCKEditor {
  [MenuItem("SRPGCK/Create skill IO", false, 20)]
  public static SkillIO CreateSkillIO()
  {
		return ScriptableObjectUtility.CreateAsset<SkillIO>();
  }

	SkillIO io;
	public override void OnEnable() {
		//FIXME: fdb = What exactly?
		//standalone SkillIO don't have owners in
		//any meaningful data-lookup sense!
		base.OnEnable();
		io = target as SkillIO;
		name = "Target Settings";
	}

	public override void OnSRPGCKInspectorGUI () {
		io = EditorGUIExt.SkillIOGUI(null, io, null, formulaOptions, lastFocusedControl);
	}
}
