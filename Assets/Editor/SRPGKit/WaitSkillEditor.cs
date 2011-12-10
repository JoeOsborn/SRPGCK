using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(WaitSkill))]
public class WaitSkillEditor : SRPGCKEditor {
	protected WaitSkill ws;
  public override void OnEnable() {
		base.OnEnable();
		name = "WaitSkill";
		ws = target as WaitSkill;
	}
	
	public override void OnSRPGCKInspectorGUI () {
		ws.skillName = EditorGUILayout.TextField("Name", ws.skillName).NormalizeName();
		ws.skillGroup = EditorGUILayout.TextField("Group", ws.skillGroup).NormalizeName();
		ws.skillSorting = EditorGUILayout.IntField("Sorting", ws.skillSorting);
		ws.supportKeyboard = EditorGUILayout.Toggle("Support Keyboard", ws.supportKeyboard);
		ws.supportMouse = EditorGUILayout.Toggle("Support Mouse", ws.supportMouse);
		ws.requireConfirmation = EditorGUILayout.Toggle("Require Confirmation", ws.requireConfirmation);
		ws.waitArrows = EditorGUILayout.ObjectField("Wait Arrow Prefab", ws.waitArrows, typeof(GameObject), false) as GameObject;
	}
}