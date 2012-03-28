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
		skipStats = new string[]{"team", "facing", "isTargetable"};
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

		c.canMountF =
			EditorGUIExt.FormulaField(
				"Can Mount (t.)",
				c.canMountF ?? Formula.True(),
				"character.canMount.param",
				formulaOptions,
				lastFocusedControl,
				0
			);

		c.isMountableF =
			EditorGUIExt.FormulaField(
				"Mountable By (t.)",
				c.isMountableF ?? Formula.False(),
				"character.isMountable.param",
				formulaOptions,
				lastFocusedControl,
				0
			);

		c.EditorSetBaseStat("isTargetable",
			EditorGUIExt.FormulaField(
				"Is Targetable",
				c.EditorGetBaseStat("isTargetable") ?? Formula.True(),
				"character.isTargetable.param",
				formulaOptions,
				lastFocusedControl,
				0
			)
		);

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