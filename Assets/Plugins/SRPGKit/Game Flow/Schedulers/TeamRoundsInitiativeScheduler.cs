using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TeamRoundsInitiativeScheduler : Scheduler {
	public List<Initiative> order;
	public int teamCount=2;
	public int currentTeam=0;

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
			if(order.Count == 0) {
				EndRound();
			} else {
				Initiative i = order[0];
				order.RemoveAt(0);
				Activate(i.character);
			}
		}
	}
}
