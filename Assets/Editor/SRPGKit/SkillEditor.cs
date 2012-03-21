using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

[CustomEditor(typeof(Skill))]
public class SkillEditor : SRPGCKEditor {
	bool showParameters=true;
	bool showPassiveEffects=true;
	bool showReactionTypesApplied=true, showReactionTypesApplier=true;
	bool showReactionStatChangesApplied=true, showReactionStatChangesApplier=true;

	protected Skill s;
  public override void OnEnable() {
		s = target as Skill;
		fdb = s.fdb;
		base.OnEnable();
		name = "Skill";
	}

	protected void CoreSkillGUI() {
		s.skillName = EditorGUILayout.
			TextField("Name", s.skillName).NormalizeName();
		s.skillGroup = EditorGUILayout.
			TextField("Group", s.skillGroup).NormalizeName();
		s.skillSorting = EditorGUILayout.
			IntField("Sorting", s.skillSorting);
	}

	protected void BasicSkillGUI() {
		CoreSkillGUI();
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
		//parameters LATER: group parameters by used component
		//(e.g. reaction. params near reaction)
		s.parameters = EditorGUIExt.ParameterFoldout(
			"Parameter",
			s.parameters,
			""+s.GetInstanceID(),
			formulaOptions,
			lastFocusedControl,
			ref showParameters
		);
		EditorGUILayout.Space();

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

	protected void ReactionSkillGUI() {
		s.reactionSkill = EditorGUILayout.Toggle(
			"Reaction Skill",
			s.reactionSkill
		);
		if(s.reactionSkill) {
			GUILayout.Label("Reaction Triggers");
			EditorGUILayout.HelpBox(
				"e.g. \"applied loses health\"",
				MessageType.Info
			);
			s.reactionStatChangesApplied = EditorGUIExt.StatChangeFoldout(
				"Stat Change (Applied)",
				s.reactionStatChangesApplied,
				ref showReactionStatChangesApplied
			);
			EditorGUILayout.HelpBox(
				"e.g. \"applied is hit by geomancy\"",
				MessageType.Info
			);
			s.reactionTypesApplied = EditorGUIExt.ArrayFoldout(
				"Reactable Type (Applied)",
				s.reactionTypesApplied,
				ref showReactionTypesApplied,
				false,
				-1,
				"attack"
			);
			EditorGUILayout.HelpBox(
				"e.g. \"applier loses MP\"",
				MessageType.Info
			);
			s.reactionStatChangesApplier = EditorGUIExt.StatChangeFoldout(
				"Stat Change (Applier)",
				s.reactionStatChangesApplier,
				ref showReactionStatChangesApplier
			);
			EditorGUILayout.HelpBox(
				"e.g. \"applier receives vampirism bonus\"",
				MessageType.Info
			);
			s.reactionTypesApplier = EditorGUIExt.ArrayFoldout(
				"Reactable Type (Applier)",
				s.reactionTypesApplier,
				ref showReactionTypesApplier,
				false,
				-1,
				"attack"
			);
			//reaction region
			//reaction effects
			s.reactionTargetRegion = EditorGUIExt.RegionGUI(
				"Reaction Target",
				s.name+".reaction",
				s.reactionTargetRegion,
				formulaOptions,
				lastFocusedControl,
				Screen.width-32
			);
			s.reactionEffectRegion = EditorGUIExt.RegionGUI(
				"Reaction Effect",
				s.name+".reaction",
				s.reactionEffectRegion,
				formulaOptions,
				lastFocusedControl,
				Screen.width-32
			);
			if(s.reactionEffects != null && s.reactionEffects.Length > 1) {
				EditorGUILayout.HelpBox(
					"Be sure that the reaction.hitType parameter is defined "+
						"to provide a value from 0 to "+(s.reactionEffects.Length-1),
					(s.HasParam("reaction.hitType") ? MessageType.Info : MessageType.Error));
			}
			s.reactionApplicationEffects = EditorGUIExt.StatEffectGroupGUI(
				"Per-Reaction Effect",
				s.reactionApplicationEffects,
				StatEffectContext.Action,
				""+s.GetInstanceID(),
				formulaOptions,
				lastFocusedControl
			);
			s.reactionEffects = EditorGUIExt.StatEffectGroupsGUI(
				"Per-Target Effect Group",
				s.reactionEffects,
				StatEffectContext.Action,
				""+s.GetInstanceID(),
				formulaOptions,
				lastFocusedControl
			);
			EditorGUILayout.Space();
		}
	}

	protected override void SaveAsset() {
		if(s.def != null && s.def.reallyDefined) {
			CopyFieldsTo<SkillDef, Skill>(s.def, s);
		}
		base.SaveAsset();
		if(s.def != null && s.def.reallyDefined) {
			EditorUtility.SetDirty(s.def);
		}
	}

	public override void OnSRPGCKInspectorGUI () {
		s.def = EditorGUIExt.PickAssetGUI<SkillDef>("Skill Definition", s.def);
		if(s.def == null || !s.def.reallyDefined) {
			if(GUILayout.Button("Convert to Skill Definition")) {
				EnsurePath("Assets/SRPGCK Data/Skills");
				SkillDef def = ScriptableObjectUtility.CreateAsset<SkillDef>(
					s.skillName,
					"Assets/SRPGCK Data/Skills",
					false
				);
				CopyFieldsTo<Skill, SkillDef>(s, def);
				s.def = def;
				def.reallyDefined = true;
			}
			GUILayout.BeginVertical();
			BasicSkillGUI();
			EditorGUILayout.Space();
			ReactionSkillGUI();
			GUILayout.EndVertical();
		} else {
			//edit the sdef!
			// GUILayout.BeginVertical();
			// SkillDefEditor.BasicSkillGUI(s.def, fdb);
			// EditorGUILayout.Space();
			// SkillDefEditor.ReactionSkillGUI(s.def, fdb);
			// GUILayout.EndVertical();
			//set it to dirty!
		}
	}
}

