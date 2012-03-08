using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("SRPGCK/Arbiter/Scheduler/Team Rounds with Initiative")]
public class TeamRoundsInitiativeScheduler : Scheduler {
	public List<Initiative> order;
	public int teamCount=2;
	public int currentTeam=0;
	public float activeInitiative=-1;

	override public void Start () {
		base.Start();
		order = new List<Initiative>();
	}

	public void BeginRound() {
		order.Clear();
		// Debug.Log("begin round with "+characters.Count+" characters");
		var teamChars = characters.
			Where(c => c.EffectiveTeamID == currentTeam).ToList();
		for(int i = 0; i < teamChars.Count; i++) {
			Character c = teamChars[i];
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
		currentTeam++;
		if(currentTeam >= teamCount) {
			currentTeam = 0;
		}
		BeginRound();
	}

	public override void ApplySkillAfterDelay(Skill s, List<Target> ts, float delay) {
		base.ApplySkillAfterDelay(s, ts, activeInitiative - delay);
	}

	override public void SkillApplied(Skill s) {
		base.SkillApplied(s);
	}

	override public void Activate(Character c, object ctx=null) {
		base.Activate(c, ctx);
	}

	protected override void Begin() {
		base.Begin();
		BeginRound();
	}

	override public void FixedUpdate () {
		base.FixedUpdate();
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
			if(order.Count == 0) {
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
