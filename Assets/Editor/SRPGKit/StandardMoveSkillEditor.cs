using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

//TODO: uncomment once we expose all the functionality we need. [CustomEditor(typeof(StandardMoveSkill))]
public class StandardMoveSkillEditor : SkillEditor {
	protected StandardMoveSkill ms;
  public override void OnEnable() {
		base.OnEnable();
		name = "StandardMoveSkill";
		ms = target as StandardMoveSkill;
	}
	
	public override void OnSRPGCKInspectorGUI () {
		base.OnSRPGCKInspectorGUI();

		EditorGUILayout.BeginVertical();
		EditorGUILayout.Space();
		
		GUILayout.Label("Movement");
		EditorGUI.indentLevel++;
		EditorGUILayout.Space();
		//strategy
		ms.strategy = EditorGUIExt.StrategyGUI(ms.strategy, false);
		EditorGUILayout.Space();
		
		//TODO: put in path drawing stuff!
		
		GUILayout.Label("Move IO");
		ms.supportKeyboard = EditorGUILayout.Toggle("Support Keyboard", ms.supportKeyboard);
		ms.supportMouse = EditorGUILayout.Toggle("Support Mouse", ms.supportMouse);
		ms.requireConfirmation = EditorGUILayout.Toggle("Require Confirmation", ms.requireConfirmation);
		ms.indicatorCycleLength = EditorGUILayout.FloatField("Z Cycle Time", ms.indicatorCycleLength);
		EditorGUILayout.Space();

		//move animation
		GUILayout.Label("Animation");
		ms.animateTemporaryMovement = EditorGUILayout.Toggle("Animate Temporary Movement", ms.animateTemporaryMovement);
		ms.XYSpeed = EditorGUILayout.FloatField("XY Speed", ms.XYSpeed);
		ms.ZSpeedUp = EditorGUILayout.FloatField("Z Speed Up", ms.ZSpeedUp);
		ms.ZSpeedDown = EditorGUILayout.FloatField("Z Speed Down", ms.ZSpeedDown);
		
		EditorGUI.indentLevel--;
		EditorGUILayout.EndVertical();
	}
}