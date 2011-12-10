using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(Skill))]
//TODO: ActionSkillEditor and MoveSkillEditor.
public class SkillEditor : SRPGCKEditor {
	bool showParameters=true;
	bool showPassiveEffects=true;
	bool showReactionTypesApplied=true, showReactionTypesApplier=true;
	bool showReactionStatChangesApplied=true, showReactionStatChangesApplier=true;
	
	protected Skill s;
  public override void OnEnable() {
		base.OnEnable();
		name = "Skill";
		s = target as Skill;
	}
	
	public override void OnSRPGCKInspectorGUI () {
		float halfWidth = Screen.width/2;
		GUILayout.BeginVertical();

		s.skillName = EditorGUILayout.TextField("Name", s.skillName).NormalizeName();
		s.skillGroup = EditorGUILayout.TextField("Group", s.skillGroup).NormalizeName();
		s.skillSorting = EditorGUILayout.IntField("Sorting", s.skillSorting);
		s.replacesSkill = EditorGUILayout.Toggle("Replaces Skill", s.replacesSkill);
		if(s.replacesSkill) {
			s.replacedSkill = EditorGUILayout.TextField("Skill", s.replacedSkill).NormalizeName();
			s.replacementPriority = EditorGUILayout.IntField("Priority", s.replacementPriority);
		}
		EditorGUILayout.Space();

		s.isPassive = EditorGUILayout.Toggle("Passive Skill", s.isPassive);
		
		EditorGUILayout.Space();
		//parameters LATER: group parameters by used component (e.g. reaction. params near reaction)
		s.parameters = EditorGUIExt.ParameterFoldout("Parameter", s.parameters, formulaOptions, lastFocusedControl, ref showParameters);
		EditorGUILayout.Space();
	
		s.passiveEffects = EditorGUIExt.StatEffectFoldout("Passive Effect", s.passiveEffects, StatEffectContext.Normal, formulaOptions, lastFocusedControl, ref showPassiveEffects);
	
		EditorGUILayout.Space();
		s.reactionSkill = EditorGUILayout.Toggle("Is Reaction Skill", s.reactionSkill);
		if(s.reactionSkill) {
			GUILayout.Label("Reaction Triggers");
			EditorGUILayout.BeginHorizontal(GUILayout.Width(Screen.width-32));
			s.reactionStatChangesApplier = EditorGUIExt.StatChangeFoldout("Stat Change (Attacker)", s.reactionStatChangesApplier, ref showReactionStatChangesApplier, halfWidth-16);
			s.reactionStatChangesApplied = EditorGUIExt.StatChangeFoldout("Stat Change (Defender)", s.reactionStatChangesApplied, ref showReactionStatChangesApplied, halfWidth-16);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal(GUILayout.Width(Screen.width-32));
			s.reactionTypesApplier = EditorGUIExt.ArrayFoldout("Effect Type (Attacker)", s.reactionTypesApplier, ref showReactionTypesApplier, false, halfWidth-16, "attack");
			s.reactionTypesApplied = EditorGUIExt.ArrayFoldout("Effect Type (Defender)", s.reactionTypesApplied, ref showReactionTypesApplied, false, halfWidth-16, "attack");
			EditorGUILayout.EndHorizontal();
			//reaction strategy
			//reaction effects
			s.reactionStrategy = EditorGUIExt.StrategyGUI(s.reactionStrategy);
			s.reactionEffects = EditorGUIExt.StatEffectGroupsGUI("Effect Group", s.reactionEffects, StatEffectContext.Action, formulaOptions, lastFocusedControl);
			EditorGUILayout.Space();
			if(s.reactionEffects != null && s.reactionEffects.Length > 1) {
				GUI.enabled=false;
				bool priorWrap = EditorStyles.textField.wordWrap;
				Color priorColor = EditorStyles.textField.normal.textColor;
				EditorStyles.textField.wordWrap = true;
				if(!s.HasParam("reaction.hitType")) {
					EditorStyles.textField.normal.textColor = Color.red;	
				}
				EditorGUILayout.TextArea("Be sure that the reaction.hitType parameter is defined to provide a value from 0 to "+(s.reactionEffects.Length-1), GUILayout.Width(Screen.width-32));
				EditorStyles.textField.wordWrap = priorWrap;
				EditorStyles.textField.normal.textColor = priorColor;	
				GUI.enabled=true;
			}
		}

		//be sure each reaction and target effect's reactableTypes and target are shown!
		
/*		EditorGUILayout.BeginHorizontal();
		EditorGUIExt.ArrayFoldout("Slots", e.equipmentSlots, ref showSlots, halfWidth);
		EditorGUIExt.ArrayFoldout("Categories", e.equipmentCategories, ref showCategories, halfWidth);
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();

		e.parameters = EditorGUIExt.ParameterFoldout("Parameter", e.parameters, formulaOptions, lastFocusedControl, ref showParameters);
		
		EditorGUILayout.Space();

		
		EditorGUILayout.Space();
		
		e.statusEffectPrefabs = EditorGUIExt.ObjectArrayFoldout<StatusEffect>("Status Effect Prefabs", e.statusEffectPrefabs, ref showStatusEffects);
*/		
		GUILayout.EndVertical();
	}
}