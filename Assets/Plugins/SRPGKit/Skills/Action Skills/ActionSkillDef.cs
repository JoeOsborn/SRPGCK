using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum TargetingMode {
	Self, //self only, suggests a Self-type targeting region.
	Pick, //one of a number of tiles
	Cardinal, //one of a set number of angles when applied to a cone or line targeting region OR one of a set number of subregions when applied to a compound region. jumps directly to the input angle.
	Radial, //any Quaternion—usually applied to a cone or a line targeting region OR to a compound region. input rotates the target angle by an amount.
	SelectRegion, //one of a number of regions, requires a Compound-type targeting region.
	Path, //a specific path, most sensibly applied to line, cone, sphere, or cylinder targeting regions
	Custom //subclass responsibility
}

public enum MultiTargetMode {
	Single,
	Chain,
	List
}

public enum TargetOption {
	Character,
	Path
}

public class ActionSkillDef : SkillDef {
	//overridden/overridable properties
	override public bool isPassive { get { return false; } }
	virtual public MoveExecutor Executor { get { return null; } }

	//def properties
	public SkillIO _io;
	public SkillIO io {
		get {
			if(_io == null) {
				_io = new SkillIO();
			}
			return _io;
		}
		set { _io = value; }
	}

	public StatEffectGroup applicationEffects;
	public StatEffectGroup[] targetEffects;
	public Formula delay;

	public MultiTargetMode multiTargetMode = MultiTargetMode.Single;
	public bool waypointsAreIncremental=false;
	public bool canCancelWaypoints=true;

	public TargetSettings[] _targetSettings;
	public TargetSettings[] targetSettings {
		get {
			if(_targetSettings == null || _targetSettings.Length == 0) {
				_targetSettings = new TargetSettings[]{new TargetSettings()};
				_targetSettings[0].Owner = this;
			}
			return _targetSettings;
		}
		set {
			_targetSettings = value;
			for(int i = 0; i < _targetSettings.Length; i++) {
				TargetSettings ts = _targetSettings[i];
				if(ts == null) { _targetSettings[i] = ts = new TargetSettings(); }
				ts.Owner = this;
			}
		}
	}

	public Formula maxWaypointDistanceF;
	public float maxWaypointDistance { get {
		return maxWaypointDistanceF.GetValue(fdb, this, null, null);
	} }

	//internals

	[System.NonSerialized]
	public bool lastTargetPushed = false;

	[System.NonSerialized]
	public Target initialTarget;
	[System.NonSerialized]
	public List<Target> targets;

	[SerializeField]
	protected bool awaitingConfirmation = false;

	const float facingClosenessThreshold = 1;
	bool cycleIndicatorZ = false;
	float indicatorCycleT = 0;
	protected CharacterController probe;

	protected float lastIndicatorKeyboardMove=0;
	protected float indicatorKeyboardMoveThreshold=0.3f;
	protected float firstClickTime = -1;
	protected float doubleClickThreshold = 0.3f;

	[System.NonSerialized]
	public LineRenderer lines;
	protected int nodeCount=0;

	[System.NonSerialized]
	public Overlay overlay;
	//caching for overlay tiles, esp. for subregions
	[System.NonSerialized]
	public PathNode[] targetRegionTiles;

	[System.NonSerialized]
	public float radiusSoFar=0;

	//properties and wrappers

	public Map Map { get { return map; } }
	public bool SupportKeyboard { get { return supportKeyboard; } }
	public bool SupportMouse { get { return supportMouse; } }

	//io
	public bool supportKeyboard {
		get { return io.supportKeyboard; }
		set { io.supportKeyboard = value; }
	}
	public bool supportMouse {
		get { return io.supportMouse; }
		set { io.supportMouse = value; }
	}
	public bool requireConfirmation {
		get { return io.requireConfirmation; }
		set { io.requireConfirmation = value; }
	}
	public float keyboardMoveSpeed {
		get { return io.keyboardMoveSpeed; }
		set { io.keyboardMoveSpeed = value; }
	}
	public float indicatorCycleLength {
		get { return io.indicatorCycleLength; }
		set { io.indicatorCycleLength = value; }
	}
	public Material pathMaterial {
		get { return io.pathMaterial; }
		set { io.pathMaterial = value; }
	}
	public CharacterController probePrefab {
		get { return io.probePrefab; }
		set { io.probePrefab = value; }
	}
	public bool invertOverlay {
		get { return io.invertOverlay; }
		set { io.invertOverlay = value; }
	}
	public bool lockToGrid {
		get { return io.lockToGrid; }
		set { io.lockToGrid = value; }
	}
	public bool performTemporaryStepsImmediately {
		get { return io.performTemporaryStepsImmediately; }
		set { io.performTemporaryStepsImmediately = value; }
	}
	public bool performTemporaryStepsOnConfirmation {
		get { return io.performTemporaryStepsOnConfirmation; }
		set { io.performTemporaryStepsOnConfirmation = value; }
	}
	public Color overlayColor {
		get { return io.overlayColor; }
		set { io.overlayColor = value; }
	}
	public Color highlightColor {
		get { return io.highlightColor; }
		set { io.highlightColor = value; }
	}
	public RadialOverlayType overlayType {
		get { return io.overlayType; }
		set { io.overlayType = value; }
	}
	public bool drawOverlayRim {
		get { return io.drawOverlayRim; }
		set { io.drawOverlayRim = value; }
	}
	public bool drawOverlayVolume {
		get { return io.drawOverlayVolume; }
		set { io.drawOverlayVolume = value; }
	}

	public bool RequireConfirmation { get {
		return requireConfirmation &&
			!(multiTargetMode == MultiTargetMode.Chain && targets.Count < targetSettings.Length);
	} }

	public bool AwaitingConfirmation {
		get { return awaitingConfirmation; }
		set {
			if(!value && awaitingConfirmation) {
				IncrementalCancel();
			}
			awaitingConfirmation = value;
		}
	}

	public float IndicatorCycleLength { get { return indicatorCycleLength; } }
	protected bool ShouldDrawPath { get { return currentSettings.ShouldDrawPath; } }
	protected bool DeferPathRegistration { get { return currentSettings.DeferPathRegistration; } }
	public float NewNodeThreshold { get { return lockToGrid ? 1 : currentSettings.newNodeThreshold; } }
	public Overlay Overlay { get { return overlay; } }
	protected GridOverlay _GridOverlay { get { return overlay as GridOverlay; } }
	protected RadialOverlay _RadialOverlay { get { return overlay as RadialOverlay; } }

	public bool HasTargetingMode(TargetingMode tm) {
		return targetSettings.Any(t => t.targetingMode == tm);
	}

	public TargetSettings currentSettings { get {
		return targetSettings[Mathf.Min(targetSettings.Length-1, targets.Count-1)];
	} }

	public Target currentTarget { get {
		return targets.Count == 0 ? initialTarget : targets[targets.Count-1];
	} }
	public TargetSettings lastSettings { get {
		if(targets.Count == 1) { return null; }
		return targetSettings[targets.Count-2];
	} }
	public Target lastTarget { get {
		if(targets.Count == 1) { return initialTarget; }
		return targets[targets.Count-2];
	} }

	public Vector3 TargetPosition { get {
		if(multiTargetMode == MultiTargetMode.Chain) {
			for(int i = targets.Count-2; i >= 0; i--) {
				Target t = targets[i];
				if(targetSettings[i].doNotMoveChain) { continue; }
				if(t.path != null) { return t.path.pos; }
				if(t.character != null) { return t.character.TilePosition; }
			}
		} else {
			Target t = lastTarget;
			if(t.path != null) { return t.path.pos; }
			if(t.character != null) { return t.character.TilePosition; }
		}
		return initialTarget.Position;
	} }
	public Quaternion TargetFacing { get {
		if(multiTargetMode == MultiTargetMode.Chain) {
			for(int i = targets.Count-2; i >= 0; i--) {
				Target t = targets[i];
				if(t.facing != null) { return t.facing.Value; }
	//			if(t.character != null) { return t.character.Facing; }
			}
		} else {
			Target t = lastTarget;
			if(t.facing != null) { return t.facing.Value;	}
		}
		return initialTarget.facing.Value;
	} }

	public Vector3 EffectPosition { get {
		if(multiTargetMode == MultiTargetMode.Chain) {
			for(int i = targets.Count-1; i >= 0; i--) {
				Target t = targets[i];
				if(t.path != null) { return t.path.pos; }
				if(t.character != null) { return t.character.TilePosition; }
			}
		} else {
			Target t = currentTarget;
			if(t.path != null) { return t.path.pos; }
			if(t.character != null) { return t.character.TilePosition; }
		}
		return initialTarget.Position;
	} }
	public Quaternion EffectFacing { get {
		if(multiTargetMode == MultiTargetMode.Chain) {
			for(int i = targets.Count-1; i >= 0; i--) {
				Target t = targets[i];
				if(t.facing != null) { return t.facing.Value; }
	//			if(t.character != null) { return t.character.Facing; }
			}
		} else {
			Target t = currentTarget;
			if(t.facing != null) { return t.facing.Value;	}
		}
		return initialTarget.facing.Value;
	} }

	public Vector3 EffectPositionForTarget(Target t) {
		if(t.path != null) { return t.path.pos; }
		if(t.character != null) { return t.character.TilePosition; }
		int idx = targets.IndexOf(t);
		for(int i = idx; i >= 0; i--) {
			Target ti = targets[i];
			if(ti.path != null) { return ti.path.pos; }
			if(ti.character != null) { return ti.character.TilePosition; }
		}
		return initialTarget.Position;
	}
	public Quaternion EffectFacingForTarget(Target t) {
		if(t.facing != null) { return t.facing.Value; }
		int idx = targets.IndexOf(t);
		for(int i = idx; i >= 0; i--) {
			Target ti = targets[i];
			if(ti.facing != null) { return ti.facing.Value; }
		}
		return initialTarget.facing.Value;
	}

	public bool SingleTarget { get {
		return multiTargetMode == MultiTargetMode.Single;
	} }
	public bool MultiTarget { get {
		return multiTargetMode != MultiTargetMode.Single;
	} }
	public bool ChainedTarget { get {
		return multiTargetMode == MultiTargetMode.Chain;
	} }
	public bool ListTarget { get {
		return multiTargetMode == MultiTargetMode.List;
	} }

	public override void Start() {
		base.Start();
	}
	protected override void ResetSkill() {
		skillName = "Attack";
		skillGroup = "Act";
	}
	protected virtual void ResetActionSkill() {
		overlayColor = new Color(0.6f, 0.3f, 0.2f, 0.7f);
		highlightColor = new Color(0.9f, 0.6f, 0.4f, 0.85f);

		if(!HasParam("hitType")) {
			AddParam("hitType", Formula.Constant(0));
		}
		targetSettings = new TargetSettings[]{new TargetSettings()};
		targetSettings[0].Owner = this;

		// if(targetEffects == null || targetEffects.Length == 0) {
		// 	StatEffect healthDamage = new StatEffect();
		// 	healthDamage.statName = "health";
		// 	healthDamage.effectType = StatEffectType.Augment;
		// 	healthDamage.reactableTypes = new[]{"attack"};
		// 	healthDamage.value = Formula.Lookup("damage", LookupType.ActorSkillParam);
		// 	targetEffects = new StatEffectGroup[]{new StatEffectGroup{effects=new StatEffect[]{healthDamage}}};
		// }
	}
	public override void Reset() {
		base.Reset();
		ResetActionSkill();
	}
	public override void Cancel() {
		if(!isActive) { return; }
		FaceDirection(initialTarget.facing.Value);
		//executor.Cancel();
		base.Cancel();
	}
	public virtual void FaceDirection(Quaternion q) {
		FaceDirection(q.eulerAngles.y);
	}

	public virtual void FaceDirection(float ang) {
		if(targets.Count == 1) {
			character.Facing = ang;
		}
		currentTarget.Facing(ang);
	}

	public override void ActivateSkill() {
		if(isActive) { return; }
		foreach(TargetSettings ts in targetSettings) {
			ts.Owner = this;
		}
		lastTargetPushed = false;
		base.ActivateSkill();
		awaitingConfirmation=false;
		initialTarget = (new Target()).
			Path(character.TilePosition).
			Facing(character.Facing);
		Debug.Log("activate with initial target "+initialTarget);
		if(targets == null) {
			targets = new List<Target>();
		}
		targets.Add(initialTarget.Clone());
		SetArgsFromTarget(initialTarget, currentSettings, "");
		nodeCount = 0;
		radiusSoFar = 0;
		if(currentSettings.targetingMode == TargetingMode.Custom) {
			ActivateTargetCustom();
		}
		PresentMoves();
	}
	public override void DeactivateSkill() {
		if(!isActive) { return; }
		overlay = null;
		if(map.IsShowingOverlay(skillName, character.gameObject.GetInstanceID())) {
			map.RemoveOverlay(skillName, character.gameObject.GetInstanceID());
		}
		if(currentSettings.targetingMode == TargetingMode.Custom) {
			DeactivateTargetCustom();
		}
		if(probe != null) {
			Object.Destroy(probe.gameObject);
		}
		initialTarget = null;
		targets.Clear();
		radiusSoFar = 0;
		nodeCount = 0;
		awaitingConfirmation=false;
		base.DeactivateSkill();
	}
	public override void Update() {
		base.Update();
		if(!isActive) { return; }
		if(Executor != null && Executor.IsMoving) { return; }
		currentSettings.targetRegion.Owner = this;
		currentSettings.effectRegion.Owner = this;
		if(character == null || !character.isActive) { return; }
		if(!arbiter.IsLocalTeam(character.EffectiveTeamID)) {
			return;
		}
		if(GUIUtility.hotControl != 0) { return; }
		UpdateTarget();
		UpdateCancel();
	}

	protected virtual void UpdatePickOrPath() {
		if(supportMouse) {
			if(Input.GetMouseButton(0)) {
				cycleIndicatorZ = false;
				Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
				Vector3 hitSpot;
				if(overlay == null) { return; }
				bool inside = overlay.Raycast(r, out hitSpot);
				PathNode pn = overlay.PositionAt(hitSpot);
				if(lockToGrid) {
					hitSpot.x = Mathf.Floor(hitSpot.x+0.5f);
					hitSpot.y = Mathf.Floor(hitSpot.y+0.5f);
					hitSpot.z = Mathf.Floor(hitSpot.z+0.5f);
				}
				if(inside && (DeferPathRegistration || pn != null)) {
					if((!awaitingConfirmation || !RequireConfirmation)) {
						if(DeferPathRegistration) {
							RegisterPathPoint(pn ?? new PathNode(hitSpot, null, 0));
						} else {
							Vector3 srcPos = currentTarget.Position;
							//add positions from pn back to current pos
							List<PathNode> pts = new List<PathNode>();
							while(pn != null && pn.pos != srcPos) {
								pts.Add(pn);
								pn = pn.prev;
							}
							for(int i = 0; i < pts.Count; i++) {
								RegisterPathPoint(pts[i]);
							}
						}
					}
					if(Input.GetMouseButtonDown(0)) {
						if(Time.time-firstClickTime > doubleClickThreshold) {
							firstClickTime = Time.time;
						} else {
							firstClickTime = -1;
							if(!waypointsAreIncremental &&
								 !currentSettings.immediatelyExecuteDrawnPath &&
								 targets.Count > 1 &&
								 currentTarget.Position == hitSpot) {
								UnwindToLastWaypoint();
							} else {
								if(overlay.ContainsPosition(hitSpot) &&
									 overlay.PositionAt(hitSpot).canStop) {
	 								if(RequireConfirmation &&
	 									 !awaitingConfirmation) {
	 									awaitingConfirmation = true;
	 									if(performTemporaryStepsOnConfirmation) {
	 										TemporaryApplyCurrentTarget();
	 									}
	 								} else {
	 									ExecutePathTo(currentTarget.path);
	 								}
								}
							}
						}
					}
				}
			}
		}
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");
		if(supportKeyboard &&
			(h != 0 || v != 0) &&
			(!awaitingConfirmation || !RequireConfirmation)) {
			cycleIndicatorZ = true;
			indicatorCycleT = 0;
			if(lockToGrid) {
				if((Time.time-lastIndicatorKeyboardMove) > indicatorKeyboardMoveThreshold) {
					Vector2 d = TransformKeyboardAxes(h, v);
					if(Mathf.Abs(d.x) > Mathf.Abs(d.y)) { d.x = Mathf.Sign(d.x); d.y = 0; }
					else { d.x = 0; d.y = Mathf.Sign(d.y); }
					Vector3 newDest = currentTarget.Position;
					if(newDest.x+d.x >= 0 && newDest.y+d.y >= 0 &&
							map.HasTileAt((int)(newDest.x+d.x), (int)(newDest.y+d.y))) {
						lastIndicatorKeyboardMove = Time.time;
						newDest.x += d.x;
						newDest.y += d.y;
						newDest.z = map.NearestZLevel(
							(int)newDest.x,
							(int)newDest.y,
							(int)newDest.z
						);
						if(DestIsBacktrack(newDest)) {
							UnwindPath();
						} else {
							PathNode pn = overlay.PositionAt(newDest);
							if(DeferPathRegistration || (pn != null && pn.canStop)) {
								RegisterPathPoint(pn ?? new PathNode(newDest, null, 0));
							}
						}
					}
				}
			} else {
				Transform cameraTransform = Camera.main.transform;
				Vector3 forward = cameraTransform.TransformDirection(Vector3.forward);
				forward.y = 0;
				forward = forward.normalized;
				Vector3 right = new Vector3(forward.z, 0, -forward.x);
				Vector3 offset = h * right + v * forward;

				//try to move the probe
				Vector3 lastProbePos = probe.transform.position;
				probe.SimpleMove(offset*keyboardMoveSpeed);

				Vector3 newDest = map.InverseTransformPointWorld(probe.transform.position);
				PathNode pn = overlay.PositionAt(newDest);
				float thisDistance = Vector3.Distance(newDest, currentTarget.Position);
				if(thisDistance >= NewNodeThreshold) {
					if(DeferPathRegistration || (pn != null && pn.canStop)) {
						if(ShouldDrawPath) {
							lines.SetPosition(nodeCount, probe.transform.position);
						}
						RegisterPathPoint(pn ?? new PathNode(newDest, null, 0));
					} else {
						probe.transform.position = lastProbePos;
					}
				} else {
					if(ShouldDrawPath && pn != null && pn.canStop) {
						lines.SetPosition(nodeCount, probe.transform.position);
					}
				}
			}
		}
		if((currentSettings.targetingMode == TargetingMode.Pick ||
			  currentSettings.targetingMode == TargetingMode.SelectRegion) &&
				cycleIndicatorZ &&
				(!awaitingConfirmation || !RequireConfirmation)) {
			indicatorCycleT += Time.deltaTime;
			if(indicatorCycleT >= indicatorCycleLength) {
				indicatorCycleT -= indicatorCycleLength;
				Vector3 newSel = currentTarget.Position;
				newSel.z = map.NextZLevel((int)newSel.x, (int)newSel.y, (int)newSel.z, true);
				RegisterPathPoint(overlay.PositionAt(newSel) ?? new PathNode(newSel, null, 0));
			}
		}
		if(supportKeyboard &&
			Input.GetButtonDown("Confirm") &&
			overlay.ContainsPosition(currentTarget.Position) &&
		  overlay.PositionAt(currentTarget.Position).canStop) {
			if(RequireConfirmation &&
				!awaitingConfirmation) {
				awaitingConfirmation = true;
				if(performTemporaryStepsOnConfirmation) {
					TemporaryApplyCurrentTarget();
				}
			} else {
				ExecutePathTo(currentTarget.path);
			}
		}
	}

	protected virtual void UpdateTarget() {
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");
		switch(currentSettings.targetingMode) {
			case TargetingMode.Self:
				if(RequireConfirmation) {
					if(!awaitingConfirmation) {
						currentTarget.path = new PathNode(currentTarget.Position, null, 0);
						TemporaryApplyCurrentTarget();
						awaitingConfirmation = true;
					}
				} else {
					ExecutePathTo(currentTarget.path);
				}
				if(RequireConfirmation &&
				   awaitingConfirmation &&
				   supportKeyboard &&
				   Input.GetButtonDown("Confirm")) {
					ExecutePathTo(currentTarget.path);
					awaitingConfirmation = false;
				}
				break;
			case TargetingMode.Cardinal:
			case TargetingMode.Radial:
				float oldFacing = EffectFacing.eulerAngles.y;
				float targetFacing = oldFacing;
				if(supportKeyboard && (h != 0 || v != 0) &&
					(!awaitingConfirmation || !RequireConfirmation)) {
					Vector2 d = TransformKeyboardAxes(h, v);
					if(currentSettings.targetingMode == TargetingMode.Cardinal) {
						if(d.x != 0 && d.y != 0) {
							if(Mathf.Abs(d.x) > Mathf.Abs(d.y)) { d.x = Mathf.Sign(d.x); d.y = 0; }
							else { d.x = 0; d.y = Mathf.Sign(d.y); }
						}
						targetFacing = Mathf.Atan2(d.y, d.x)*Mathf.Rad2Deg;
					} else {
						//adjust facing by d.x
						if(lockToGrid) {
							if((Time.time-lastIndicatorKeyboardMove) > indicatorKeyboardMoveThreshold) {
								targetFacing += d.x < 0 ? 90 : (d.x > 0 ? -90 : 0);
								lastIndicatorKeyboardMove = Time.time;
							}
						} else {
							targetFacing += currentSettings.rotationSpeedXY*d.x*Time.deltaTime;
						}
//						targetFacingZ += rotationSpeedZ*d.y*Time.deltaTime;
					}
				}
				if(supportMouse &&
					Input.GetMouseButton(0)) {
					if(!awaitingConfirmation || !RequireConfirmation) {
						Vector2 d = Input.mousePosition - lastTarget.Position;
						//convert d to -1..1 range in x and y
						d.x /= Screen.width/2.0f;
						d.x -= 1;
						d.y /= Screen.height/2.0f;
						d.y -= 1;
						d = TransformKeyboardAxes(d.x, d.y);
						if(currentSettings.targetingMode == TargetingMode.Cardinal ||
						   (currentSettings.targetingMode == TargetingMode.Radial && lockToGrid)) {
							if(d.x != 0 && d.y != 0) {
								if(Mathf.Abs(d.x) > Mathf.Abs(d.y)) { d.x = Mathf.Sign(d.x); d.y = 0; }
								else { d.x = 0; d.y = Mathf.Sign(d.y); }
							}
							targetFacing = Mathf.Atan2(d.y, d.x)*Mathf.Rad2Deg-90;
						} else {
							targetFacing = Mathf.Atan2(d.y, d.x)*Mathf.Rad2Deg-90;
						}
					}
				}
				if(lockToGrid) {
					FaceDirection(targetFacing);
				} else {
					FaceDirection(Mathf.MoveTowardsAngle(oldFacing, targetFacing, currentSettings.rotationSpeedXY*Time.deltaTime));
				}
				float newFacing = EffectFacing.eulerAngles.y;
				if(!Mathf.Approximately(oldFacing, newFacing)) {
					TentativePickFacing(newFacing);
				}
				float df = Mathf.Abs(Mathf.DeltaAngle(targetFacing,newFacing));
				if(supportMouse &&
				   Input.GetMouseButtonDown(0) &&
				   df < facingClosenessThreshold) {
					if(Time.time-firstClickTime > doubleClickThreshold) {
						firstClickTime = Time.time;
					} else {
						firstClickTime = -1;
						if(awaitingConfirmation) {
							PickFacing(currentTarget.facing.Value);
							awaitingConfirmation = false;
						} else {
							TentativePickFacing(EffectFacing);
							awaitingConfirmation = true;
						}
					}
				}
				if(supportKeyboard &&
					 Input.GetButtonDown("Confirm") &&
				   df < facingClosenessThreshold) {
					if(awaitingConfirmation || !RequireConfirmation) {
						awaitingConfirmation = false;
						PickFacing(EffectFacing);
					} else if(RequireConfirmation) {
						TentativePickFacing(EffectFacing);
						awaitingConfirmation = true;
					}
				}
				break;
			case TargetingMode.SelectRegion:
			case TargetingMode.Pick:
			case TargetingMode.Path:
				UpdatePickOrPath();
				break;
			case TargetingMode.Custom:
				UpdateTargetCustom();
				break;
		}
	}

	public virtual void IncrementalCancel() {
		map.BroadcastMessage("SkillIncrementalCancel", this, SendMessageOptions.DontRequireReceiver);
		if(AwaitingTargetOption || (RequireConfirmation && awaitingConfirmation)) {
			awaitingConfirmation = false;
			currentTarget.character = null;
			TemporaryApplyCurrentTarget();
		} else if(targets.Count > 1 &&
		          canCancelWaypoints &&
		          !waypointsAreIncremental &&
		          !currentSettings.immediatelyExecuteDrawnPath) {
			UnwindToLastWaypoint();
		} else {
			if(awaitingConfirmation && RequireConfirmation) {
				awaitingConfirmation = false;
				if(currentSettings.IsPickOrPath) {
					UpdateGridSelection();
				} else {
					TemporaryApplyCurrentTarget();
				}
			} else {
				//Back out of skill!
				Cancel();
			}
		}
	}

	protected virtual void UpdateCancel() {
		if(supportKeyboard &&
				Input.GetButtonDown("Cancel")) {
			IncrementalCancel();
		}
	}

	public void DelayedApply(List<Target> targs) {
		targets = targs;
		Debug.Log("delayed apply skill "+skillName+" with "+targs.Count+" targets");
		ApplySkillToTargets();
	}

	public void ConfirmDelayedSkillTarget(TargetOption tgo) {
		if(AwaitingTargetOption) {
			for(int i = 0; i < targets.Count; i++) {
				Target t = targets[i];
				TargetSettings ts = targetSettings[i];
				if(!ts.allowsCharacterTargeting) {
					continue;
				}
				if(t.path != null && t.character != null) {
					if(tgo == TargetOption.Path) {
						t.character = null;
					} else if(tgo == TargetOption.Character) {
						t.path = null;
					}
				}
			}
			ApplySkill();
		} else {
			Debug.LogError("ConfirmDelayedSkillTarget called when skill "+this+" was not expecting target changes");
		}
	}

	public bool AwaitingTargetOption { get {
		for(int i = 0; i < targets.Count; i++) {
			Target t = targets[i];
			TargetSettings ts = targetSettings[i];
			if(t.character != null && t.path != null && ts.allowsCharacterTargeting) {
				return true;
			}
		}
		return false;
	} }


	public virtual void FindPerApplicationCharacterTargets() {
		switch(currentSettings.targetingMode) {
			case TargetingMode.Self:
			case TargetingMode.Pick:
			case TargetingMode.Path:
				Vector3 pos = currentTarget.Position;
				Character c = map.CharacterAt(pos);
				if(c != null) {
					targetCharacters = new List<Character>(){c};
				} else {
					targetCharacters = null;
				}
				break;
			case TargetingMode.Cardinal:
			case TargetingMode.Radial:
			//FIXME: wrong for selectRegion
			case TargetingMode.SelectRegion:
				targetCharacters = null;
				break;
			default:
				Debug.LogError("Unrecognized targeting mode");
				break;
		}
	}
	public override void ApplySkill() {
		ClearLastEffects();
		float delayVal = delay == null ? 0 : delay.GetValue(fdb, this);
		if(delayVal == 0) {
			Debug.Log("No delay!");
			ApplySkillToTargets();
			base.ApplySkill();
		} else {
			Debug.Log("using target with tile "+currentTarget.path+" and character "+currentTarget.character);
			if(AwaitingTargetOption) {
				map.BroadcastMessage(
					"SkillNeedsCharacterTargetingOption",
					this,
					SendMessageOptions.DontRequireReceiver
				);
			} else {
				Debug.Log("apply after delay");
				scheduler.ApplySkillAfterDelay(this, targets, delayVal);
				base.ApplySkill();
			}
			//FIXME: Move this delayed-application concept up into Skill,
			//let it work with reactions too.
		}
	}
	public virtual void ApplySkillToTargets() {
		//set up all args
		//FIXME: NEXT: see email
		Debug.Log("ready the args");
		for(int i = 0; i < targets.Count; i++) {
			Target t = targets[i];
			TargetSettings ts = targetSettings[i];
			Debug.Log("set args at "+i+" from "+t);
			//arg0... arg1...
			SetArgsFromTarget(t, ts, ""+i);
		}
		Debug.Log("set default args from "+currentTarget);
		SetArgsFromTarget(currentTarget, currentSettings, "");
		FindPerApplicationCharacterTargets();
		ApplyPerApplicationEffectsTo(applicationEffects.effects, targetCharacters);

		switch(multiTargetMode) {
			case MultiTargetMode.Single:
			case MultiTargetMode.List:
				if(targetEffects.Length == 0) {
					break;
				}
				for(int i = 0; i < targets.Count; i++) {
					Target t = targets[i];
					TargetSettings ts = targetSettings[i];
					//set up "current" args
					Debug.Log("Apply vs target "+t);
					SetArgsFromTarget(t, ts, "");
					PathNode[] targetTiles = PathNodesForTarget(t, ts.targetRegion, ts.effectRegion, EffectPositionForTarget(t), EffectFacingForTarget(t));
					Debug.Log("tts:"+targetTiles.Length);
					foreach(PathNode tt in targetTiles) {
						Debug.Log(tt);
					}
					targetCharacters = ts.effectRegion.CharactersForTargetedTiles(targetTiles);
					Debug.Log("targetChars:"+targetCharacters.Count);
					ApplyEffectsTo(t, ts, targetEffects, targetCharacters, "hitType");
				}
				break;
			case MultiTargetMode.Chain:
				//at each step, gather up targets in the effect region of the previous step
				//and apply subsequent steps to them.
				//path and pick and selectregion require individual target characters
				//set up "current" args -- "current" always means "last" here
				if(targetEffects.Length == 0) { break; }
				List<Character> chars = new List<Character>();
				for(int i = 0; i < targets.Count-1; i++) {
					Target t = targets[i];
					TargetSettings ts = targetSettings[i];
					if(ts.IsPickOrPath && chars.Count > 1) {
						Debug.LogError("Can't chain pick/path/select region after multitarget effect");
					}
					PathNode[] targetTiles = PathNodesForTarget(t, ts.targetRegion, ts.effectRegion, EffectPositionForTarget(t), EffectFacingForTarget(t));
					List<Character> hereChars = ts.effectRegion.CharactersForTargetedTiles(targetTiles);
					foreach(Character c in hereChars) {
						if(!chars.Contains(c)) {
							chars.Add(c);
						}
					}
				}
				targetCharacters = chars;
				ApplyEffectsTo(currentTarget, currentSettings, targetEffects, targetCharacters, "hitType");
				break;
		}
		map.BroadcastMessage("SkillEffectApplied", this, SendMessageOptions.DontRequireReceiver);
	}

	virtual protected PathNode[] GetPresentedActionTiles() {
		return currentSettings.displayUnimpededTargetRegion ?
		  currentSettings.targetRegion.GetTilesInRegion(TargetPosition, TargetFacing) :
		  GetValidActionTiles();
	}
	virtual protected PathNode[] GetValidActionTiles() {
		return currentSettings.targetRegion.GetValidTiles(TargetPosition, TargetFacing);
	}

	virtual protected void CreateOverlay() {
		//TODO: let the region create the overlay
		//TODO: overlays for weird region types, including compound
		if(overlay != null) { return; }
		if(lockToGrid) {
			targetRegionTiles = GetPresentedActionTiles();
			// Debug.Log("trt:"+targetRegionTiles.Length);
			overlay = map.PresentGridOverlay(
				skillName, character.gameObject.GetInstanceID(),
				overlayColor,
				highlightColor,
				targetRegionTiles
			);
		} else {
			Vector3 charPos = currentTarget.Position;
			//FIXME: minimum ranges and different z up/down ranges don't work with radial overlays
			if(overlayType == RadialOverlayType.Sphere) {
				overlay = map.PresentSphereOverlay(
					skillName, character.gameObject.GetInstanceID(),
					overlayColor,
					charPos,
					currentSettings.targetRegion.radiusMax - radiusSoFar,
					drawOverlayRim,
					drawOverlayVolume,
					invertOverlay
				);
			} else if(overlayType == RadialOverlayType.Cylinder) {
				overlay = map.PresentCylinderOverlay(
					skillName, character.gameObject.GetInstanceID(),
					overlayColor,
					charPos,
					currentSettings.targetRegion.radiusMax - radiusSoFar,
					currentSettings.targetRegion.zDownMax,
					drawOverlayRim,
					drawOverlayVolume,
					invertOverlay
				);
			}
		}
	}

	virtual public void PresentMoves() {
		CreateOverlay();
		awaitingConfirmation = false;
		if(probePrefab != null) {
			probe = Object.Instantiate(probePrefab, Vector3.zero, Quaternion.identity) as CharacterController;
			probe.transform.parent = map.transform;
			Physics.IgnoreCollision(probe.collider, character.collider);
		}
		ResetPosition();
		if(currentSettings.targetingMode == TargetingMode.Custom) {
			PresentMovesCustom();
		} else if(currentSettings.targetingMode == TargetingMode.Path) {
			lines = probe.gameObject.AddComponent<LineRenderer>();
			lines.materials = new Material[]{pathMaterial};
			lines.useWorldSpace = true;
		}
	}

	protected virtual void ActivateTargetCustom() {}
	protected virtual void UpdateTargetCustom() {}
	protected virtual void PresentMovesCustom() {}
	protected virtual void DeactivateTargetCustom() {}
	protected virtual void CancelTargetCustom() {}
	protected virtual void ResetToInitialPositionCustom() {}

	public void TentativePick(Vector3 p) {
		Character c = map.CharacterAt(p);
		currentTarget.Path(p).Character(c);
		float angle = TargetFacing.eulerAngles.y;
		Vector3 ep = EffectPosition;
		Vector3 tp = TargetPosition;
		if(!Mathf.Approximately(ep.y,tp.y) ||
		   !Mathf.Approximately(ep.x,tp.x)) {
			angle = Mathf.Atan2(ep.y-tp.y, ep.x-tp.x)*Mathf.Rad2Deg;
		}
		currentTarget.Facing(angle);
		Debug.Log("show path from "+tp+" angle "+angle);
		_GridOverlay.SetSelectedPoints(
			map.CoalesceTiles(
				currentSettings.effectRegion.GetValidTiles(tp, TargetFacing)
			)
		);
	}

	public void TentativePick(PathNode pn) {
		Character c = map.CharacterAt(pn.pos);
		currentTarget.Path(pn).Character(c);
		float angle = TargetFacing.eulerAngles.y;
		Vector3 ep = EffectPosition;
		Vector3 tp = TargetPosition;
		if(!Mathf.Approximately(ep.y,tp.y) ||
		   !Mathf.Approximately(ep.x,tp.x)) {
			angle = Mathf.Atan2(ep.y-tp.y, ep.x-tp.x)*Mathf.Rad2Deg;
		}
		currentTarget.Facing(angle);
		Debug.Log("show path from node "+tp+" angle "+angle);
		_GridOverlay.SetSelectedPoints(
			map.CoalesceTiles(
				currentSettings.effectRegion.GetValidTiles(tp, Quaternion.Euler(0,angle,0))
			)
		);
	}

	public void CancelEffectPreview() {

	}

	public virtual void Pick(Vector3 p) {
		Character c = map.CharacterAt(p);
		currentTarget.Path(p).Character(c);
		PushTarget();
	}

	public virtual void Pick(PathNode pn) {
		Character c = map.CharacterAt(pn.pos);
		currentTarget.Path(pn).Character(c);
		PushTarget();
	}

	public virtual void ImmediatelyPickSubregion(int subregionIndex) {
		if(subregionIndex < 0 || subregionIndex >= currentSettings.targetRegion.regions.Length) {
			Debug.LogError("Subregion "+subregionIndex+" out of bounds "+currentSettings.targetRegion.regions.Length);
		}
		_GridOverlay.SetSelectedPoints(map.CoalesceTiles(currentSettings.effectRegion.GetValidTiles(currentSettings.targetRegion.regions[subregionIndex].GetValidTiles(TargetPosition, TargetFacing), EffectFacing)));
	}

	public virtual void TentativePickSubregion(int subregionIndex) {
		currentTarget.Subregion(subregionIndex);
		ImmediatelyPickSubregion(subregionIndex);
	}

	public virtual void PickSubregion(int subregionIndex) {
		if(subregionIndex < 0 || subregionIndex >= currentSettings.targetRegion.regions.Length) {
			Debug.LogError("Subregion "+subregionIndex+" out of bounds "+currentSettings.targetRegion.regions.Length);
		}
		currentTarget.Subregion(subregionIndex);
		PushTarget();
	}
	//targeting facings, unlike character facings (for now at least!), may be arbitrary quaternions
	//FIXME: somehow involve target region?
	public virtual void ImmediatelyPickFacing(Quaternion f) {
		FaceDirection(f);
	}
	public virtual void TentativePickFacing(Quaternion f) {
		FaceDirection(f);
		currentTarget.Facing(f);
		_GridOverlay.SetSelectedPoints(map.CoalesceTiles(currentSettings.effectRegion.GetValidTiles(EffectPosition, EffectFacing)));
	}
	public virtual void PickFacing(Quaternion f) {
		//TODO: wut???
		currentTarget.Facing(f);
		PushTarget();
	}
	public virtual void TentativePickFacing(float angle) {
		TentativePickFacing(Quaternion.Euler(0, angle, 0));
	}
	public virtual void PickFacing(float angle) {
		PickFacing(Quaternion.Euler(0, angle, 0));
	}

	public void UnwindToLastWaypoint() {
		int priorCount = targets.Count;
		if(priorCount == 0) {
			ResetPosition();
		} else {
			while(targets.Count == priorCount) {
				UnwindPath(1);
			}
		}
	}

	protected void PopTarget() {
		targets.RemoveAt(targets.Count-1);
		if(performTemporaryStepsImmediately ||
		   (RequireConfirmation && performTemporaryStepsOnConfirmation)) {
			TemporaryApplyCurrentTarget();
		}
		targetRegionTiles = GetPresentedActionTiles();
	}

	protected bool CanUnwindPath { get {
		return !currentSettings.immediatelyExecuteDrawnPath &&
		((currentTarget.path != null && currentTarget.path.prev != null) ||
		 (!waypointsAreIncremental && targets.Count > 1));
	} }
	public void UnwindPath(int nodes=1) {
		for(int i = 0; i < nodes && CanUnwindPath; i++) {
			if(currentSettings.IsPickOrPath) {
				PathNode endOfPath = currentTarget.path;
				Vector3 oldEnd = endOfPath.pos;
				PathNode prev = endOfPath.prev != null ? endOfPath.prev : lastTarget.path;
				float thisDistance = 0;
				if(prev != null) {
					thisDistance = Vector2.Distance(
						new Vector2(oldEnd.x, oldEnd.y),
						new Vector2(prev.pos.x, prev.pos.y)
					);
				}
				if(lockToGrid) {
					thisDistance = (int)thisDistance;
					Vector4[] selPts = _GridOverlay.selectedPoints ?? new Vector4[0];
					_GridOverlay.SetSelectedPoints(selPts.Except(
						new Vector4[]{new Vector4(oldEnd.x, oldEnd.y, oldEnd.z, 1)}
					).ToArray());
				}
				if(ShouldDrawPath) {
					radiusSoFar -= thisDistance;
				}
				endOfPath = endOfPath.prev;
				currentTarget.path = endOfPath;
				if((endOfPath == null ||
				    endOfPath.prev == null) &&
				   targets.Count > 1 &&
				   !waypointsAreIncremental) {
					if(endOfPath == null) {
						PathNode wp=lastTarget.path, wpp=wp.prev;
						if(ShouldDrawPath) {
							endOfPath = wp.prev;
							thisDistance = Vector2.Distance(
								new Vector2(wp.pos.x, wp.pos.y),
								new Vector2(wpp.pos.x, wpp.pos.y)
							);
						} else {
							endOfPath = lastTarget.path;
							thisDistance = wp.xyDistanceFromStart;
						}
						if(lockToGrid) { thisDistance = (int)thisDistance; }
						radiusSoFar -= thisDistance;
					} else {
						endOfPath = lastTarget.path;
					}
					PopTarget();
				}
				nodeCount -= 1;
				if(probe != null) {
					probe.transform.position = map.TransformPointWorld(endOfPath.pos);
				}
				if(ShouldDrawPath) {
					lines.SetVertexCount(nodeCount+1);
					lines.SetPosition(nodeCount, probe.transform.position);
				}
			} else {
				PopTarget();
			}
		}
		//update the overlay
		UpdateOverlayParameters();
	}

	virtual protected void ResetToInitialPosition() {
		radiusSoFar = 0;
		Vector3 tp = initialTarget.Position;
		if(lockToGrid) {
			tp.x = (int)Mathf.Round(tp.x);
			tp.y = (int)Mathf.Round(tp.y);
			tp.z = map.NearestZLevel((int)tp.x, (int)tp.y, (int)Mathf.Round(tp.z));
		}
		if(probe != null) {
			probe.transform.position = map.TransformPointWorld(tp);
		}
		targets.Clear();
		Debug.Log("reset to ip "+tp+" init "+initialTarget);
		targets.Add(initialTarget.Clone());
		if(ShouldDrawPath) {
			lines.SetVertexCount(1);
			lines.SetPosition(0, probe.transform.position);
		}
		UpdateOverlayParameters();
		if(overlay != null && overlay.IsReady && lockToGrid) {
			_GridOverlay.SetSelectedPoints(new Vector4[0]);
		}
		switch(currentSettings.targetingMode) {
			case TargetingMode.SelectRegion://??
			case TargetingMode.Pick:
				RegisterPathPoint(currentTarget.path);
				cycleIndicatorZ = false;
				break;
			case TargetingMode.Cardinal://??
			case TargetingMode.Radial://??
				Debug.Log("reset; tentative pick facing "+currentTarget.facing);
				TentativePickFacing(currentTarget.facing.Value);
				break;
			case TargetingMode.Path:
				RegisterPathPoint(currentTarget.path);
				break;
			case TargetingMode.Custom:
				ResetToInitialPositionCustom();
				break;
		}
	}

	protected void ResetPosition() {
		if(targets.Count > 1 &&
		   !waypointsAreIncremental &&
		   !currentSettings.immediatelyExecuteDrawnPath) {
			UnwindToLastWaypoint();
		} else {
			ResetToInitialPosition();
		}
	}

	protected bool DestIsBacktrack(Vector3 newDest) {
		return !currentSettings.immediatelyExecuteDrawnPath && ShouldDrawPath && (
		(currentTarget.path != null && currentTarget.path.prev != null && newDest == currentTarget.path.prev.pos) ||
		(!waypointsAreIncremental && targets.Count > 1 &&
			(((currentTarget.path.prev == null) &&
			(targets[targets.Count-1].path.pos == newDest)) ||

			(currentTarget.path.prev == null &&
			targets[targets.Count-1].path.prev != null &&
			newDest == targets[targets.Count-1].path.prev.pos)
			)));
	}

	public bool PermitsNewWaypoints { get {
		if(targetSettings.Length == targets.Count) { return false; }
		if(currentSettings.immediatelyExecuteDrawnPath) { return false; }
		if(SingleTarget) { return false; }
		return
			maxWaypointDistance == 0 ||
			((radiusSoFar + NewNodeThreshold) < maxWaypointDistance);
	} }

	protected virtual void LastTargetPushed() {
		ApplySkill();
	}

	protected void PushTarget() {
		if(lastTargetPushed || targets.Count > targetSettings.Length) {
			Debug.LogError("Too many targets being pushed");
			return;
		}
 		if(!ShouldDrawPath) {
 			radiusSoFar += currentTarget.path.xyDistanceFromStart;
 		}
		Debug.Log("push target "+currentTarget);
		// if(RequireConfirmation && performTemporaryStepsOnConfirmation) {
		// 	Debug.Log("last target "+lastTarget);
		// 	Debug.Log("cur target "+currentTarget);
		// 	ImmediatelyApplyTarget(lastTarget, lastSettings);
		// }
		awaitingConfirmation = false;
		if(!PermitsNewWaypoints) { //no more new targets, finish up
			Debug.Log("last target!");
			lastTargetPushed = true;
			LastTargetPushed();
		} else { //new target
			map.BroadcastMessage(
				"SkillWillPushIntermediateTarget",
				this,
				SendMessageOptions.DontRequireReceiver
			);
			SetArgsFromTarget(currentTarget, currentSettings, ""+(targets.Count-1));
			if(multiTargetMode == MultiTargetMode.Chain) {
				SetArgsFromTarget(currentTarget, currentSettings, "");
			}
			if(currentSettings.targetingMode == TargetingMode.Path &&
			   currentSettings.immediatelyExecuteDrawnPath) {
				currentTarget.path = new PathNode(currentTarget.path.pos, null, 0);
				IncrementalApplyCurrentTarget();
			} else if(waypointsAreIncremental) {
				IncrementalApplyCurrentTarget();
			} else {
				TemporaryApplyCurrentTarget();
			}
			//FIXME: is it ok to set up so much data?
			Debug.Log("targpos " + TargetPosition);

			targets.Add((new Target()).
				Path(
					new PathNode(
						currentSettings.doNotMoveChain ?
							TargetPosition : EffectPosition,
						null,
						radiusSoFar
					)
				).
				Facing(EffectFacing)
			);
			UpdateOverlayParameters();
			map.BroadcastMessage(
				"SkillDidPushIntermediateTarget",
				this,
				SendMessageOptions.DontRequireReceiver
			);
		}
	}
	protected int SubregionContaining(Vector3 p) {
		Vector3 tp = new Vector3((int)p.x, (int)p.y, (int)p.z);
		foreach(PathNode pn in targetRegionTiles) {
			if(pn.pos == tp) {
				return pn.subregion;
			}
		}
		return -1;
	}
	virtual protected void TemporaryApplyTarget(Target t, TargetSettings ts) {
		if(ts == null) {
			if(t.path != null) {
				TemporaryExecutePathTo(t.path);
			}
			if(t.facing != null) {
				TentativePickFacing(t.facing.Value);
			}
			if(t.subregion != -1) {
				TentativePickSubregion(t.subregion);
			}
		} else {
			switch(ts.targetingMode) {
				case TargetingMode.Self:
				case TargetingMode.Pick:
				case TargetingMode.Path:
					TemporaryExecutePathTo(t.path);
					break;
				case TargetingMode.SelectRegion:
					TentativePickSubregion(t.subregion);
					break;
				case TargetingMode.Cardinal:
				case TargetingMode.Radial:
					Debug.Log("temp apply; tentative pick facing "+t.facing.Value);
					TentativePickFacing(t.facing.Value);
					break;
			}
		}
	}
	virtual protected void ImmediatelyApplyTarget(Target t, TargetSettings ts) {
		if(ts == null) {
			if(t.path != null) {
				ImmediatelyExecutePathTo(t.path);
			}
			if(t.facing != null) {
				ImmediatelyPickFacing(t.facing.Value);
			}
			if(t.subregion != -1) {
				ImmediatelyPickSubregion(t.subregion);
			}
		} else {
			switch(ts.targetingMode) {
				case TargetingMode.Self:
				case TargetingMode.Pick:
				case TargetingMode.Path:
					ImmediatelyExecutePathTo(t.path);
					break;
				case TargetingMode.SelectRegion:
					ImmediatelyPickSubregion(t.subregion);
					break;
				case TargetingMode.Cardinal:
				case TargetingMode.Radial:
					Debug.Log("temp apply; tentative pick facing "+t.facing.Value);
					ImmediatelyPickFacing(t.facing.Value);
					break;
			}
		}
	}
	virtual public void TemporaryApplyCurrentTarget() {
		TemporaryApplyTarget(currentTarget, currentSettings);
	}
	virtual public void IncrementalApplyCurrentTarget() {
		switch(currentSettings.targetingMode) {
			case TargetingMode.Self:
			case TargetingMode.Pick:
			case TargetingMode.Path:
				IncrementalExecutePathTo(currentTarget.path);
				break;
			case TargetingMode.SelectRegion:
				TentativePickSubregion(currentTarget.subregion);
				break;
			case TargetingMode.Cardinal:
			case TargetingMode.Radial:
				Debug.Log("inc apply; tentative pick facing "+currentTarget.facing.Value);
				TentativePickFacing(currentTarget.facing.Value);
				break;
		}
	}
	virtual public void ApplyCurrentTarget() {
		switch(currentSettings.targetingMode) {
			case TargetingMode.Self:
			case TargetingMode.Pick:
			case TargetingMode.Path:
				ExecutePathTo(currentTarget.path);
				break;
			case TargetingMode.SelectRegion:
				PickSubregion(currentTarget.subregion);
				break;
			case TargetingMode.Cardinal:
			case TargetingMode.Radial:
				Debug.Log("inc apply; tentative pick facing "+currentTarget.facing.Value);
				PickFacing(currentTarget.facing.Value);
				break;
		}
	}
	virtual protected void ImmediatelyExecutePathTo(PathNode pn) {
		MoveExecutor me = Executor;
		if(me != null &&
		   (currentSettings == null ||
		    !(currentSettings.targetingMode == TargetingMode.Path &&
		      currentSettings.immediatelyExecuteDrawnPath))) {
			//FIXME: really? what about chained moves?
			Debug.Log("first, pop back to "+pn);
			me.ImmediatelyMoveTo(pn);
		}
	}

	virtual protected void TemporaryExecutePathTo(PathNode pn) {
		//pick? face? dunno?
		if(currentSettings.targetingMode == TargetingMode.SelectRegion) {
			int sr = SubregionContaining(pn.pos);
			if(sr == -1) { return; }
			TentativePickSubregion(sr);
		} else {
			TentativePick(pn);
		}
	}

	virtual protected void IncrementalExecutePathTo(PathNode pn) {
		//??
		if(currentSettings.targetingMode == TargetingMode.SelectRegion) {
			int sr = SubregionContaining(pn.pos);
			if(sr == -1) { return; }
			TentativePickSubregion(sr);
		} else {
			TentativePick(pn);
		}
	}
	virtual protected void ExecutePathTo(PathNode pn) {
		if(currentSettings.targetingMode == TargetingMode.SelectRegion) {
			PickSubregion(SubregionContaining(pn.pos));
		} else {
			Pick(pn);
		}
	}

	protected void UpdateOverlayParameters() {
		if(overlay == null) { return; }
		//HACK: to avoid Unity crashers when I create and update an overlay on the same frame.
		if(!overlay.IsReady) {
			Owner.StartCoroutine(UpdateOverlayNextFrame());
			return;
		}
		if(lockToGrid) {
			_GridOverlay.UpdateDestinations(targetRegionTiles);
		} else {
			Vector3 charPos = currentTarget.Position;
			_RadialOverlay.tileRadius = (currentSettings.targetRegion.radiusMax - radiusSoFar);
			_RadialOverlay.UpdateOriginAndRadius(
				map.TransformPointWorld(charPos),
				(currentSettings.targetRegion.radiusMax - radiusSoFar)*map.sideLength
			);
		}
	}

	protected IEnumerator UpdateOverlayNextFrame() {
		yield return new WaitForFixedUpdate();
		UpdateOverlayParameters();
	}

	protected void RegisterPathPoint(PathNode endOfPath) {
		float thisDistance = Vector3.Distance(endOfPath.pos, currentTarget.Position);
		if(lockToGrid) {
			thisDistance = (int)thisDistance;
		}
		if(!ShouldDrawPath) {
			currentTarget.path = endOfPath;
		} else {
			radiusSoFar += thisDistance;
			currentTarget.path = new PathNode(endOfPath.pos, currentTarget.path, radiusSoFar);
			//add a line to this point
			nodeCount += 1;
			lines.SetVertexCount(nodeCount+1);
			lines.SetPosition(nodeCount, map.TransformPointWorld(endOfPath.pos));
		}
		if(currentSettings.immediatelyExecuteDrawnPath) {
			IncrementalExecutePathTo(new PathNode(endOfPath.pos, null, 0));
		} else if(performTemporaryStepsImmediately ||
		          currentSettings.targetingMode == TargetingMode.SelectRegion) {
			TemporaryApplyCurrentTarget();
		}
		if(currentSettings.targetingMode != TargetingMode.SelectRegion) {
			if(ShouldDrawPath) {
				//update the overlay
				UpdateOverlayParameters();
				if(lockToGrid &&
				   !currentSettings.immediatelyExecuteDrawnPath &&
				   !performTemporaryStepsImmediately) {
					Vector4[] selPts = _GridOverlay.selectedPoints ?? new Vector4[0];
					Vector3 selectedTile = endOfPath.pos;
					_GridOverlay.SetSelectedPoints(selPts.Concat(
						new Vector4[]{new Vector4(selectedTile.x, selectedTile.y, selectedTile.z, 1)}
					).ToArray());
				}
			} else {
				if(lockToGrid &&
				   !currentSettings.immediatelyExecuteDrawnPath &&
				   !performTemporaryStepsImmediately) {
					UpdateGridSelection();
				}
			}
		}
		if(probe != null) {
			probe.transform.position = map.TransformPointWorld(endOfPath.pos);
		}
	}
	protected virtual void UpdateGridSelection() {
		Vector3 selectedTile = currentTarget.Position;
		_GridOverlay.SetSelectedPoints(
			new Vector4[]{new Vector4(selectedTile.x, selectedTile.y, selectedTile.z, 1)}
		);
	}
}
