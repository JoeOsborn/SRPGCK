using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("SRPGCK/Arbiter/AI/Grid")]
public class GridAI : AI {
	public void Start () {

	}
/*	public void CharacterActivated() {

	}
	public void CharacterDeactivated() {

	}
	public void RoundBegan() {

	}
	public void RoundEnded() {

	}*/
	public void Update() {
		//we'll need different ones of these later for team phased vs ct, I'm sure
		Character c = GetComponent<Scheduler>().activeCharacter;
		if(c == null || !GetComponent<Arbiter>().IsAITeam(c.EffectiveTeamID)) {
			return;
		}
		if(c.moveSkill.Executor.IsMoving) { return; }
		CTScheduler cts = GetComponent<Scheduler>() as CTScheduler;
		if(cts != null) {
			if(!cts.activeCharacterHasMoved) {
				c.moveSkill.ActivateSkill();
				MoveSkillDef sptms = c.moveSkill;
				if(sptms.Overlay != null) {
					PathNode[] dests = (sptms.Overlay as GridOverlay).destinations;
					if(dests.Length == 0) {
						c.moveSkill.Cancel();
						c.waitSkill.ActivateSkill();
						Quaternion dir=Quaternion.Euler(0,0,0);
						const float TAU = 360.0f;
						switch((int)Mathf.Floor(Random.Range(0, 4))) {
							case 0: dir = Quaternion.Euler(0, 0*TAU/4, 0); break;
							case 1: dir = Quaternion.Euler(0, 1*TAU/4, 0); break;
							case 2: dir = Quaternion.Euler(0, 2*TAU/4, 0); break;
							case 3: dir = Quaternion.Euler(0, 3*TAU/4, 0); break;
						}
						c.waitSkill.PickFacing(dir);
					} else {
						c.moveSkill.PerformMoveToPathNode(dests[(int)Mathf.Floor(Random.Range(0, dests.Length))]);
					}
				}
			} else {
				c.waitSkill.ActivateSkill();
				Quaternion dir=Quaternion.Euler(0,0,0);
				const float TAU = 360.0f;
				switch((int)Mathf.Floor(Random.Range(0, 4))) {
					case 0: dir = Quaternion.Euler(0, 0*TAU/4, 0); break;
					case 1: dir = Quaternion.Euler(0, 1*TAU/4, 0); break;
					case 2: dir = Quaternion.Euler(0, 2*TAU/4, 0); break;
					case 3: dir = Quaternion.Euler(0, 3*TAU/4, 0); break;
				}
				c.waitSkill.PickFacing(dir);
			}
		} else {
			c.moveSkill.ActivateSkill();
			MoveSkillDef ms = c.moveSkill;
			if(ms.Overlay != null) {
				PathNode[] dests = (ms.Overlay as GridOverlay).destinations;
				if(dests.Length == 0) {
					c.moveSkill.Cancel();
				} else {
					c.moveSkill.PerformMoveToPathNode(
						dests[(int)Mathf.Floor(Random.Range(0, dests.Length))]
					);
				}
				c.waitSkill.ActivateSkill();
				Quaternion dir=Quaternion.Euler(0,0,0);
				const float TAU = 360.0f;
				switch((int)Mathf.Floor(Random.Range(0, 4))) {
					case 0: dir = Quaternion.Euler(0, 0*TAU/4, 0); break;
					case 1: dir = Quaternion.Euler(0, 1*TAU/4, 0); break;
					case 2: dir = Quaternion.Euler(0, 2*TAU/4, 0); break;
					case 3: dir = Quaternion.Euler(0, 3*TAU/4, 0); break;
				}
				c.waitSkill.PickFacing(dir);
			}
		}
	}
}
