using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(TargetSettings))]

public class TargetSettingsEditor : SRPGCKEditor {
  [MenuItem("SRPGCK/Create target settings", false, 21)]
  public static TargetSettings CreateTargetSettings()
  {
		return ScriptableObjectUtility.CreateAsset<TargetSettings>();
  }

	TargetSettings ts;
	public override void OnEnable() {
		//FIXME: fdb = What exactly?
		//standalone TargetSettings don't have owners in
		//any meaningful data-lookup sense!
		base.OnEnable();
		ts = target as TargetSettings;
		name = "Target Settings";
	}

	public override void OnSRPGCKInspectorGUI () {
		ts = EditorGUIExt.TargetSettingsGUI(null, ts, null, formulaOptions, lastFocusedControl, -1);
	}
}
