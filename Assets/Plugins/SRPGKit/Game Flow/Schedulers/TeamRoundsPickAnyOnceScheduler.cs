using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("SRPGCK/Arbiter/Scheduler/Team Rounds Pick Any Once")]
public class TeamRoundsPickAnyOnceScheduler : Scheduler {
	public int teamCount=2;

	public int currentTeam=0;

	public List<Character> remainingCharacters;

	override public void Start () {
		base.Start();
		foreach(Character c in characters) {
			if(c.EffectiveTeamID == currentTeam) {
				remainingCharacters.Add(c);
			}
		}
	}

	public void EndRound() {
		if(activeCharacter != null) {
			Deactivate(activeCharacter);
		}
		map.BroadcastMessage("RoundEnded", currentTeam, SendMessageOptions.DontRequireReceiver);
		currentTeam++;
		if(currentTeam >= teamCount) {
			currentTeam = 0;
		}
		remainingCharacters.Clear();
		foreach(Character c in characters) {
			if(c.EffectiveTeamID == currentTeam) {
				remainingCharacters.Add(c);
			}
		}
		map.BroadcastMessage("RoundBegan", currentTeam, SendMessageOptions.DontRequireReceiver);
	}

	override public void SkillApplied(SkillDef s) {
		base.SkillApplied(s);
	//	Deactivate(s.character);
	}

	override public void Activate(Character c, object ctx=null) {
		base.Activate(c, ctx);
		remainingCharacters.Remove(c);
//		c.moveSkill.ActivateSkill();
	}
	public void WillSpecialMoveCharacter(CharacterSpecialMoveReport csmr) {
		Pause();
	}
	public void DidSpecialMoveCharacter(CharacterSpecialMoveReport csmr) {
		Resume();
	}

	override public void FixedUpdate() {
		base.FixedUpdate();
		if(paused) { return; }
		if(activeCharacter == null) {
			if(remainingCharacters.Count == 0) {
				EndRound();
			}
		}
	}
	public void Update() {
		if(paused) { return; }
		if(activeCharacter != null) { return; }
		if(GetComponent<Arbiter>().IsLocalTeam(currentTeam) &&
		   Input.GetMouseButtonDown(0)) {
			//TODO: need another caller for Activate()
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
			if(closestCharacter != null &&
			   !closestCharacter.isActive &&
			   closestCharacter.EffectiveTeamID == currentTeam) {
				Activate(closestCharacter);
			}
		}
	}
}
