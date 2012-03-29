using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(StatusEffect))]

public class StatusEffectEditor : SRPGCKEditor {
	bool showPassiveEffects=true;
	bool showTickIntervalEffects=true;
	bool showCharacterActivatedEffects=true;
	bool showCharacterDeactivatedEffects=true;
	bool showStatusEffectAppliedEffects=true;
	bool showStatusEffectRemovedEffects=true;

	public override void OnEnable() {
		//FIXME: fdb = What exactly?
		//status effects don't have owners in
		//any meaningful data-lookup sense!
		base.OnEnable();
		name = "StatusEffect";
	}

	public override void OnSRPGCKInspectorGUI () {
		StatusEffect se = target as StatusEffect;

		se.effectType = EditorGUILayout.TextField("Effect Type", se.effectType).NormalizeName();
		se.always = EditorGUILayout.Toggle("Unremovable", se.always);
		if(se.always) { se.usesDuration = false; }
		GUILayout.BeginHorizontal();
		se.usesDuration = EditorGUILayout.Toggle("Time-Limited", se.usesDuration);
		se.ticksInLocalTime = EditorGUILayout.Toggle("Uses Char. Spd", se.ticksInLocalTime);
		GUILayout.EndHorizontal();
		if(se.usesDuration) {
			se.tickDuration = (float)EditorGUILayout.IntField((se.ticksInLocalTime ? "CT" : "Global") + " Ticks", (int)se.tickDuration);
		} else {
			se.tickDuration = 0;
			se.ticksInLocalTime = false;
		}
		GUILayout.BeginHorizontal();
		se.overrides = EditorGUILayout.Toggle("Overrides Similar Effects", se.overrides);
		if(se.overrides) {
			se.overridePriority = EditorGUILayout.IntField("Strength", se.overridePriority);
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		se.replaces = EditorGUILayout.Toggle("Replaces Similar Effects", se.replaces);
		if(se.overrides) {
			se.replacementPriority = EditorGUILayout.IntField("Strength", se.replacementPriority);
		}
		GUILayout.EndHorizontal();
		EditorGUILayout.Space();
		//now, effects!
		se.passiveEffects = EditorGUIExt.StatEffectFoldout("Passive Effect", se.passiveEffects, StatEffectContext.Normal, ""+se.GetInstanceID(), formulaOptions, lastFocusedControl, ref showPassiveEffects);

		se.tickEffectInterval = EditorGUILayout.IntField((se.ticksInLocalTime ? "CT" : "Global") + " Tick Effect Interval", (int)se.tickEffectInterval);
		se.tickIntervalEffects = EditorGUIExt.StatEffectFoldout("Tick Interval Effect", se.tickIntervalEffects, StatEffectContext.Normal, ""+se.GetInstanceID(), formulaOptions, lastFocusedControl, ref showTickIntervalEffects);

		se.characterActivatedEffects = EditorGUIExt.StatEffectFoldout("Character-Activation Effect", se.characterActivatedEffects, StatEffectContext.Normal, ""+se.GetInstanceID(), formulaOptions, lastFocusedControl, ref showCharacterActivatedEffects);

		se.characterDeactivatedEffects = EditorGUIExt.StatEffectFoldout("Character-Deactivation Effect", se.characterDeactivatedEffects, StatEffectContext.Normal, ""+se.GetInstanceID(), formulaOptions, lastFocusedControl, ref showCharacterDeactivatedEffects);

		se.statusEffectAppliedEffects = EditorGUIExt.StatEffectFoldout("On-Application Effect", se.statusEffectAppliedEffects, StatEffectContext.Normal, ""+se.GetInstanceID(), formulaOptions, lastFocusedControl, ref showStatusEffectAppliedEffects);

		se.statusEffectRemovedEffects = EditorGUIExt.StatEffectFoldout("On-Removal Effect", se.statusEffectRemovedEffects, StatEffectContext.Normal, ""+se.GetInstanceID(), formulaOptions, lastFocusedControl, ref showStatusEffectRemovedEffects);
	}
}