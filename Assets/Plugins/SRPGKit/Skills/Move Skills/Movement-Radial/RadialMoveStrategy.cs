using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RadialMoveStrategy : MoveStrategy {
	public float GetMoveRadius() {
		return xyRange;
		//scheduler.limiter / SchedulerTurnMoveAPCost
	}
	public float GetJumpHeight() {
		return zDelta;
	}
}
