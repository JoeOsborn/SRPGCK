using UnityEngine;
using System.Collections.Generic;

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
		if(c == null || !GetComponent<Arbiter>().IsAIPlayer(c.EffectiveTeamID)) {
			return;
		}
		if(c.moveSkill.Executor.IsMoving) { return; }
		if(!c.GetComponent<CTCharacter>().HasMoved) {
			c.moveSkill.ActivateSkill();
			PickTileMoveIO mio = c.moveSkill.IO as PickTileMoveIO;
			if(mio != null && mio.overlay != null) {
				PathNode[] dests = mio.overlay.destinations;
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
					c.waitSkill.WaitInDirection(dir);
					c.waitSkill.FinishWaitPick();
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
			c.waitSkill.WaitInDirection(dir);
			c.waitSkill.FinishWaitPick();
		}
	}
}
