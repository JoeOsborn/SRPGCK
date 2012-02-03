using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Initiative {
	public Character character;
	public float initiative;
	public Initiative(Character c, float i) {
		character = c;
		initiative = i;
	}
}

public class RoundsInitiativeScheduler : Scheduler {
	public List<Initiative> order;

	override public void Start () {
		base.Start();
		order = new List<Initiative>();
	}

	public void BeginRound() {
		order.Clear();
		// Debug.Log("begin round with "+characters.Count+" characters");
		for(int i = 0; i < characters.Count; i++) {
			Character c = characters[i];
			float ini = c.GetStat("initiative");
			Debug.Log("character "+c.name+" ini:"+ini);
			order.Add(new Initiative(c, ini));
		}
		order = order.OrderBy(i => i.initiative).ToList();
		map.BroadcastMessage("RoundBegan", SendMessageOptions.DontRequireReceiver);
	}

	public void EndRound() {
		// Debug.Log("end round");
		map.BroadcastMessage("RoundEnded", SendMessageOptions.DontRequireReceiver);
		BeginRound();
	}

	override public void SkillApplied(Skill s) {
		base.SkillApplied(s);
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

	override public void Update () {
		base.Update();
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
