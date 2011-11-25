using UnityEngine;

[System.Serializable]
public class MoveSkill : Skill {
	public MoveIO io;
	public MoveStrategy strategy;
	public MoveExecutor executor;
	public override void Start() {
		base.Start();
		skillName = "Move";
	}
	public override void Cancel() {
		if(!isActive) { return; }
		executor.Cancel();
		base.Cancel();
	}
}