using UnityEngine;

[System.Serializable]
public class MoveSkill : Skill {
	public MoveIO io;
	public MoveStrategy strategy;
	public MoveExecutor executor;
	public override void Cancel() {
		executor.Cancel();
		base.Cancel();
	}
}