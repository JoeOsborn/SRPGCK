using UnityEngine;

[System.Serializable]
public class StandardPickTileMoveSkill : MoveSkill {	
	//io
	[HideInInspector]
	public PickTileMoveIO moveIO;
	public bool supportKeyboard = true;	
	public bool supportMouse = true;
	public bool requireConfirmation = true;
	public float indicatorCycleLength=1.0f;
	
	//strategy
	public ActionStrategy moveStrategy;
	public float ZDelta { get { return GetParam("range.z", Formula.Lookup("jump", LookupType.ActorStat).GetValue(this, null, null)); } }
	public float XYRange { get { return GetParam("range.xy", Formula.Lookup("move", LookupType.ActorStat).GetValue(this, null, null)); } }
	
	//executor
	[HideInInspector]
	public MoveExecutor moveExecutor;
	public bool animateTemporaryMovement=false;
	public float XYSpeed = 12;
	public float ZSpeedUp = 15;
	public float ZSpeedDown = 20;
	
	public override MoveIO IO { get {
		return moveIO;
	} }
	public override ActionStrategy Strategy { get {
		return moveStrategy;
	} }
	public override MoveExecutor Executor { get {
		return moveExecutor;
	} }
	
	public override void Start() {
		base.Start();
		moveIO = new PickTileMoveIO();
		if(moveStrategy == null) {
			moveStrategy = new ActionStrategy();
		}
		moveExecutor = new MoveExecutor();
		moveIO.owner = this;
		moveStrategy.owner = this;
		moveExecutor.owner = this;
	}
	
	public override void ActivateSkill() {
		if(isActive) { return; }
		base.ActivateSkill();
		moveIO.owner = this;
		moveStrategy.owner = this;
		moveExecutor.owner = this;

		if(moveIO == null) { moveIO = new PickTileMoveIO(); moveIO.owner = this; }
		moveIO.supportKeyboard = supportKeyboard;
		moveIO.supportMouse = supportMouse;
		moveIO.requireConfirmation = requireConfirmation;
		moveIO.indicatorCycleLength = indicatorCycleLength;
		
		moveStrategy.zRangeDownMin = 0;
		moveStrategy.zRangeDownMax = ZDelta;
		moveStrategy.zRangeUpMin = 0;
		moveStrategy.zRangeUpMax = ZDelta;
		moveStrategy.xyRangeMin = 0;
		moveStrategy.xyRangeMax = XYRange;
		
		moveExecutor.animateTemporaryMovement = animateTemporaryMovement;
		moveExecutor.XYSpeed = XYSpeed;
		moveExecutor.ZSpeedUp = ZSpeedUp;
		moveExecutor.ZSpeedDown = ZSpeedDown;
		
		moveIO.Activate();
		moveStrategy.Activate();
		moveExecutor.Activate();
		
		//present moves
		moveIO.PresentMoves();
	}	
	public override void DeactivateSkill() {
		if(!isActive) { return; }
		moveIO.Deactivate();
		moveStrategy.Deactivate();
		moveExecutor.Deactivate();
		base.DeactivateSkill();
	}
	public override void Update() {
		base.Update();
		if(!isActive) { return; }
		moveIO.owner = this;
		moveStrategy.owner = this;
		moveExecutor.owner = this;
		moveIO.Update();
		moveStrategy.Update();
		moveExecutor.Update();	
	}
}