using UnityEngine;
using System;
using System.Collections;

public class Arbiter : MonoBehaviour {
	//packages up game rules
	//for now, just maps players (network, AI, local) to teamIDs

	public int[] localPlayers, aiPlayers, networkPlayers;

	public Formulae formulae;

	public bool IsLocalPlayer(int teamID) {
		return Array.IndexOf(localPlayers, teamID) != -1;
	}
	public bool IsNetworkPlayer(int teamID) {
		return Array.IndexOf(networkPlayers, teamID) != -1;
	}
	public bool IsAIPlayer(int teamID) {
		return Array.IndexOf(aiPlayers, teamID) != -1;
	}
}
