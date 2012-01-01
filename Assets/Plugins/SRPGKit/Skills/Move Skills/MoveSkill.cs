using UnityEngine;

[System.Serializable]
public class MoveSkill : ActionSkill {
	override public MoveExecutor Executor { get { return moveExecutor; } }
	
	//executor
	[HideInInspector]
	public MoveExecutor moveExecutor;
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
	
	public override void ResetSkill() {
		skillName = "Move";
		skillGroup = "";
		skillSorting=-1;
		effectRegion = new Region();
		effectRegion.IsEffectRegion = true;
		effectRegion.radiusMinF = Formula.Constant(0);
		effectRegion.radiusMaxF = Formula.Constant(0);
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
	
	protected override PathNode[] GetValidActionTiles() {
		if(!lockToGrid) { return null; }
		return targetRegion.GetValidTiles(
			selectedTile, Quaternion.Euler(0, character.Facing, 0),
			targetRegion.radiusMin, targetRegion.radiusMax-radiusSoFar, 
			targetRegion.zDownMin, targetRegion.zDownMax, 
			targetRegion.zUpMin, targetRegion.zUpMax,
			targetRegion.interveningSpaceType
		);
	}
	
	//N.B. for some reason, putting UpdateParameters inside of CreateOverlay -- even with
	//checks to see if the overlay already existed -- caused horrible unity crashers.
	
	override public void ActivateSkill() {
		Executor.owner = this;	
		Executor.lockToGrid = lockToGrid;
		Executor.animateTemporaryMovement = animateTemporaryMovement;
		Executor.XYSpeed = XYSpeed;
		Executor.ZSpeedUp = ZSpeedUp;
		Executor.ZSpeedDown = ZSpeedDown;	
		Executor.Activate();

		base.ActivateSkill();
	}
	
	override public void DeactivateSkill() {
		if(!isActive) { return; }
		Executor.Deactivate();
		base.DeactivateSkill();
	}
	
	public override void ResetActionSkill() {
		overlayColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
		highlightColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);	
		targetingMode = TargetingMode.Custom;
	}
	
	protected override void ActivateTargetCustom() {
	
	}
	
	protected override void UpdateTargetCustom() {

	}
		
	override protected void TemporaryExecutePathTo(PathNode p) {
		TemporaryMoveToPathNode(p);
	}
	
	override protected void IncrementalExecutePathTo(PathNode p) {
		IncrementalExecutePathTo(p);
	}
	
	override protected void ExecutePathTo(PathNode p) {
		PerformMoveToPathNode(p);
	}
}