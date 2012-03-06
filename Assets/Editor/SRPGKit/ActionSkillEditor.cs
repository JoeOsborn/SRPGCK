using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(ActionSkill))]
public class ActionSkillEditor : SkillEditor {
	ActionSkill atk;
	bool showIO=true;
	bool showTargeting=true;

	GUIContent[] targetingModes;
	GUIContent[] displayUnimpededTargetRegionFlags;

 	public override void OnEnable() {
		base.OnEnable();
		name = "ActionSkill";
		atk = target as ActionSkill;
	}

	protected void TargetedSkillGUI() {
		int buttonsWide = (int)(Screen.width/EditorGUIExt.buttonDim);
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
			if(atk.targetingMode == TargetingMode.Path) {
				atk.pathMaterial = EditorGUILayout.ObjectField("Path Material", atk.pathMaterial, typeof(Material), false) as Material;
			}
			if(atk.targetingMode == TargetingMode.Pick ||
			   atk.targetingMode == TargetingMode.Path) {
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
		if((showTargeting = EditorGUILayout.Foldout(showTargeting, "Targeting Mode"))) {
			EditorGUI.indentLevel++;
			atk.targetingMode = (TargetingMode)GUILayout.SelectionGrid((int)atk.targetingMode, targetingModes, buttonsWide, EditorGUIExt.imageButtonGridStyle);
			if(atk.targetingMode == TargetingMode.SelectRegion) {
				atk.targetRegion.type = RegionType.Compound;
			}
			if(atk.targetingMode == TargetingMode.Path) {
				atk.newNodeThreshold = EditorGUILayout.FloatField("Min Path Distance", atk.newNodeThreshold);
				atk.immediatelyExecuteDrawnPath = EditorGUILayout.Toggle("Instantly Apply Path", atk.immediatelyExecuteDrawnPath);
			}
			if(atk.targetingMode != TargetingMode.Self) {
				if(atk.targetRegion == null) {
					atk.targetRegion = new Region();
				}
				if(atk.targetRegion.interveningSpaceType != InterveningSpaceType.Pick) {
					GUILayout.Label("Show blocked tiles?");
					atk.displayUnimpededTargetRegion = GUILayout.SelectionGrid(atk.displayUnimpededTargetRegion ? 1 : 0, displayUnimpededTargetRegionFlags, 2, EditorGUIExt.imageButtonGridStyle) == 1 ? true : false;
				}
			}
			if(atk.targetingMode == TargetingMode.Pick ||
			   atk.targetingMode == TargetingMode.Path) {
 				if(!(atk.useOnlyOneWaypoint = !EditorGUILayout.Toggle("Use Waypoints", !atk.useOnlyOneWaypoint))) {
 					atk.waypointsAreIncremental = EditorGUILayout.Toggle("Instantly Apply Waypoints", atk.waypointsAreIncremental);
 					atk.maxWaypointDistance = EditorGUIExt.FormulaField("Max Waypoint Distance", atk.maxWaypointDistance, atk.name+".targeting.maxWaypointDistance", formulaOptions);
 					atk.canCancelWaypoints = EditorGUILayout.Toggle("Cancellable Waypoints", atk.canCancelWaypoints);
 				}
			}
			EditorGUI.indentLevel--;
			EditorGUILayout.Space();
		}
		atk.delay = EditorGUIExt.FormulaField("Scheduled Delay", atk.delay, atk.name+".delay", formulaOptions);
		if(atk.targetingMode == TargetingMode.Pick ||
		   atk.targetingMode == TargetingMode.Path) {
		  atk.allowsCharacterTargeting = EditorGUILayout.Toggle("Can Delay-Target Characters", atk.allowsCharacterTargeting);
		} else {
			atk.allowsCharacterTargeting = false;
		}
		atk.targetRegion = EditorGUIExt.RegionGUI("Target", atk.name, atk.targetRegion, formulaOptions, Screen.width-32);
		EditorGUILayout.Space();
	}

	protected void EffectSkillGUI() {
		atk.effectRegion = EditorGUIExt.RegionGUI("Effect", atk.name, atk.effectRegion, formulaOptions, Screen.width-32);
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