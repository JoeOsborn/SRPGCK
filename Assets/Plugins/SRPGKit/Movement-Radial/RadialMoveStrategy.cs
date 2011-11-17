using UnityEngine;
using System.Collections.Generic;

public class RadialMoveStrategy : MoveStrategy {
	public float GetMoveRadius() {
		return xyRange;
		//scheduler.limiter / SchedulerTurnMoveAPCost
	}
	public float GetJumpHeight() {
		return zDelta;
	}
}
