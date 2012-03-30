using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("SRPGCK/Arbiter/Scheduler/Rounds with Initiative")]
public class RoundsInitiativeScheduler : Scheduler {
	public List<Initiative> order;
	public float activeInitiative=-1;

	override public void Start () {
		base.Start();
		order = new List<Initiative>();
	}

	public void BeginRound() {
		order.Clear();
		activeInitiative=-1;
		// Debug.Log("begin round with "+characters.Count+" characters");
		for(int i = 0; i < characters.Count; i++) {
			Character c = characters[i];
			float ini = c.GetStat("initiative");
			Debug.Log("character "+c.name+" ini:"+ini);
			order.Add(new Initiative(c, ini));
		}
		order = order.OrderByDescending(i => i.initiative).ToList();
		map.BroadcastMessage("RoundBegan", SendMessageOptions.DontRequireReceiver);
	}

	public void EndRound() {
		// Debug.Log("end round");
		map.BroadcastMessage("RoundEnded", SendMessageOptions.DontRequireReceiver);
		BeginRound();
	}

	public override void ApplySkillAfterDelay(SkillDef s, Vector3? start, List<Target> ts, float delay) {
		base.ApplySkillAfterDelay(s, start, ts, activeInitiative - delay);
	}

	override public void SkillApplied(SkillDef s) {
		base.SkillApplied(s);
		if(s.character != activeCharacter) { return; }
		//FIXME: too eager, put something in the UI
		Deactivate(s.character);
	}

	override public void Activate(Character c, object ctx=null) {
		base.Activate(c, ctx);
		//FIXME: (for now): ON `activate`, MOVE
		c.moveSkill.ActivateSkill();
	}

	protected override void Begin() {
		base.Begin();
		BeginRound();
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
			float highestInit = -1;
			SkillActivation nowSA = null;
			foreach(SkillActivation sa in pendingSkillActivations) {
				if(sa.delay > highestInit) {
					highestInit = sa.delay;
					nowSA = sa;
				}
			}
			if(order.Count > 0 && order[0].initiative > highestInit) {
				highestInit = order[0].initiative;
				nowSA = null;
			}
			activeInitiative = highestInit;
			if(order.Count == 0 && nowSA == null) {
				EndRound();
			} else {
				if(nowSA == null) {
					Initiative i = order[0];
					order.RemoveAt(0);
					Activate(i.character);
				} else {
					nowSA.Apply();
					pendingSkillActivations.Remove(nowSA);
				}
			}
		}
	}
}
