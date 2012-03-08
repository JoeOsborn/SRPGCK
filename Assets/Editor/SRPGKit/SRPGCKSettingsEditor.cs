using UnityEngine;
using System.Linq;
using System.Collections.Generic;

using UnityEditor;

public class SRPGCKSettingsEditor : SRPGCKEditor {
  [MenuItem("SRPGCK/Create global settings", false, 0)]
  public static SRPGCKSettings CreateSettings()
  {
    SRPGCKSettings asset = ScriptableObject.CreateInstance<SRPGCKSettings>();
    AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath("Assets/Resources/SRPGCKSettings.asset"));
    AssetDatabase.SaveAssets();
		EditorUtility.FocusProjectWindow();
    Selection.activeObject = asset;
		return asset;
  }
	SRPGCKSettings settings;
	public override void OnEnable() {
		settings = target as SRPGCKSettings;
		fdb = settings.defaultFormulae;
		base.OnEnable();
	}
	public override void OnSRPGCKInspectorGUI () {
	}
}
