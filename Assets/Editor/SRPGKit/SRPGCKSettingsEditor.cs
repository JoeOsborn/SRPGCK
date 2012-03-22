using UnityEngine;
using System.Linq;
using System.Collections.Generic;

using UnityEditor;

[CustomEditor(typeof(SRPGCKSettings))]
public class SRPGCKSettingsEditor : SRPGCKEditor {
  [MenuItem("SRPGCK/Create global settings", false, 0)]
  public static SRPGCKSettings CreateSettings() {
		SRPGCKSettings sas = ScriptableObjectUtility.CreateAsset<SRPGCKSettings>(
			"SRPGCKSettings",
			"Assets/Resources/",
			true
		);
		return sas;
  }
	SRPGCKSettings settings;
	public override void OnEnable() {
		settings = target as SRPGCKSettings;
		fdb = settings.defaultFormulae;
		base.OnEnable();
	}
	public override void OnSRPGCKInspectorGUI () {
		settings.defaultFormulae = EditorGUIExt.PickAssetGUI<Formulae>(
			"Default Formulae",
			settings.defaultFormulae
		);
		settings.defaultMoveIO = EditorGUIExt.PickAssetGUI<SkillIO>(
			"Default Move I/O",
			settings.defaultMoveIO
		);
		settings.defaultActionIO = EditorGUIExt.PickAssetGUI<SkillIO>(
			"Default Action I/O",
			settings.defaultActionIO
		);
	}
}
