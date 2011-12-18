using UnityEngine;

[System.Serializable]
public class MoveSkill : ActionSkill {
	override public MoveExecutor Executor { get { return moveExecutor; } }
	
	//strategy
	public float ZDelta { get { return GetParam("range.z", character.GetStat("jump", 3)); } }
	public float XYRange { get { return GetParam("range.xy", character.GetStat("move", 5)); } }
	
	//executor
	[HideInInspector]
	public MoveExecutor moveExecutor;
	public bool performTemporaryMoves = false;
	public bool animateTemporaryMovement=false;
	public float XYSpeed = 12;
	public float ZSpeedUp = 15;
	public float ZSpeedDown = 20;
	
	public override void Start() {
		base.Start();
		moveExecutor = new MoveExecutor();
		Executor.lockToGrid = lockToGrid;
		Executor.owner = this;
	}
	
	protected override void SetupStrategy() {
		//N.B.: don't use superclass implementation
		Strategy.zRangeDownMin = 0;
		Strategy.zRangeDownMax = ZDelta;
		Strategy.zRangeUpMin = 0;
		Strategy.zRangeUpMax = ZDelta;
		Strategy.xyRangeMin = 0;
		Strategy.xyRangeMax = XYRange;
	}

	public override void ResetSkill() {
		skillName = "Move";
		skillGroup = "";
		skillSorting=-1;
	}
	public override void ResetActionSkill() {
		overlayColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
		highlightColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);	
	}
	
	public override void ActivateSkill() {
		Executor.owner = this;	
		Executor.lockToGrid = lockToGrid;
		Executor.animateTemporaryMovement = animateTemporaryMovement;
		Executor.XYSpeed = XYSpeed;
		Executor.ZSpeedUp = ZSpeedUp;
		Executor.ZSpeedDown = ZSpeedDown;	
		Executor.Activate();

		base.ActivateSkill();
	}	
	
	protected override PathNode[] GetValidActionTiles() {
		return strategy.GetValidMoves();
	}

	public override void DeactivateSkill() {
		if(!isActive) { return; }
		Executor.Deactivate();
		base.DeactivateSkill();
	}
	
	public override void Update() {
		if(!isActive) { return; }
		Executor.owner = this;
		base.Update();
		Executor.Update();	
	}

	public override void Cancel() {
		if(!isActive) { return; }
		Executor.Cancel();
		base.Cancel();
	}
	
	public virtual void TemporaryMove(Vector3 tc) {
		TemporaryMoveToPathNode(new PathNode(tc, null, 0));
	}

	public virtual void IncrementalMove(Vector3 tc) {
		IncrementalMoveToPathNode(new PathNode(tc, null, 0));
	}
	
	public virtual void PerformMove(Vector3 tc) {
		PerformMoveToPathNode(new PathNode(tc, null, 0));
	}
	
	public virtual void TemporaryMoveToPathNode(PathNode pn) {
		MoveExecutor me = Executor;
		me.TemporaryMoveTo(pn, delegate(Vector3 src, PathNode endNode, bool finishedNicely) {
			scheduler.CharacterMovedTemporary(
				character, 
				map.InverseTransformPointWorld(src), 
				map.InverseTransformPointWorld(endNode.pos)
			);
		});
	}

	public virtual void IncrementalMoveToPathNode(PathNode pn) {
		MoveExecutor me = Executor;
		me.IncrementalMoveTo(pn, delegate(Vector3 src, PathNode endNode, bool finishedNicely) {
/*			Debug.Log("moved from "+src);*/
			scheduler.CharacterMovedIncremental(
				character, 
				src, 
				endNode.pos
			);
		});
	}
	
	public virtual void PerformMoveToPathNode(PathNode pn) {
		MoveExecutor me = Executor;
		me.MoveTo(pn, delegate(Vector3 src, PathNode endNode, bool finishedNicely) {
			scheduler.CharacterMoved(
				character, 
				map.InverseTransformPointWorld(src), 
				map.InverseTransformPointWorld(endNode.pos)
			);
			ApplySkill();
		});
	}
}