using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(Team))]
public class TeamEditor : SRPGCKEditor {
	Team team;
	public Vector2 teamScroll;

	public override void OnEnable() {
		team = target as Team;
		teamScroll = Vector2.zero;
		base.OnEnable();
		name = "Team";
	}

	public override void OnSRPGCKInspectorGUI () {
		//Be sure that changing the team ID checks for duplicates!
		int newID = EditorGUILayout.IntField("ID", team.id);
		if(newID != team.id) {
			if(team.arbiter.GetTeam(newID) != null)
				Debug.LogError("Cannot assign duplicate team ID "+newID);
			else
				team.id = newID;
		}
		team.type = (TeamLocation)EditorGUILayout.EnumPopup("Type", team.type);

		team.parameters = EditorGUIExt.ParameterFoldout(
			"Parameter",
			team.parameters,
			"team."+team.id+".params.",
			formulaOptions,
			lastFocusedControl,
			ref team.editorShowParameters
		);

		if(team.arbiter == null) {
			EditorGUILayout.HelpBox("Teams must be children of Arbiter objects", MessageType.Error);
			return;
		}

		//toggles for ally/enemy by team, excepting self
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.BeginVertical();
		//labels
		GUILayout.Label(  "Team:", GUILayout.Width(64), GUILayout.Height(18));
		GUILayout.Label(   "Ally", GUILayout.Width(64), GUILayout.Height(18));
		GUILayout.Label(  "Enemy", GUILayout.Width(64), GUILayout.Height(18));
		GUILayout.Label("Neutral", GUILayout.Width(64), GUILayout.Height(18));
		EditorGUILayout.EndVertical();

		teamScroll = EditorGUILayout.BeginScrollView(teamScroll, false, false, GUILayout.Height(90));
		EditorGUILayout.BeginHorizontal();
		foreach(var t in team.arbiter.EditorGetTeams()) {
			if(t == team) {
				EditorGUILayout.BeginVertical();
				GUILayout.Label(""+t.id, GUILayout.Width(32), GUILayout.Height(16));
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndVertical();
				continue;
			}
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
			bool ally = team.allies.Contains(t.id);
			bool enemy = !ally && team.enemies.Contains(t.id);
			bool neutral = !ally && !enemy;
			bool newAlly = EditorGUILayout.Toggle(ally, EditorStyles.radioButton, GUILayout.Width(16), GUILayout.Height(16));
			if(newAlly != ally) {
				if(newAlly) { team.allies.Add(t.id); team.enemies.Remove(t.id); }
				else team.allies.Remove(t.id);
			}
			if(newAlly) {
				enemy = false;
				neutral = false;
			}

			bool newEnemy = EditorGUILayout.Toggle(enemy, EditorStyles.radioButton, GUILayout.Width(16), GUILayout.Height(16));
			if(newEnemy != enemy) {
				if(newEnemy) { team.enemies.Add(t.id); team.allies.Remove(t.id); }
				else team.enemies.Remove(t.id);
			}
			if(newEnemy) {
				neutral = false;
			}

			bool newNeutral = EditorGUILayout.Toggle(neutral, EditorStyles.radioButton, GUILayout.Width(16), GUILayout.Height(16));
			if(newNeutral != neutral) {
				if(newNeutral) {
					team.enemies.Remove(t.id);
					team.allies.Remove(t.id);
				}
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
