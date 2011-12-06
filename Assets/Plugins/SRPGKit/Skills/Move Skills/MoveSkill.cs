using UnityEngine;

[System.Serializable]
public class MoveSkill : Skill {
	public MoveIO IO { get {
		return null;
	} }
	public MoveStrategy Strategy { get {
		return null;
	} }
	public MoveExecutor Executor { get {
		return null;
	} }
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