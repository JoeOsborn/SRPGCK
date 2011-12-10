using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(Equipment))]

public class EquipmentEditor : SRPGCKEditor {	
	bool showSlots=true;
	bool showCategories=true;
	bool showParameters=true;
	bool showPassiveEffects=true;
	bool showStatusEffects=true;
	
	Equipment e;
  public override void OnEnable() {
		base.OnEnable();
		name = "Equipment";
		e = target as Equipment;
	}
	
	public override void OnSRPGCKInspectorGUI () {		
		e.equipmentName = EditorGUILayout.TextField("Name", e.equipmentName).NormalizeName();
		float halfWidth = Screen.width/2;
		EditorGUILayout.BeginHorizontal(GUILayout.Width(Screen.width-32));
		e.equipmentSlots = EditorGUIExt.ArrayFoldout("Slots", e.equipmentSlots, ref showSlots, false, halfWidth-16, "body");
		e.equipmentCategories = EditorGUIExt.ArrayFoldout("Categories", e.equipmentCategories, ref showCategories, false, halfWidth-16, "armor");
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();

		e.parameters = EditorGUIExt.ParameterFoldout("Parameter", e.parameters, formulaOptions, lastFocusedControl, ref showParameters);
		
		EditorGUILayout.Space();

		e.passiveEffects = EditorGUIExt.StatEffectFoldout("Passive Effect", e.passiveEffects, StatEffectContext.Normal, formulaOptions, lastFocusedControl, ref showPassiveEffects);
		
		EditorGUILayout.Space();
		
		e.statusEffectPrefabs = EditorGUIExt.ObjectArrayFoldout<StatusEffect>("Status Effect Prefabs", e.statusEffectPrefabs, ref showStatusEffects);		
	}
}