using UnityEngine;
using System.Collections.Generic;

public class TeamPhasedPickAnyOnceScheduler : Scheduler {
	public int teamCount=2;

	public int currentTeam=0;
	
	public List<Character> remainingCharacters;

	override public void Start () {
		base.Start();
		foreach(Character c in characters) {
			if(c.GetEffectiveTeamID() == currentTeam) {
				remainingCharacters.Add(c);
				c.SendMessage("BeginTurn", currentTeam, SendMessageOptions.DontRequireReceiver);
			}
		}
	}
	
	public void EndPhase() {
		if(activeCharacter != null) {
			Deactivate(activeCharacter);
		}
		foreach(Character c in characters) {
			c.SendMessage("EndPhase", currentTeam, SendMessageOptions.DontRequireReceiver);
		}
		currentTeam++;
		if(currentTeam >= teamCount) {
			currentTeam = 0;
		}
		remainingCharacters.Clear();
		foreach(Character c in characters ){
			if(c.GetEffectiveTeamID() == currentTeam) {
				remainingCharacters.Add(c);
			}
			c.SendMessage("BeginPhase", currentTeam, SendMessageOptions.DontRequireReceiver);
		}
	}

	override public void EndMovePhase(Character c) {
		Deactivate(c);
	}
	
	public void OnGUI() {
		GUILayout.BeginArea(new Rect(
			8, 8, 
			96, 128
		));
		GUILayout.Label("Current Team:"+currentTeam);
		if(GUILayout.Button("End Phase")) {
			EndPhase();
		}
		GUILayout.EndArea();
	}
	
	override public void Update () {
		base.Update();
		if(activeCharacter == null) {
			if(remainingCharacters.Count == 0) {
				EndPhase();
			}
		}
		if(Input.GetMouseButtonDown(0)) {
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit[] hits = Physics.RaycastAll(r);
			float closestDistance = Mathf.Infinity;
			Character closestCharacter = null;
			foreach(RaycastHit h in hits) {
				Character thisChar = h.transform.GetComponent<Character>();
				if(thisChar!=null && remainingCharacters.Contains(thisChar)) {
					if(h.distance < closestDistance) {
						closestDistance = h.distance;
						closestCharacter = h.transform.GetComponent<Character>();
					}
				}
			}
			if(closestCharacter != null && !closestCharacter.isActive && closestCharacter.GetEffectiveTeamID() == currentTeam) {
				Activate(closestCharacter);
				remainingCharacters.Remove(closestCharacter);
			}
		}
	}
}
