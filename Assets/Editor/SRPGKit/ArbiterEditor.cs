using UnityEngine;
using UnityEditor;
using System.Collections;
using System;

[CustomEditor(typeof(Arbiter))]

public class ArbiterEditor : SRPGCKEditor {

	Arbiter arb;
	public Vector2 teamScroll;

	public override void OnEnable() {
		useFormulae = false;
		arb = target as Arbiter;
		teamScroll = Vector2.zero;
		base.OnEnable();
		name = "Arbiter";
	}

	public override void OnSRPGCKInspectorGUI () {
		//fdb
		arb.formulae = EditorGUILayout.ObjectField("Formulae", arb.formulae, typeof(Formulae), !EditorUtility.IsPersistent(arb)) as Formulae;
		//teams
		int newTeamCount = EditorGUILayout.IntField("Teams", arb.teams.Length);
		if(newTeamCount < 1) { newTeamCount = 1; }
		if(newTeamCount != arb.teams.Length) {
			Array.Resize<TeamLocation>(ref arb.teams, newTeamCount);
		}
		//local/ai/network toggle grid
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.BeginVertical();
		//labels
		GUILayout.Label(  "Team:", GUILayout.Width(64), GUILayout.Height(18));
		GUILayout.Label(  "Local", GUILayout.Width(64), GUILayout.Height(18));
		GUILayout.Label(     "AI", GUILayout.Width(64), GUILayout.Height(18));
		GUILayout.Label("Network", GUILayout.Width(64), GUILayout.Height(18));
		EditorGUILayout.EndVertical();
		teamScroll = EditorGUILayout.BeginScrollView(teamScroll, false, false, GUILayout.Height(90));
		EditorGUILayout.BeginHorizontal();
		for(int i = 0; i < arb.teams.Length; i++) {
			EditorGUILayout.BeginVertical();
			GUILayout.Label(""+i, GUILayout.Width(20), GUILayout.Height(16));
			//radio boxes
			bool local = arb.teams[i] == TeamLocation.Local;
			bool ai = arb.teams[i] == TeamLocation.AI;
			bool network = arb.teams[i] == TeamLocation.Network;
			if(  local = EditorGUILayout.Toggle(  local, EditorStyles.radioButton, GUILayout.Width(16), GUILayout.Height(16))) {
				arb.teams[i] = TeamLocation.Local;
				ai = false;
				network = false;
			}
			if(     ai = EditorGUILayout.Toggle(     ai, EditorStyles.radioButton, GUILayout.Width(16), GUILayout.Height(16))) {
				arb.teams[i] = TeamLocation.AI;
				network = false;
			}
			if(network = EditorGUILayout.Toggle(network, EditorStyles.radioButton, GUILayout.Width(16), GUILayout.Height(16))) {
				arb.teams[i] = TeamLocation.Network;
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndScrollView();
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
	}

}
