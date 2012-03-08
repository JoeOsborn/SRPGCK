using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(Region))]

public class RegionEditor : SRPGCKEditor {
  [MenuItem("SRPGCK/Create region", false, 22)]
  public static Region CreateRegion()
  {
		return ScriptableObjectUtility.CreateAsset<Region>();
  }

	Region r;
	public override void OnEnable() {
		//FIXME: fdb = What exactly?
		//standalone regions don't have owners in
		//any meaningful data-lookup sense!
		base.OnEnable();
		r = target as Region;
		name = "Region";
	}

	public override void OnSRPGCKInspectorGUI () {
		r = EditorGUIExt.RegionGUI(null, "region", r, formulaOptions, lastFocusedControl, Screen.width);
	}
}
