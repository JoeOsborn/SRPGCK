using UnityEngine;

[System.Serializable]
public class MoveSkill : Skill {
	[HideInInspector]
	public MoveIO io;
	[HideInInspector]
	public MoveStrategy strategy;
	[HideInInspector]
	public MoveExecutor executor;
	public override void Start() {
		base.Start();
	}
	public override void Reset() {
		base.Reset();
		skillName = "Move";
		skillSorting = -1;
	}
	public override void Cancel() {
		if(!isActive) { return; }
		executor.Cancel();
		base.Cancel();
	}
}