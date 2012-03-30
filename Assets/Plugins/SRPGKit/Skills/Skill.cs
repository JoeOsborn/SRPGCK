using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("SRPGCK/Character/Skill")]
public class Skill : MonoBehaviour {
	public SkillDef def;
	protected ActionSkillDef actionDef { get { return def as ActionSkillDef; } }
	protected MoveSkillDef moveDef { get { return def as MoveSkillDef; } }
	protected WaitSkillDef waitDef { get { return def as WaitSkillDef; } }

	public bool IsActionSkill { get { return def is ActionSkillDef; } }
	public bool IsMoveSkill { get { return def is MoveSkillDef; } }
	public bool IsWaitSkill { get { return def is WaitSkillDef; } }

	public bool isPassive { get { return def.isPassive; } }
	public MoveExecutor Executor { get { return actionDef.Executor; } }

	public void Awake() {
		//make sure we don't mess with the real asset
		if(def != null && def.reallyDefined) {
			def = Instantiate(def) as SkillDef;
			def.Owner = this;
		}
	}

	//generic skill stuff

	public void Start() {
		def.Start();
	}
	public void ActivateSkill() {
		def.ActivateSkill();
	}
	public void DeactivateSkill() {
		def.DeactivateSkill();
	}
	public void Update() {
		def.Update();
	}
	public void Cancel() {
		def.Cancel();
	}
	public void ApplySkill() {
		def.ApplySkill();
	}

	//action skill stuff

	public bool RequireConfirmation { get {
		return actionDef.RequireConfirmation;
	} }

	public bool AwaitingConfirmation {
		get { return actionDef.AwaitingConfirmation; }
		set { actionDef.AwaitingConfirmation = value; }
	}

	public Overlay Overlay { get { return actionDef.Overlay; } }

	public virtual void IncrementalCancel() {
		actionDef.IncrementalCancel();
	}

	public void DelayedApply(Vector3? start, List<Target> targs) {
		actionDef.DelayedApply(start, targs);
	}

	public void ConfirmDelayedSkillTarget(TargetOption tgo) {
		actionDef.ConfirmDelayedSkillTarget(tgo);
	}

	public bool AwaitingTargetOption { get { return actionDef.AwaitingTargetOption; } }

	virtual public void TemporaryApplyCurrentTarget() {
		actionDef.TemporaryApplyCurrentTarget();
	}
	virtual public void IncrementalApplyCurrentTarget() {
		actionDef.IncrementalApplyCurrentTarget();
	}
	virtual public void ApplyCurrentTarget() {
		actionDef.ApplyCurrentTarget();
	}

	//move skill stuff

	public virtual void TemporaryMove(Vector3 tc) {
		moveDef.TemporaryMove(tc);
	}

	public virtual void IncrementalMove(Vector3 tc) {
		moveDef.IncrementalMove(tc);
	}

	public virtual void PerformMove(Vector3 tc) {
		moveDef.PerformMove(tc);
	}

	public virtual void TemporaryMoveToPathNode(
		PathNode pn,
		MoveExecutor.MoveFinished callback=null
	) {
		moveDef.TemporaryMoveToPathNode(pn, callback);
	}

	public virtual void IncrementalMoveToPathNode(
		PathNode pn,
		MoveExecutor.MoveFinished callback=null
	) {
		moveDef.IncrementalMoveToPathNode(pn, callback);
	}

	public virtual void PerformMoveToPathNode(
		PathNode pn,
		MoveExecutor.MoveFinished callback=null
	) {
		moveDef.PerformMoveToPathNode(pn, callback);
	}

}