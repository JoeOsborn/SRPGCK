using UnityEngine;

[System.Serializable]
public class StandardPickTileMoveSkill : MoveSkill {	
	//io
	public bool supportKeyboard = true;	
	public bool supportMouse = true;
	public bool requireConfirmation = true;
	public float indicatorCycleLength=1.0f;
	
	//strategy
	public bool canCrossWalls=false;
	public float ZDelta { get { return GetParam("range.z"); } }
	public float XYRange { get { return GetParam("range.xy"); } }
	public bool canCrossEnemies=false;
	
	//executor
	public bool animateTemporaryMovement=false;
	public float XYSpeed = 12;
	public float ZSpeedUp = 15;
	public float ZSpeedDown = 20;
	
	public override void Start() {
		base.Start();
		io = new PickTileMoveIO();
		strategy = new GridMoveStrategy();
		executor = new MoveExecutor();
		io.owner = this;
		strategy.owner = this;
		executor.owner = this;
	}
	
	public override void ActivateSkill() {
		if(isActive) { return; }
		base.ActivateSkill();
		io.owner = this;
		strategy.owner = this;
		executor.owner = this;

		PickTileMoveIO mio = io as PickTileMoveIO;
		mio.supportKeyboard = supportKeyboard;
		mio.supportMouse = supportMouse;
		mio.requireConfirmation = requireConfirmation;
		mio.indicatorCycleLength = indicatorCycleLength;
		
		strategy.canCrossWalls = canCrossWalls;
		strategy.zDelta = ZDelta;
		strategy.xyRange = XYRange;
		strategy.canCrossEnemies = canCrossEnemies;
		
		executor.animateTemporaryMovement = animateTemporaryMovement;
		executor.XYSpeed = XYSpeed;
		executor.ZSpeedUp = ZSpeedUp;
		executor.ZSpeedDown = ZSpeedDown;
		
		io.Activate();
		strategy.Activate();
		executor.Activate();
		
		//present moves
		io.PresentMoves();
	}	
	public override void DeactivateSkill() {
		if(!isActive) { return; }
		io.Deactivate();
		strategy.Deactivate();
		executor.Deactivate();
		base.DeactivateSkill();
	}
	public override void Update() {
		base.Update();
		if(!isActive) { return; }
		io.owner = this;
		strategy.owner = this;
		executor.owner = this;
		io.Update();
		strategy.Update();
		executor.Update();	
	}
}