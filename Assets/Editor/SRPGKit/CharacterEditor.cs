using UnityEditor;
using UnityEngine;

using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(Character))]
public class CharacterEditor : SRPGCKEditor {
	bool showStats=true;
	bool showSlots=true;
	
	Character c;
  public override void OnEnable() {
		c = target as Character;
		fdb = c.fdb;
		base.OnEnable();
		name = "Character";
	}
	public override void OnSRPGCKInspectorGUI () {		
		c.characterName = EditorGUILayout.TextField("Name", c.characterName).NormalizeName();
		c.teamID = EditorGUILayout.IntField("Team #", c.teamID);
		EditorGUILayout.Space();
		c.transformOffset = EditorGUILayout.Vector3Field("Visual Offset:", c.transformOffset);

		EditorGUILayout.Space();
		
		c.equipmentSlots = EditorGUIExt.ArrayFoldout("Slots", c.equipmentSlots, ref showSlots, false, Screen.width/2, "body");
		
		EditorGUILayout.Space();

		c.stats = EditorGUIExt.ParameterFoldout("Statistic", c.stats, formulaOptions, lastFocusedControl, ref showStats);

		EditorGUILayout.Space();
	}
}