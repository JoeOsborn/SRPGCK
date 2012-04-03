using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

[CustomEditor(typeof(ProxyActionSkillDef))]
public class ProxyActionSkillDefEditor : ActionSkillDefEditor {
	[MenuItem("SRPGCK/Create proxy-action skill", false, 23)]
	public static ProxyActionSkillDef CreateProxyActionSkillDef()
	{
		ProxyActionSkillDef sd = ScriptableObjectUtility.CreateAsset<ProxyActionSkillDef>(
			null,
			"Assets/SRPGCK Data/Skills/Action",
			true
		);
		sd.isEnabledF = Formula.True();
		sd.io = SRPGCKSettings.Settings.defaultActionIO;
		sd.reallyDefined = true;
		return sd;
	}
	ProxyActionSkillDef patk;

	protected MergeMode MergeChoiceGUI(string label, MergeMode mm) {
		return (MergeMode)EditorGUILayout.EnumPopup(label, mm);
	}

	protected MergeModeList MergeListChoiceGUI(string label, MergeModeList mm) {
		return (MergeModeList)EditorGUILayout.EnumPopup(label, mm);
	}

 	public override void OnEnable() {
		base.OnEnable();
		name = "ProxyActionSkillDef";
		patk = target as ProxyActionSkillDef;
	}
	protected override void TargetedSkillGUI() {
		if((patk.mergeTurnToFaceTarget = MergeChoiceGUI("Face Target", patk.mergeTurnToFaceTarget)) != MergeMode.UseOriginal) {
			atk.turnToFaceTarget = EditorGUILayout.Toggle("Face Target", atk.turnToFaceTarget);
		}
		if((patk.mergeDelay = MergeChoiceGUI("Scheduled Delay", patk.mergeDelay)) != MergeMode.UseOriginal) {
			atk.delay = EditorGUIExt.FormulaField("Scheduled Delay", atk.delay, atk.GetInstanceID()+"."+atk.name+".delay", formulaOptions, lastFocusedControl);
		}
		if((patk.mergeDelayedApplicationUsesOriginalPosition = MergeChoiceGUI("Trigger from Original Position", patk.mergeDelayedApplicationUsesOriginalPosition)) != MergeMode.UseOriginal) {
			atk.delayedApplicationUsesOriginalPosition = EditorGUILayout.Toggle("Trigger from Original Position", atk.delayedApplicationUsesOriginalPosition);
		}
		if((patk.mergeMultiTargetMode = MergeChoiceGUI("Multi-Target Mode", patk.mergeMultiTargetMode)) != MergeMode.UseOriginal) {
			atk.multiTargetMode = (MultiTargetMode)EditorGUILayout.EnumPopup("Multi-Target Mode", atk.multiTargetMode);
		}
		if((patk.mergeMaxWaypointDistanceF = MergeChoiceGUI("Max Waypoint Distance", patk.mergeMaxWaypointDistanceF)) != MergeMode.UseOriginal) {
			atk.maxWaypointDistanceF = EditorGUIExt.FormulaField("Max Waypoint Distance", atk.maxWaypointDistanceF, atk.GetInstanceID()+"."+atk.name+".targeting.maxWaypointDistance", formulaOptions, lastFocusedControl);
		}
		if((patk.mergeWaypointsAreIncremental = MergeChoiceGUI("Instantly Apply Waypoints", patk.mergeWaypointsAreIncremental)) != MergeMode.UseOriginal) {
			atk.waypointsAreIncremental = EditorGUILayout.Toggle("Instantly Apply Waypoints", atk.waypointsAreIncremental);
		}
		if((patk.mergeCanCancelWaypoints = MergeChoiceGUI("Cancellable Waypoints", patk.mergeCanCancelWaypoints)) != MergeMode.UseOriginal) {
			atk.canCancelWaypoints = EditorGUILayout.Toggle("Cancellable Waypoints", atk.canCancelWaypoints);
		}
		if((patk.mergeTargetSettings = MergeChoiceGUI("Target Settings", patk.mergeTargetSettings)) != MergeMode.UseOriginal) {
			if(atk.targetSettings == null) {
				atk.targetSettings = new TargetSettings[]{new TargetSettings()};
			}
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
			for(int i = 0; i < atk.targetSettings.Length; i++)
			{
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
		}
	}

	protected override void EffectSkillGUI() {
		if((patk.mergeScheduledEffects = MergeChoiceGUI("On-Scheduled Effects", patk.mergeScheduledEffects)) != MergeMode.UseOriginal) {
	 		atk.scheduledEffects = EditorGUIExt.StatEffectGroupGUI("On-Scheduled Effect", atk.scheduledEffects, StatEffectContext.Action, ""+atk.GetInstanceID(), formulaOptions, lastFocusedControl);
		}
		if((patk.mergeApplicationEffects = MergeChoiceGUI("Per-Application Effects", patk.mergeApplicationEffects)) != MergeMode.UseOriginal) {
			atk.applicationEffects = EditorGUIExt.StatEffectGroupGUI("Per-Application Effect", atk.applicationEffects, StatEffectContext.Action, ""+atk.GetInstanceID(), formulaOptions, lastFocusedControl);
		}
		if((patk.mergeTargetEffects = MergeChoiceGUI("Per-Target Effects", patk.mergeTargetEffects)) != MergeMode.UseOriginal) {
			atk.targetEffects = EditorGUIExt.StatEffectGroupsGUI("Application Effect Group", atk.targetEffects, StatEffectContext.Action, ""+atk.GetInstanceID(), formulaOptions, lastFocusedControl);
		}
	}

	protected override void BasicSkillGUI() {
		CoreSkillGUI();
		patk.referredSkillName = EditorGUILayout.TextField("Referred Skill", patk.referredSkillName);

		if((patk.mergeIsEnabledF = MergeChoiceGUI("IsEnabled", patk.mergeIsEnabledF)) != MergeMode.UseOriginal) {
			s.isEnabledF = EditorGUIExt.FormulaField(
				"Is Enabled",
				s.isEnabledF,
				s.skillName+".isEnabledF",
				formulaOptions,
				lastFocusedControl
			);
		}
		s.replacesSkill = EditorGUILayout.
			Toggle("Replaces Skill", s.replacesSkill);
		if(s.replacesSkill) {
			s.replacedSkill = EditorGUILayout.
				TextField("Skill", s.replacedSkill).NormalizeName();
			s.replacementPriority = EditorGUILayout.
				IntField("Priority", s.replacementPriority);
		}

		if(!s.isPassive) {
			s.deactivatesOnApplication = EditorGUILayout.
				Toggle("Deactivates After Use", s.deactivatesOnApplication);
		}

		EditorGUILayout.Space();
		if((patk.mergeParameters = MergeListChoiceGUI("Parameters", patk.mergeParameters)) != MergeModeList.UseOriginal) {
			s.parameters = EditorGUIExt.ParameterFoldout(
				"Parameter",
				s.parameters,
				""+s.GetInstanceID(),
				formulaOptions,
				lastFocusedControl,
				ref showParameters
			);
		}
		EditorGUILayout.Space();

		if((patk.mergePassiveEffects = MergeListChoiceGUI("Passive Effects", patk.mergePassiveEffects)) != MergeModeList.UseOriginal) {
			s.passiveEffects = EditorGUIExt.StatEffectFoldout(
				"Passive Effect",
				s.passiveEffects,
				StatEffectContext.Normal,
				""+s.GetInstanceID(),
				formulaOptions,
				lastFocusedControl,
				ref showPassiveEffects
			);
		}
	}

	public override void OnSRPGCKInspectorGUI () {
		BasicSkillGUI();
		EditorGUILayout.Space();
		if((patk.mergeIO = MergeChoiceGUI("I/O", patk.mergeIO)) != MergeMode.UseOriginal) {
			if(atk._io == null) {
				atk._io = ScriptableObject.CreateInstance<SkillIO>();
			}
			atk._io = EditorGUIExt.PickAssetGUI<SkillIO>("I/O", atk._io);
		}
		EditorGUILayout.Space();
		TargetedSkillGUI();
		EditorGUILayout.Space();
		EffectSkillGUI();
		EditorGUILayout.Space();
	}
}
