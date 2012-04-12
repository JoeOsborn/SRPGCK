using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("SRPGCK/Arbiter/Arbiter")]
public class Arbiter : MonoBehaviour {
	//packages up game rules
	public Formulae formulae;
	public Formulae fdb { get { return formulae; } }
	
	public bool IsLocalTeam(int teamID) {
		return GetTeam(teamID).type == TeamLocation.Local;
	}
	public bool IsNetworkTeam(int teamID) {
		return GetTeam(teamID).type == TeamLocation.Network;
	}
	public bool IsAITeam(int teamID) {
		return GetTeam(teamID).type == TeamLocation.AI;
	}

	//Is teamA an enemy/ally/neither of teamB?
	//enemy/ally is not necessarily transitive!
	public bool IsEnemyOf(int teamA, int teamB) {
		return teamA != teamB && GetTeam(teamB).enemies.Contains(teamA);
	}
	public bool IsAllyOf(int teamA, int teamB) {
		return teamA == teamB || GetTeam(teamB).allies.Contains(teamA);
	}
	public bool IsNeutralTo(int teamA, int teamB) {
		return !IsEnemyOf(teamA, teamB) && !IsAllyOf(teamA, teamB);
	}

	private Dictionary<int, Team> _teams;
	private Dictionary<int, Team> teams {
		get {
			if(!Application.isPlaying) {
				Debug.LogError("Do not access teams dict in editor");
				return null;
			}
			if(_teams == null || _teams.Count == 0) {
				_teams = new Dictionary<int, Team>();
				foreach(Team t in GetComponentsInChildren<Team>()) {
					if(_teams.ContainsKey(t.id) && _teams[t.id] != t) {
						Debug.LogError("Duplicate team ID "+t.id);
						_teams = null;
						return null;
					}
				}
			}
			return _teams;
		}
		set {
			_teams = value;
		}
	}

	public Team GetTeam(int team) {
		if(!Application.isPlaying) { return EditorGetTeam(team); }
		return teams[team];
	}
	public void AddTeam(Team t) {
		if(!Application.isPlaying) { return; }
		if(teams.ContainsKey(t.id) && teams[t.id] != t) {
			Debug.LogError("Duplicate team ID "+t.id);
			teams = null;
			return;
		}
		teams[t.id] = t;
	}

	public int EditorGetTeamCount() {
		return GetComponentsInChildren<Team>().Length;
	}
	public Team EditorGetTeam(int team) {
		return GetComponentsInChildren<Team>().FirstOrDefault(t => t.id == team);
	}
	public Team[] EditorGetTeams() {
		return GetComponentsInChildren<Team>().OrderBy(t => t.id).ToArray();
	}

	public virtual bool HasParam(int team, string pname) {
		return GetTeam(team).HasParam(pname);
	}

	public virtual float GetParam(
		int team,
		string pname,
		float fallback=float.NaN,
		SkillDef parentCtx=null
	) {
		return GetTeam(team).GetParam(pname, fallback, parentCtx);
	}

	public virtual void SetParam(int team, string pname, float value) {
		GetTeam(team).SetParam(pname, value);
	}
	public virtual void SetParam(int team, string pname, Formula f) {
		GetTeam(team).SetParam(pname, f);
	}

}
