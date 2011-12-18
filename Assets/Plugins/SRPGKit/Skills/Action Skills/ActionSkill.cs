using UnityEngine;
using System.Collections.Generic;

public enum TargetingMode {
	Self, //self only	//TODO: not clear if "self" is really a target mode, since it's easy to imagine both self-cardinal and arbitrary-tile-cardinal
	Pick, //one of a number of tiles
	Cardinal, //one of four angles
	Radial, //any Quaternion
	SelectLine //one of a number of lines
	//path?
	//waypoints?
};

public class ActionSkill : Skill, ITilePickerOwner {
	public StatEffectGroup[] targetEffects;

	override public bool isPassive { get { return false; } }
	
	public TargetingMode targetingMode = TargetingMode.Pick;
	
	//cardinal/radial targeting mode
	[HideInInspector]
	public Quaternion initialFacing;
	
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
		FaceDirection(initialFacing);
		//executor.Cancel();
		base.Cancel();
	}
	
	public virtual void FaceDirection(float ang) {
		character.Facing = Quaternion.Euler(0, ang, 0);
	}
	public virtual void FaceDirection(Quaternion dir) {
		character.Facing = dir;
	}
	
	
	public override void ActivateSkill() {
		if(isActive) { return; }
		initialFacing = character.Facing;
		if(targetingMode == TargetingMode.Pick) {
			tilePicker = new TilePicker();
			tilePicker.owner = this;
		} else {
			//TODO: radial and cardinal
		}
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
		
		strategy.Activate();
		
		PresentMoves();
	}	
	public override void DeactivateSkill() {
		if(!isActive) { return; }
		strategy.Deactivate();
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
		if(character == null || !character.isActive) { return; }
		if(!arbiter.IsLocalPlayer(character.EffectiveTeamID)) {
			return;
		}
		if(GUIUtility.hotControl != 0) { return; }
		if(targetingMode == TargetingMode.Pick) {
			tilePicker.owner = this;
			tilePicker.Update();
		} else {
			//TODO: radial and cardinal
			float h = Input.GetAxis("Horizontal");
			float v = Input.GetAxis("Vertical");
			if(supportKeyboard && 
				(h != 0 || v != 0) && 
			  (!awaitingConfirmation || !requireConfirmation)) {
				Vector2 d = map.TransformKeyboardAxes(h, v);
				if(targetingMode == TargetingMode.Cardinal) {
					if(d.x != 0 && d.y != 0) {
						if(Mathf.Abs(d.x) > Mathf.Abs(d.y)) { d.x = Mathf.Sign(d.x); d.y = 0; }
						else { d.x = 0; d.y = Mathf.Sign(d.y); }
					}
				}
				FaceDirection(Mathf.Atan2(d.y, d.x)*Mathf.Rad2Deg);
			}
			if(supportKeyboard && 
				 Input.GetButtonDown("Confirm")) {
				if(awaitingConfirmation || !requireConfirmation) {
	  	  	awaitingConfirmation = false;
					PickFacing(character.Facing);
				} else if(requireConfirmation) {
					TentativePickFacing(character.Facing);
					awaitingConfirmation = true;
				}
			}
			if(supportKeyboard && Input.GetButtonDown("Cancel")) {
				if(awaitingConfirmation && requireConfirmation) {
					awaitingConfirmation = false;
				} else {
					//Back out of move phase!
					Cancel();
				}
			}
		}
		strategy.Update();
		//executor.Update();	
	}
	
	public override void ApplySkill() {
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
	
	virtual public void PresentMoves() {
		PathNode[] destinations = strategy.GetValidActions();
		Vector3 charPos = character.TilePosition;
		overlay = map.PresentGridOverlay(
			skillName, character.gameObject.GetInstanceID(), 
			new Color(0.6f, 0.3f, 0.2f, 0.7f),
			new Color(0.9f, 0.6f, 0.4f, 0.85f),
			destinations
		);
		awaitingConfirmation = false;
		if(targetingMode == TargetingMode.Pick) {		
			tilePicker.FocusOnPoint(charPos);
		} else {
			//TODO: radial and cardinal
		}
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
	
	public void TentativePickFacing(Quaternion f) {
		targetTiles = strategy.GetTargetedTiles(f);
		overlay.SetSelectedPoints(map.CoalesceTiles(targetTiles));
		//TODO: show preview indicator until cancelled
	}
	public void PickFacing(Quaternion f) {
		targetTiles = strategy.GetTargetedTiles(f);
		ApplySkill();
	}
	public void TentativePickFacing(float angle) {
		TentativePickFacing(Quaternion.Euler(0, angle, 0));
	}
	public void PickFacing(float angle) {
		PickFacing(Quaternion.Euler(0, angle, 0));
	}
}
