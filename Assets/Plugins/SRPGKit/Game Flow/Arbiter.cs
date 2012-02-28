using UnityEngine;
using System;
using System.Collections;

public enum TeamLocation {
	Local,
	AI,
	Network
}

public class Arbiter : MonoBehaviour {
	//packages up game rules
	//for now, just maps players (network, AI, local) to teamIDs

	public TeamLocation[] teams;

	public void Awake() {
		teams = new TeamLocation[]{TeamLocation.Local, TeamLocation.Local};
	}

	public Formulae formulae;

	public bool IsLocalTeam(int teamID) {
		return teams[teamID] == TeamLocation.Local;
	}
	public bool IsNetworkTeam(int teamID) {
		return teams[teamID] == TeamLocation.Network;
	}
	public bool IsAITeam(int teamID) {
		return teams[teamID] == TeamLocation.AI;
	}
}
