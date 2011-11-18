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
	public float ZDelta=3;
	public float XYRange=3;
	public bool canCrossEnemies=false;
	
	//executor
	public Vector3 transformOffset = new Vector3(0, 5, 0);
	public bool animateTemporaryMovement=false;
	public float XYSpeed = 12;
	public float ZSpeedUp = 15;
	public float ZSpeedDown = 20;
	
	public override void Start() {
		base.Start();
		this.name = "Move";
		io = new PickTileMoveIO();
		strategy = new GridMoveStrategy();
		executor = new MoveExecutor();
		io.owner = this;
		strategy.owner = this;
		executor.owner = this;
		io.Start();
		strategy.Start();
		executor.Start();
	}
	
	public override void Activate() {
		base.Activate();
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
		
		executor.transformOffset = transformOffset;
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
	public override void Deactivate() {
		base.Deactivate();
		io.Deactivate();
		strategy.Deactivate();
		executor.Deactivate();
	}
	public override void Update() {
		base.Update();
		io.Update();
		strategy.Update();
		executor.Update();	
	}
}