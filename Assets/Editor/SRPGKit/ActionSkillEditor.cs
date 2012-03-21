using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

[CustomEditor(typeof(ActionSkill))]
public class ActionSkillEditor : SkillEditor {
	ActionSkill atk;

 	public override void OnEnable() {
		base.OnEnable();
		name = "ActionSkill";
		atk = target as ActionSkill;
	}

	protected void TargetedSkillGUI() {
		atk.delay = EditorGUIExt.FormulaField("Scheduled Delay", atk.delay, atk.GetInstanceID()+"."+atk.name+".delay", formulaOptions, lastFocusedControl);
		if(atk.targetSettings == null) {
			atk.targetSettings = new TargetSettings[]{new TargetSettings()};
		}
		if((atk.multiTargetMode = (MultiTargetMode)EditorGUILayout.EnumPopup("Multi-Target Mode", atk.multiTargetMode)) != MultiTargetMode.Single) {
			if(atk.multiTargetMode == MultiTargetMode.Chain) {
				atk.maxWaypointDistanceF = EditorGUIExt.FormulaField("Max Waypoint Distance", atk.maxWaypointDistanceF, atk.GetInstanceID()+"."+atk.name+".targeting.maxWaypointDistance", formulaOptions, lastFocusedControl);
			}
			atk.waypointsAreIncremental = EditorGUILayout.Toggle("Instantly Apply Waypoints", atk.waypointsAreIncremental);
			atk.canCancelWaypoints = EditorGUILayout.Toggle("Cancellable Waypoints", atk.canCancelWaypoints);
			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			int arraySize = EditorGUILayout.IntField(atk.targetSettings.Length, GUILayout.Width(32));
			GUILayout.Label(" "+"Target"+(atk.targetSettings.Length == 1 ? "" : "s"));
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			var oldSettings = atk.targetSettings;
			if(arraySize != atk.targetSettings.Length) {
				TargetSettings[] newSettings = atk.targetSettings;
				Array.Resize(ref newSettings, arraySize);
				atk.targetSettings = newSettings;
			}
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();
	   	EditorGUIUtility.LookLikeControls();
			for(int i = 0; i < atk.targetSettings.Length; i++)
			{
		   	EditorGUIUtility.LookLikeControls();
				TargetSettings ts = i < oldSettings.Length ? oldSettings[i] : atk.targetSettings[i];
				if (ts == null) {
					atk.targetSettings[i] = new TargetSettings();
					ts = atk.targetSettings[i];
				}
				atk.targetSettings[i] = EditorGUIExt.TargetSettingsGUI("Target "+i, atk.targetSettings[i], atk, formulaOptions, lastFocusedControl, i);
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
	   	EditorGUIUtility.LookLikeControls();
		} else {
			atk.targetSettings[0] = EditorGUIExt.TargetSettingsGUI("Target", atk.targetSettings[0], atk, formulaOptions, lastFocusedControl, -1);
		}
	}

	protected void EffectSkillGUI() {
		if(atk.targetEffects != null && atk.targetEffects.Length > 1) {
			EditorGUILayout.HelpBox("Be sure that the hitType parameter is defined to provide a value from 0 to "+(atk.targetEffects.Length-1), (s.HasParam("hitType") ? MessageType.Info : MessageType.Error));
		}
		atk.applicationEffects = EditorGUIExt.StatEffectGroupGUI("Per-Application Effect", atk.applicationEffects, StatEffectContext.Action, ""+atk.GetInstanceID(), formulaOptions, lastFocusedControl);
		atk.targetEffects = EditorGUIExt.StatEffectGroupsGUI("Application Effect Group", atk.targetEffects, StatEffectContext.Action, ""+atk.GetInstanceID(), formulaOptions, lastFocusedControl);
	}

	public override void OnSRPGCKInspectorGUI () {
		BasicSkillGUI();
		EditorGUILayout.Space();
		atk.io = EditorGUIExt.SkillIOGUI("I/O", atk.io, atk, formulaOptions, lastFocusedControl);
		EditorGUILayout.Space();
		TargetedSkillGUI();
		EditorGUILayout.Space();
		EffectSkillGUI();
		EditorGUILayout.Space();
		ReactionSkillGUI();
		EditorGUILayout.Space();
	}
}