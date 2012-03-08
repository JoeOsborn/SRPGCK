using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(ActionSkill))]
public class ActionSkillEditor : SkillEditor {
	ActionSkill atk;
	bool showIO=true;

	GUIContent[] targetingModes;
	GUIContent[] displayUnimpededTargetRegionFlags;

 	public override void OnEnable() {
		base.OnEnable();
		name = "ActionSkill";
		atk = target as ActionSkill;
	}

	protected void TargetedSkillGUI() {
		if(targetingModes == null || targetingModes.Length == 0) {
			targetingModes = new GUIContent[]{
				new GUIContent("Self", EditorGUIUtility.LoadRequired("skl-target-self.png") as Texture),
				new GUIContent("Pick", EditorGUIUtility.LoadRequired("skl-target-pick.png") as Texture),
				new GUIContent("Face", EditorGUIUtility.LoadRequired("skl-target-cardinal.png") as Texture),
				new GUIContent("Turn", EditorGUIUtility.LoadRequired("skl-target-radial.png") as Texture),
				new GUIContent("Select Region", EditorGUIUtility.LoadRequired("skl-target-selectregion.png") as Texture),
				new GUIContent("Draw Path", EditorGUIUtility.LoadRequired("skl-target-path.png") as Texture)
			};
		}
		if(displayUnimpededTargetRegionFlags == null || displayUnimpededTargetRegionFlags.Length == 0) {
			displayUnimpededTargetRegionFlags = new GUIContent[]{
				new GUIContent("Hide", EditorGUIUtility.LoadRequired("skl-impeded-off.png") as Texture),
				new GUIContent("Display", EditorGUIUtility.LoadRequired("skl-impeded-on.png") as Texture)
			};
		}

		//FIXME: implicit assumption: lockToGrid=true;
		//so, no invertOverlay, overlayType, drawOverlayRim, drawOverlayVolume, rotationSpeedXYF
		//FIXME: not showing probe for now
		if((showIO = EditorGUILayout.Foldout(showIO, "I/O"))) {
			EditorGUI.indentLevel++;
			atk.overlayColor = EditorGUILayout.ColorField("Target Overlay", atk.overlayColor);
			atk.highlightColor = EditorGUILayout.ColorField("Effect Highlight", atk.highlightColor);
			atk.supportKeyboard = EditorGUILayout.Toggle("Support Keyboard", atk.supportKeyboard);
			if(atk.supportKeyboard) {
				atk.keyboardMoveSpeed = EditorGUILayout.FloatField("Keyboard Move Speed", atk.keyboardMoveSpeed);
				atk.indicatorCycleLength = EditorGUILayout.FloatField("Z Cycle Time", atk.indicatorCycleLength);
			}
			atk.supportMouse = EditorGUILayout.Toggle("Support Mouse", atk.supportMouse);
			atk.requireConfirmation = EditorGUILayout.Toggle("Require Confirmation", atk.requireConfirmation);
			if(atk.HasTargetingMode(TargetingMode.Path)) {
				atk.pathMaterial = EditorGUILayout.ObjectField("Path Material", atk.pathMaterial, typeof(Material), false) as Material;
			}
			if(atk.HasTargetingMode(TargetingMode.Pick) ||
			   atk.HasTargetingMode(TargetingMode.Path)) {
 				if(atk.requireConfirmation) {
 					atk.performTemporaryStepsOnConfirmation = EditorGUILayout.Toggle("Preview Before Confirming", atk.performTemporaryStepsOnConfirmation);
 				}
				atk.performTemporaryStepsImmediately = EditorGUILayout.Toggle("Preview Immediately", atk.performTemporaryStepsImmediately);
			} else {
				atk.performTemporaryStepsImmediately = false;
				atk.performTemporaryStepsOnConfirmation = true;
			}
			EditorGUI.indentLevel--;
			EditorGUILayout.Space();
		}
		atk.delay = EditorGUIExt.FormulaField("Scheduled Delay", atk.delay, atk.name+".delay", formulaOptions);
		if((atk.multiTargetMode = (MultiTargetMode)EditorGUILayout.EnumPopup("Multi-Target Mode", atk.multiTargetMode)) != MultiTargetMode.Single) {
			if(atk.multiTargetMode == MultiTargetMode.Chain) {
				atk.maxWaypointDistanceF = EditorGUIExt.FormulaField("Max Waypoint Distance", atk.maxWaypointDistanceF, atk.name+".targeting.maxWaypointDistance", formulaOptions);
			}
			atk.waypointsAreIncremental = EditorGUILayout.Toggle("Instantly Apply Waypoints", atk.waypointsAreIncremental);
			atk.canCancelWaypoints = EditorGUILayout.Toggle("Cancellable Waypoints", atk.canCancelWaypoints);
			if(atk.targetSettings == null) { atk.targetSettings = new TargetSettings[0]; }
			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			int arraySize = EditorGUILayout.IntField(atk.targetSettings.Length, GUILayout.Width(32));
			GUILayout.Label(" "+"Target"+(atk.targetSettings.Length == 1 ? "" : "s"));
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			if(arraySize != atk.targetSettings.Length) {
				atk.targetSettings = new TargetSettings[arraySize];
			}
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();
	   	EditorGUIUtility.LookLikeControls();
			for(int i = 0; i < atk.targetSettings.Length; i++)
			{
		   	EditorGUIUtility.LookLikeControls();
				TargetSettings ts = atk.targetSettings[i];
				if (ts == null) {
					atk.targetSettings[i] = new TargetSettings();
					ts = atk.targetSettings[i];
				}
				atk.targetSettings[i] = TargetSettingsGUI("Target "+i, atk.targetSettings[i], i);
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
	   	EditorGUIUtility.LookLikeControls();
		} else {
			if(atk.targetSettings == null) {
				atk.targetSettings = new TargetSettings[]{new TargetSettings()};
			}
			atk.targetSettings[0] = TargetSettingsGUI("Target", atk.targetSettings[0], -1);
		}
	}

	protected virtual TargetSettings TargetSettingsGUI(string label, TargetSettings ts, int i=-1) {
		//targeting modes
		//FIXME: asset!
		int buttonsWide = (int)(Screen.width/EditorGUIExt.buttonDim);
		if(!(ts.showInEditor = EditorGUILayout.Foldout(ts.showInEditor, "Target "+(i==-1?"":""+i)))) {
			return ts;
		}
		if(ts.targetingMode == TargetingMode.Pick ||
		   ts.targetingMode == TargetingMode.Path) {
		  ts.allowsCharacterTargeting = EditorGUILayout.Toggle("Can Delay-Target Characters", ts.allowsCharacterTargeting);
		} else {
			ts.allowsCharacterTargeting = false;
		}
		EditorGUI.indentLevel++;
		ts.targetingMode = (TargetingMode)GUILayout.SelectionGrid((int)ts.targetingMode, targetingModes, buttonsWide, EditorGUIExt.imageButtonGridStyle);
		if(ts.targetingMode == TargetingMode.SelectRegion) {
			ts.targetRegion.type = RegionType.Compound;
		}
		if(ts.targetingMode == TargetingMode.Path) {
			ts.newNodeThreshold = EditorGUILayout.FloatField("Min Path Distance", ts.newNodeThreshold);
			ts.immediatelyExecuteDrawnPath = EditorGUILayout.Toggle("Instantly Apply Path", ts.immediatelyExecuteDrawnPath);
		}
		if(ts.targetingMode != TargetingMode.Self) {
			if(ts.targetRegion == null) {
				ts.targetRegion = new Region();
			}
			if(ts.targetRegion.interveningSpaceType != InterveningSpaceType.Pick) {
				GUILayout.Label("Show blocked tiles?");
				ts.displayUnimpededTargetRegion = GUILayout.SelectionGrid(ts.displayUnimpededTargetRegion ? 1 : 0, displayUnimpededTargetRegionFlags, 2, EditorGUIExt.imageButtonGridStyle) == 1 ? true : false;
			}
		}
		EditorGUI.indentLevel--;
		EditorGUILayout.Space();
		ts.targetRegion = EditorGUIExt.RegionGUI("Target", atk.name+"."+i, ts.targetRegion, formulaOptions, Screen.width-32);
		EditorGUILayout.Space();
		if(!(atk is MoveSkill)) {
			ts.effectRegion = EditorGUIExt.RegionGUI("Effect", atk.name+"."+i, ts.effectRegion, formulaOptions, Screen.width-32);
		}
		return ts;
	}

	protected void EffectSkillGUI() {
		if(atk.targetEffects != null && atk.targetEffects.Length > 1) {
			EditorGUILayout.HelpBox("Be sure that the hitType parameter is defined to provide a value from 0 to "+(atk.targetEffects.Length-1), (s.HasParam("hitType") ? MessageType.Info : MessageType.Error));
		}
		atk.applicationEffects = EditorGUIExt.StatEffectGroupGUI("Per-Application Effect", atk.applicationEffects, StatEffectContext.Action, formulaOptions, lastFocusedControl);
		atk.targetEffects = EditorGUIExt.StatEffectGroupsGUI("Application Effect Group", atk.targetEffects, StatEffectContext.Action, formulaOptions, lastFocusedControl);
	}

	public override void OnSRPGCKInspectorGUI () {
		BasicSkillGUI();
		EditorGUILayout.Space();
		TargetedSkillGUI();
		EditorGUILayout.Space();
		EffectSkillGUI();
		EditorGUILayout.Space();
		ReactionSkillGUI();
		EditorGUILayout.Space();
	}
}