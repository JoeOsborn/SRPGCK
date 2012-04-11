using UnityEngine;
using UnityEditor;
using System.Collections;
using System;
using System.Linq;

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
		int newTeamCount = EditorGUILayout.IntField("Teams", arb.EditorGetTeamCount());
		if(newTeamCount < 1) { newTeamCount = 1; }
		while(newTeamCount > arb.EditorGetTeamCount()) {
			int id = 1;
			while(arb.EditorGetTeam(id) != null) {
				id++;
			}
			var tgo = new GameObject();
			tgo.transform.parent = arb.transform;
			tgo.transform.localPosition = Vector3.zero;
			tgo.transform.localRotation = Quaternion.identity;
			tgo.transform.localScale = new Vector3(1,1,1);
			var t = tgo.AddComponent<Team>();
			t.id = id;
			tgo.name = "Team "+id;
		}
		var sorted = arb.EditorGetTeams().ToList();
		while(newTeamCount < arb.EditorGetTeamCount() && sorted.Count > 0) {
			var t = sorted[sorted.Count-1];
			DestroyImmediate(t.gameObject);
			sorted.RemoveAt(sorted.Count-1);
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
		foreach(var t in arb.EditorGetTeams()) {
			EditorGUILayout.BeginVertical();
			if(GUILayout.Button(""+t.id, GUILayout.Width(32), GUILayout.Height(16))) {
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndHorizontal();
				EditorUtility.FocusProjectWindow();
				Selection.activeObject = t;
				return;
			}
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(12);
			EditorGUILayout.BeginVertical();
			//radio boxes
			bool   local = t.type == TeamLocation.Local;
			bool      ai = t.type == TeamLocation.AI;
			bool network = t.type == TeamLocation.Network;
			if(  local = EditorGUILayout.Toggle(  local, EditorStyles.radioButton, GUILayout.Width(16), GUILayout.Height(16))) {
				t.type = TeamLocation.Local;
				ai = false;
				network = false;
			}
			if(     ai = EditorGUILayout.Toggle(     ai, EditorStyles.radioButton, GUILayout.Width(16), GUILayout.Height(16))) {
				t.type = TeamLocation.AI;
				network = false;
			}
			if(network = EditorGUILayout.Toggle(network, EditorStyles.radioButton, GUILayout.Width(16), GUILayout.Height(16))) {
				t.type = TeamLocation.Network;
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();
//			GUILayout.Space(6);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndScrollView();
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
	}

}
