using UnityEngine;
using System.Collections;

public class AttackSkill : Skill {
	//strategy
	public bool canCrossWalls=false;
	public bool canEffectCrossWalls=true;

	//tile generation strategy (line/range/cone/etc)
	[HideInInspector]
	public ActionStrategy strategy;

	//io
	public bool supportKeyboard = true;	
	public bool supportMouse = true;
	public bool requireConfirmation = true;
	public float indicatorCycleLength=1.0f;

	//target selection IO (range/radius/line/self/all enemies/etc, some of which is subclass responsibility)
		//which tiles (and which should be previewed) will be provided by the tile generation strategy
	[HideInInspector]
	public ActionIO io;
		
	//effect parameters (elements, w/ev)
	//effect
	//animated action (executor?)

	//knockback (executor on defender? move executor tie-in?)
	//target reaction skills and support skills
	
	public override void Start() {
		base.Start();
		io = new ActionIO();
		strategy = new ActionStrategy();
		io.owner = this;
		strategy.owner = this;
		/*
		executor = new MoveExecutor();
		executor.owner = this;
		*/
	}
	public override void Reset() {
		base.Reset();
		skillName = "Attack";
	}
	public override void Cancel() {
		if(!isActive) { return; }
		//executor.Cancel();
		base.Cancel();
	}
	
	public override void ActivateSkill() {
		if(isActive) { return; }
		base.ActivateSkill();
		io.owner = this;
		strategy.owner = this;
		
		io.supportKeyboard = supportKeyboard;
		io.supportMouse = supportMouse;
		io.requireConfirmation = requireConfirmation;
		io.indicatorCycleLength = indicatorCycleLength;
		
		strategy.canCrossWalls = canCrossWalls;
		strategy.zRangeUpMin = GetParam("range.z.up.min");
		strategy.zRangeUpMax = GetParam("range.z.up.max");
		strategy.zRangeDownMin = GetParam("range.z.down.min");
		strategy.zRangeDownMax = GetParam("range.z.down.max");
		strategy.xyRangeMin = GetParam("range.xy.min");
		strategy.xyRangeMax = GetParam("range.xy.max");

		strategy.canEffectCrossWalls = canEffectCrossWalls;
		strategy.zRadiusUp = GetParam("radius.z.up");
		strategy.zRadiusDown = GetParam("radius.z.down");
		strategy.xyRadius = GetParam("radius.xy");

		/*	
		executor.owner = this;
		
		executor.transformOffset = transformOffset;
		executor.animateTemporaryMovement = animateTemporaryMovement;
		executor.XYSpeed = XYSpeed;
		executor.ZSpeedUp = ZSpeedUp;
		executor.ZSpeedDown = ZSpeedDown;
    
		executor.Activate();
		*/	
		
		io.Activate();
		strategy.Activate();
		
		io.PresentMoves();
	}	
	public override void DeactivateSkill() {
		if(!isActive) { return; }
		io.Deactivate();
		strategy.Deactivate();
		//executor.Deactivate();
		base.DeactivateSkill();
	}
	public override void Update() {
		base.Update();
		if(!isActive) { return; }
		io.owner = this;
		strategy.owner = this;
		io.Update();
		strategy.Update();
		//executor.Update();	
	}
	
}
