using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RadialMoveStrategy : ActionStrategy {
	//TODO: FIX ME!!
	public float GetMoveRadius() {
		return xyRangeMax;
		//scheduler.limiter / SchedulerTurnMoveAPCost
	}
	public float GetJumpHeight() {
		return zRangeUpMax;
	}
}
