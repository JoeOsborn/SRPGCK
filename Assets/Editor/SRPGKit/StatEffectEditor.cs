using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(StatEffect))]
public class StatEffectEditor : SRPGCKEditor {
  [MenuItem("SRPGCK/Create stat effect", false, 23)]
  public static StatEffect CreateStatEffect()
  {
		return ScriptableObjectUtility.CreateAsset<StatEffect>();
  }

	StatEffect s;
	public override void OnEnable() {
		//FIXME: fdb = What exactly?
		//standalone stat effects don't have owners in
		//any meaningful data-lookup sense!
		base.OnEnable();
		s = target as StatEffect;
		name = "StatEffect";
	}

	public override void OnSRPGCKInspectorGUI () {
		s = EditorGUIExt.StatEffectField(s, StatEffectContext.Any, "stateffect."+s.GetInstanceID(), formulaOptions, lastFocusedControl, -1);
	}
}
