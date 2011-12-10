using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(StandardPickTileMoveSkill))]
//TODO: AttackSkillEditor and MoveSkillEditor.
public class StandardPickTileMoveSkillEditor : SkillEditor {
	protected StandardPickTileMoveSkill ms;
  public override void OnEnable() {
		base.OnEnable();
		name = "StandardPickTileMoveSkill";
		ms = target as StandardPickTileMoveSkill;
	}
	
	public override void OnSRPGCKInspectorGUI () {
		base.OnSRPGCKInspectorGUI();

		EditorGUILayout.BeginVertical();
		EditorGUILayout.Space();
		
		GUILayout.Label("Movement");
		EditorGUI.indentLevel++;
		EditorGUILayout.Space();
		//strategy
		ms.moveStrategy = EditorGUIExt.StrategyGUI(ms.moveStrategy, false);
		EditorGUILayout.Space();
		
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