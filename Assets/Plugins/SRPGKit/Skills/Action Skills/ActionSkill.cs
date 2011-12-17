using UnityEngine;
using System.Collections.Generic;

public class ActionSkill : Skill, ITilePickerOwner {
	public StatEffectGroup[] targetEffects;

	override public bool isPassive { get { return false; } }
	
	//tile generation strategy (line/range/cone/etc)
	public ActionStrategy strategy;
	
	//io
	public bool supportKeyboard = true;	
	public bool supportMouse = true;
	public bool requireConfirmation = true;
	public bool awaitingConfirmation = false;
	public float indicatorCycleLength=1.0f;
	public MoveExecutor Executor { get { return null; } }
	
	//grid
	public GridOverlay overlay;
	
	//pick
	[SerializeField]
	TilePicker tilePicker;
	
	public Map Map { get { return map; } }
	public GridOverlay Overlay { get { return overlay; } }
	public bool SupportKeyboard { get { return supportKeyboard; } }
	public bool SupportMouse { get { return supportMouse; } }
	
	public bool RequireConfirmation { get { return requireConfirmation; } }
	public bool AwaitingConfirmation { 
		get { return awaitingConfirmation; }
		set { awaitingConfirmation = value; }
  	}
	public float IndicatorCycleLength { get { return indicatorCycleLength; } }
	
	public PathNode[] targetTiles;
		
	//effect parameters (elements, w/ev)
	//effect
	//animated action (executor?)

	//knockback (executor on defender? move executor tie-in?)
	//target reaction skills and support skills
	
	public override void Start() {
		base.Start();
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

		if(!HasParam("hitType")) {
			AddParam("hitType", Formula.Constant(0));
		}
		
		if(targetEffects == null || targetEffects.Length == 0) {
			StatEffect healthDamage = new StatEffect();
			healthDamage.statName = "health";
			healthDamage.effectType = StatEffectType.Augment;
			healthDamage.reactableTypes = new[]{"attack"};
			healthDamage.value = Formula.Lookup("damage", LookupType.ActorSkillParam);
			targetEffects = new StatEffectGroup[]{new StatEffectGroup{effects=new StatEffect[]{healthDamage}}};
		}
		
	}
	public override void Cancel() {
		if(!isActive) { return; }
		//executor.Cancel();
		base.Cancel();
	}
	
	public override void ActivateSkill() {
		if(isActive) { return; }
		tilePicker = new TilePicker();
		tilePicker.owner = this;
		base.ActivateSkill();
		strategy.owner = this;
		
		strategy.zRangeUpMin = GetParam("range.z.up.min", 0);
		strategy.zRangeUpMax = GetParam("range.z.up.max", 1);
		strategy.zRangeDownMin = GetParam("range.z.down.min", 0);
		strategy.zRangeDownMax = GetParam("range.z.down.max", 2);
		strategy.xyRangeMin = GetParam("range.xy.min", 1);
		strategy.xyRangeMax = GetParam("range.xy.max", 1);

		strategy.zRadiusUp = GetParam("radius.z.up", 0);
		strategy.zRadiusDown = GetParam("radius.z.down", 0);
		strategy.xyRadius = GetParam("radius.xy", 0);
		
		/*	
		executor.owner = this;
		
		executor.transformOffset = transformOffset;
		executor.animateTemporaryMovement = animateTemporaryMovement;
		executor.XYSpeed = XYSpeed;
		executor.ZSpeedUp = ZSpeedUp;
		executor.ZSpeedDown = ZSpeedDown;
    
		executor.Activate();
		*/	
		
		strategy.Activate();
		
		PresentMoves();
	}	
	public override void DeactivateSkill() {
		if(!isActive) { return; }
		strategy.Deactivate();
		//executor.Deactivate();
		overlay = null;
		if(map.IsShowingOverlay(skillName, character.gameObject.GetInstanceID())) {
			map.RemoveOverlay(skillName, character.gameObject.GetInstanceID());
		}	
		base.DeactivateSkill();
	}
	public override void Update() {
		base.Update();
		if(!isActive) { return; }
		strategy.owner = this;
		tilePicker.owner = this;
		if(character == null || !character.isActive) { return; }
		if(!arbiter.IsLocalPlayer(character.EffectiveTeamID)) {
			return;
		}
		if(GUIUtility.hotControl != 0) { return; }
		tilePicker.Update();
		strategy.Update();
		//executor.Update();	
	}
	
	public override void ApplySkill() {
		//TODO: support reaction abilities and support abilities that depend on the attacking ability
		int hitType = (int)GetParam("hitType", 0);
		currentHitType = hitType;
		if(targetEffects.Length == 0) {
			Debug.LogError("No effects in attack skill "+skillName+"!");
		}
		if(targetEffects[hitType].Length > 0) {
			targets = strategy.CharactersForTargetedTiles(targetTiles);
			ApplyEffectsTo(targetEffects[hitType].effects, targets);
		}
		base.ApplySkill();
	}
	
	public void CancelPick(TilePicker tp) {
		Cancel();
	}
	
	public Vector3 IndicatorPosition {
		get { return tilePicker.IndicatorPosition; }
	}
	
	virtual public void PresentMoves() {
		PathNode[] destinations = strategy.GetValidActions();
/*		MoveExecutor me = executor;*/
		Vector3 charPos = character.TilePosition;
		overlay = map.PresentGridOverlay(
			skillName, character.gameObject.GetInstanceID(), 
			new Color(0.6f, 0.3f, 0.2f, 0.7f),
			new Color(0.9f, 0.6f, 0.4f, 0.85f),
			destinations
		);
		awaitingConfirmation = false;
		tilePicker.FocusOnPoint(charPos);
	}

	public void TentativePick(TilePicker tp, Vector3 p) {
		targetTiles = strategy.GetTargetedTiles(p);
		overlay.SetSelectedPoints(map.CoalesceTiles(targetTiles));
		//TODO: show preview indicator until cancelled
	}	
	
	public void TentativePick(TilePicker tp, PathNode pn) {
		targetTiles = strategy.GetTargetedTiles(pn.pos);
		overlay.SetSelectedPoints(map.CoalesceTiles(targetTiles));
		//TODO: show preview indicator until cancelled
	}
	
	public void CancelEffectPreview() {

	}
	
	public void Pick(TilePicker tp, Vector3 p) {
		targetTiles = strategy.GetTargetedTiles(p);
		ApplySkill();
	}
	
	public void Pick(TilePicker tp, PathNode pn) {
		targetTiles = strategy.GetTargetedTiles(pn.pos);
		ApplySkill();
	}	
}
