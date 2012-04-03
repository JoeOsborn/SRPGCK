using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

[CustomEditor(typeof(SkillIO))]
public class SkillIOEditor : SRPGCKEditor {
	[MenuItem("SRPGCK/Create skill io", false, 25)]
	public static SkillIO CreateSkillIO()
	{
		SkillIO sio = ScriptableObjectUtility.CreateAsset<SkillIO>(
			null,
			"Assets/SRPGCK Data/Skill IO/",
			true
		);
		return sio;
	}
	SkillIO io;

 	public override void OnEnable() {
		base.OnEnable();
		name = "Skill I/O";
		io = target as SkillIO;
	}

	public override void OnSRPGCKInspectorGUI() {
		//FIXME: implicit assumption: lockToGrid=true;
		//so, no invertOverlay, overlayType, drawOverlayRim, drawOverlayVolume, rotationSpeedXYF
		//FIXME: not showing probe for now
		io.overlayColor = EditorGUILayout.ColorField("Target Overlay", io.overlayColor);
		io.highlightColor = EditorGUILayout.ColorField("Effect Highlight", io.highlightColor);
		io.supportKeyboard = EditorGUILayout.Toggle("Support Keyboard", io.supportKeyboard);
		if(io.supportKeyboard) {
			io.keyboardMoveSpeed = EditorGUILayout.FloatField("Keyboard Move Speed", io.keyboardMoveSpeed);
			io.indicatorCycleLength = EditorGUILayout.FloatField("Z Cycle Time", io.indicatorCycleLength);
		}
		io.supportMouse = EditorGUILayout.Toggle("Support Mouse", io.supportMouse);
		io.requireConfirmation = EditorGUILayout.Toggle("Require Confirmation", io.requireConfirmation);
		io.pathMaterial = EditorGUILayout.ObjectField("Path Material", io.pathMaterial, typeof(Material), false) as Material;
		if(io.requireConfirmation) {
			io.performTemporaryStepsOnConfirmation = EditorGUILayout.Toggle("Preview Before Confirming", io.performTemporaryStepsOnConfirmation);
		}
		io.performTemporaryStepsImmediately = EditorGUILayout.Toggle("Preview Immediately", io.performTemporaryStepsImmediately);
	}
}
