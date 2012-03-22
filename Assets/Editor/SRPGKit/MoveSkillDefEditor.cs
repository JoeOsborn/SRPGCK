using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(MoveSkillDef))]
public class MoveSkillDefEditor : ActionSkillDefEditor {
	protected MoveSkillDef ms;

	public override void OnEnable() {
		base.OnEnable();
		name = "MoveSkillDef";
		ms = target as MoveSkillDef;
	}

	public bool showAnimation = true;
	protected void MoveSkillGUI() {
		if((showAnimation = EditorGUILayout.Foldout(showAnimation, "Animation"))) {
			EditorGUI.indentLevel++;
			ms.XYSpeed = EditorGUILayout.FloatField("XY Speed", ms.XYSpeed);
			ms.ZSpeedUp = EditorGUILayout.FloatField("Z Speed Up", ms.ZSpeedUp);
			ms.ZSpeedDown = EditorGUILayout.FloatField("Z Speed Down", ms.ZSpeedDown);
			ms.animateTemporaryMovement = EditorGUILayout.Toggle("Animate Temporary Movement", ms.animateTemporaryMovement);
			EditorGUI.indentLevel--;
		}
	}

	public override void OnSRPGCKInspectorGUI () {
		//normal skill
		BasicSkillGUI();
		EditorGUILayout.Space();
		ms.io = EditorGUIExt.PickAssetGUI<SkillIO>("I/O", ms.io);
		EditorGUILayout.Space();
		//move skill stuff
		MoveSkillGUI();
		EditorGUILayout.Space();
		//io and targeting
		TargetedSkillGUI();
		EditorGUILayout.Space();
		//reaction skill
		ReactionSkillGUI();
	}
}