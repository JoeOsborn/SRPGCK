using UnityEditor;
using UnityEngine;

using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(Character))]
public class CharacterEditor : SRPGCKEditor {
	bool showStats=true;
	bool showSlots=true;

	Character c;
	string[] skipStats;
  public override void OnEnable() {
		c = target as Character;
		skipStats = new string[]{"team"};
		fdb = c.fdb;
		base.OnEnable();
		name = "Character";
	}

	public override void OnSRPGCKInspectorGUI () {
		c.characterName = EditorGUILayout.TextField("Name", c.characterName).
			NormalizeName();
		c.EditorSetBaseStat(
			"team",
			EditorGUIExt.FormulaField(
				"Team #",
				c.EditorGetBaseStat("team") ?? Formula.Constant(0),
				"character.team.param",
				formulaOptions,
				lastFocusedControl,
				0
			)
		);
		EditorGUILayout.Space();

		c.transformOffset = EditorGUILayout.Vector3Field(
			"Visual Offset:",
			c.transformOffset
		);
		EditorGUILayout.Space();

		c.equipmentSlots = EditorGUIExt.ArrayFoldout(
			"Equipment Slots",
			c.equipmentSlots,
			ref showSlots,
			false,
			128,
			"body"
		);

		EditorGUILayout.Space();

		c.stats = EditorGUIExt.ParameterFoldout(
			"Statistic",
			c.stats,
			""+c.GetInstanceID(),
			formulaOptions,
			lastFocusedControl,
			ref showStats,
			skipStats
		);

		EditorGUILayout.Space();
	}
}