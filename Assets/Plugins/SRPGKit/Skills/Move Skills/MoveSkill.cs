using UnityEngine;

[System.Serializable]
public abstract class MoveSkill : Skill {
	public abstract MoveIO IO { get; }
	public abstract MoveStrategy Strategy { get; }
	public abstract MoveExecutor Executor { get; }

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
		Executor.Cancel();
		base.Cancel();
	}
}