using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

//[CustomEditor(typeof(ActionSkill))]
//TODO: ActionSkillEditor and MoveSkillEditor.
public class ActionSkillEditor : SkillEditor {	
	ActionSkill atk;
 	public override void OnEnable() {
		base.OnEnable();
		name = "ActionSkill";
		atk = target as ActionSkill;
	}
	
	public override void OnSRPGCKInspectorGUI () {
		base.OnSRPGCKInspectorGUI();
		EditorGUILayout.Space();
		
		GUILayout.Label("Targeting IO");
		EditorGUI.indentLevel++;
		atk.supportKeyboard = EditorGUILayout.Toggle("Support Keyboard", atk.supportKeyboard);
		atk.supportMouse = EditorGUILayout.Toggle("Support Mouse", atk.supportMouse);
		atk.requireConfirmation = EditorGUILayout.Toggle("Require Confirmation", atk.requireConfirmation);
		atk.indicatorCycleLength = EditorGUILayout.FloatField("Z Cycle Time", atk.indicatorCycleLength);
		EditorGUI.indentLevel--;
		EditorGUILayout.Space();
		
		GUILayout.Label("Attack");
		EditorGUI.indentLevel++;
		atk.strategy = EditorGUIExt.StrategyGUI(atk.strategy);
		atk.targetEffects = EditorGUIExt.StatEffectGroupsGUI("Attack Effect Group", atk.targetEffects, StatEffectContext.Action, formulaOptions, lastFocusedControl);
		EditorGUI.indentLevel--;
		EditorGUILayout.Space();
		if(atk.targetEffects != null && atk.targetEffects.Length > 1) {
			GUI.enabled=false;
			bool priorWrap = EditorStyles.textField.wordWrap;
			Color priorColor = EditorStyles.textField.normal.textColor;
			EditorStyles.textField.wordWrap = true;
			if(!s.HasParam("hitType")) {
				EditorStyles.textField.normal.textColor = Color.red;	
			}
			EditorGUILayout.TextArea("Be sure that the hitType parameter is defined to provide a value from 0 to "+(atk.targetEffects.Length-1), GUILayout.Width(Screen.width-32));
			EditorStyles.textField.wordWrap = priorWrap;
			EditorStyles.textField.normal.textColor = priorColor;	
			GUI.enabled=true;
		}
		
		//be sure each reaction and target effect's reactableTypes and target are shown!
		
/*		EditorGUILayout.BeginHorizontal();
		EditorGUIExt.ArrayFoldout("Slots", e.equipmentSlots, ref showSlots, halfWidth);
		EditorGUIExt.ArrayFoldout("Categories", e.equipmentCategories, ref showCategories, halfWidth);
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();

		e.parameters = EditorGUIExt.ParameterFoldout("Parameter", e.parameters, formulaOptions, lastFocusedControl, ref showParameters);
		
		EditorGUILayout.Space();

		
		EditorGUILayout.Space();
		
		e.statusEffectPrefabs = EditorGUIExt.ObjectArrayFoldout<StatusEffect>("Status Effect Prefabs", e.statusEffectPrefabs, ref showStatusEffects);
*/
	}
}