using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(WaitSkillDef))]
public class WaitSkillDefEditor : SkillDefEditor {
	protected WaitSkillDef ws;
  public override void OnEnable() {
		base.OnEnable();
		name = "WaitSkillDef";
		ws = target as WaitSkillDef;
	}

	public override void OnSRPGCKInspectorGUI () {
		CoreSkillGUI();
		EditorGUILayout.Space();
		ws.supportKeyboard = EditorGUILayout.Toggle("Support Keyboard", ws.supportKeyboard);
		ws.supportMouse = EditorGUILayout.Toggle("Support Mouse", ws.supportMouse);
		ws.requireConfirmation = EditorGUILayout.Toggle("Require Confirmation", ws.requireConfirmation);
		ws.waitArrows = EditorGUILayout.ObjectField("Wait Arrow Prefab", ws.waitArrows, typeof(GameObject), false) as GameObject;
	}
}
