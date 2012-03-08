using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(StatusEffect))]

public class StatusEffectEditor : SRPGCKEditor {
	bool showPassiveEffects=true;

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
		if(se.usesDuration) {
			se.ticksInLocalTime = EditorGUILayout.Toggle("Uses Char. Spd", se.ticksInLocalTime);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			se.tickDuration = (float)EditorGUILayout.IntField((se.ticksInLocalTime ? "CT" : "Global") + " Ticks", (int)se.tickDuration);
		} else {
			se.tickDuration = 0;
			se.ticksInLocalTime = false;
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		se.replaces = EditorGUILayout.Toggle("Overrides Similar Effects", se.replaces);
		if(se.replaces) {
			se.priority = EditorGUILayout.IntField("Strength", se.priority);
		}
		GUILayout.EndHorizontal();
		EditorGUILayout.Space();
		//now, effects!
		se.passiveEffects = EditorGUIExt.StatEffectFoldout("Passive Effect", se.passiveEffects, StatEffectContext.Normal, ""+se.GetInstanceID(), formulaOptions, lastFocusedControl, ref showPassiveEffects);
	}
}