using UnityEngine;
using System.Collections.Generic;

public class AttackSkill : Skill {
	//tile generation strategy (line/range/cone/etc)
	public ActionStrategy strategy;
	
	public string[] actionTypes;

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
		skillGroup = "Act";
		actionTypes = new string[]{"attack"};
		if(!HasParam("range.z.up.min")) {
			AddParam("range.z.up.min", Formula.Constant(0));
		}
		if(!HasParam("range.z.up.max")) {
			AddParam("range.z.up.max", Formula.Constant(1));
		}
		if(!HasParam("range.z.down.min")) {
			AddParam("range.z.down.min", Formula.Constant(0));
		}
		if(!HasParam("range.z.down.max")) {
			AddParam("range.z.down.max", Formula.Constant(2));
		}
		if(!HasParam("range.xy.min")) {
			AddParam("range.xy.min", Formula.Constant(1));
		}
		if(!HasParam("range.xy.max")) {
			AddParam("range.xy.max", Formula.Constant(1));
		}
		
		if(!HasParam("radius.z.up")) {
			AddParam("radius.z.up", Formula.Constant(0));
		}
		if(!HasParam("radius.z.down")) {
			AddParam("radius.z.down", Formula.Constant(0));
		}
		if(!HasParam("radius.xy")) {
			AddParam("radius.xy", Formula.Constant(0));
		}

		if(!HasParam("damage")) {
			AddParam("damage", Formula.Constant(0));
		}
		
		if(targetEffects == null || targetEffects.Length == 0) {
			StatEffect healthDamage = new StatEffect();
			healthDamage.statName = "health";
			healthDamage.effectType = StatEffect.EffectType.Augment;
			healthDamage.value = Formula.Lookup("damage");
			targetEffects = new StatEffect[]{healthDamage};
		}
		
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
		
		strategy.zRangeUpMin = GetParam("range.z.up.min");
		strategy.zRangeUpMax = GetParam("range.z.up.max");
		strategy.zRangeDownMin = GetParam("range.z.down.min");
		strategy.zRangeDownMax = GetParam("range.z.down.max");
		strategy.xyRangeMin = GetParam("range.xy.min");
		strategy.xyRangeMax = GetParam("range.xy.max");

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
	
	public override void ApplySkill() {
		//TODO: support reaction abilities and support abilities that depend on the attacking ability
		//find any units within target tiles
		targets = new List<Character>();
		foreach(PathNode pn in io.targetTiles) {
			Character c = map.CharacterAt(pn.pos);
			if(c != null) {
				targets.Add(c);
			}
		}
		foreach(Character c in targets) {
			currentTarget = c;
			foreach(StatEffect se in targetEffects) {
				//TODO: associated equipment?
				c.SetBaseStat(se.statName, se.ModifyStat(c.GetStat(se.statName), this, c, null));
			}
		}
		base.ApplySkill();
	}
	
}
