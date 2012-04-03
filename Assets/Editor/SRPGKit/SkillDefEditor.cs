using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(SkillDef))]
public class SkillDefEditor : SRPGCKEditor {
	[MenuItem("SRPGCK/Create generic skill", false, 20)]
	public static SkillDef CreateSkillDef() {
		SkillDef sd = ScriptableObjectUtility.CreateAsset<SkillDef>(
			null,
			"Assets/SRPGCK Data/Skills/Generic",
			true
		);
		sd.isEnabledF = Formula.True();
		sd.reallyDefined = true;
		return sd;
	}
	protected bool showParameters=true;
	protected bool showPassiveEffects=true;
	protected bool showReactionTypesApplied=true, showReactionTypesApplier=true;
	protected bool showReactionStatChangesApplied=true, showReactionStatChangesApplier=true;

	protected SkillDef s;
  public override void OnEnable() {
		s = target as SkillDef;
		fdb = Formulae.DefaultFormulae;
		base.OnEnable();
		name = "Skill Definition";
	}

	protected void CoreSkillGUI() {
		s.skillName = EditorGUILayout.
			TextField("Name", s.skillName).NormalizeName();
		s.skillGroup = EditorGUILayout.
			TextField("Group", s.skillGroup).NormalizeName();
		s.skillSorting = EditorGUILayout.
			IntField("Sorting", s.skillSorting);
	}

	protected virtual void BasicSkillGUI() {
		CoreSkillGUI();
		s.isEnabledF = EditorGUIExt.FormulaField(
			"Is Enabled",
			s.isEnabledF,
			s.skillName+".isEnabledF",
			formulaOptions,
			lastFocusedControl
		);
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

	public override void OnSRPGCKInspectorGUI () {
		GUILayout.BeginVertical();
		BasicSkillGUI();
		EditorGUILayout.Space();
		ReactionSkillGUI();
		GUILayout.EndVertical();
	}
}
