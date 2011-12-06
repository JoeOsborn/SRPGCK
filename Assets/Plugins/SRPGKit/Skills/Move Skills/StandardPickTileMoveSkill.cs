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
	public GridMoveStrategy moveStrategy;
	public float ZDelta { get { return GetParam("range.z"); } }
	public float XYRange { get { return GetParam("range.xy"); } }
	
	//executor
	[HideInInspector]
	public MoveExecutor moveExecutor;
	public bool animateTemporaryMovement=false;
	public float XYSpeed = 12;
	public float ZSpeedUp = 15;
	public float ZSpeedDown = 20;
	
	public override void Start() {
		base.Start();
		moveIO = new PickTileMoveIO();
		if(moveStrategy == null) {
			moveStrategy = new GridMoveStrategy();
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
		
		moveStrategy.zDelta = ZDelta;
		moveStrategy.xyRange = XYRange;
		
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