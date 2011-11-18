using UnityEngine;
using System.Collections;

[System.Serializable]
public class WaitSkill : Skill {
	public bool supportKeyboard = true;
	public bool supportMouse = true;
	public bool requireConfirmation = true;
	
	public GameObject waitArrows;
	
	public Quaternion initialFacing;

	public WaitIO io;
	
	public override void Start() {
		base.Start();
		this.name = "Wait";
		if(waitArrows == null) {
			waitArrows = Resources.Load("Wait Arrows") as GameObject;
		}
		io = new WaitIO();
		io.owner = this;
		io.waitArrows = waitArrows;
		io.Start();
/*		strategy = new GridMoveStrategy();
		executor = new MoveExecutor();
		io.owner = this;
		strategy.owner = this;
		executor.owner = this;
		io.Start();
		strategy.Start();
		executor.Start();*/
	}
	
	public virtual void CancelWaitPick() {
		Cancel();
	}
	
	public override void Activate() {
		base.Activate();
		initialFacing = character.Facing;
		io.waitArrows = waitArrows;
		io.owner = this;
		io.supportKeyboard = supportKeyboard;
		io.supportMouse = supportMouse;
		io.RequireConfirmation = requireConfirmation;
		io.Activate();
/*		io.owner = this;
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
		executor.Activate();*/
		
		//present moves
/*		io.PresentMoves();*/
	}	
	public override void Deactivate() {
		base.Deactivate();
		io.Deactivate();
/*		io.Deactivate();
		strategy.Deactivate();
		executor.Deactivate();
*/	}
	public override void Update() {
		base.Update();
		io.Update();
/*		io.Update();
		strategy.Update();
		executor.Update();	
*/}
	public override void Cancel() {
		//switch to idle animation
/*		io.Cancel();*/
		WaitInDirection(initialFacing);
		base.Cancel();
	}
	public virtual void WaitInDirection(Quaternion dir) {
		character.Facing = dir;
	}
	public virtual void FinishWaitPick() {
		if(isActive) {
			map.BroadcastMessage("SkillApplied", this, SendMessageOptions.DontRequireReceiver);
		}
		this.Deactivate();
	}
}
