using UnityEngine;
using System.Collections.Generic;

public class GridAI : MonoBehaviour {
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
		CTCharacter ctc = c.GetComponent<CTCharacter>();
		if(c.GetComponent<MoveExecutor>().IsMoving) { return; }
		if(!ctc.HasMoved) {
			PickTileMoveIO mio = c.GetComponent<PickTileMoveIO>();
			PathNode[] dests = mio.overlay.destinations;
			mio.PerformMoveToPathNode(dests[(int)Mathf.Floor(Random.Range(0, dests.Length))]);
		} else {
			GetComponent<Scheduler>().Deactivate(c);
		}
	}
}
