using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum TargetingMode {
	Self, //self only	//TODO: not clear if "self" is really a target mode, since it's easy to imagine both self-cardinal and arbitrary-tile-cardinal
	Pick, //one of a number of tiles
	Cardinal, //one of four angles, usually applied to a cone or line targeting region inside of a sphere
	Radial, //any Quaternion—usually applied to a cone or a line targeting region inside of a sphere
	SelectRegion, //one of a number of regions
	Path, //a specific path
	Custom
};
//waypoints are orthogonal to targeting mode, but only work for Pick and Path.

public class ActionSkill : Skill {
	public float keyboardMoveSpeed=10.0f;

	public bool lockToGrid=true;

	//if lockToGrid
	protected float lastIndicatorKeyboardMove=0;
	protected float indicatorKeyboardMoveThreshold=0.3f;

	//TODO: support for inverting grid-locked overlay
	public bool invertOverlay = false;

	public StatEffectGroup[] targetEffects;

	override public bool isPassive { get { return false; } }

	//TODO: expose in editor
	public TargetingMode targetingMode = TargetingMode.Pick;

	//TODO: expose in editor
	public Color overlayColor = new Color(0.6f, 0.3f, 0.2f, 0.7f);
	public Color highlightColor = new Color(0.9f, 0.6f, 0.4f, 0.85f);

	//cardinal/radial targeting mode
	[HideInInspector]
	public float initialFacing;

	//tile generation region (line/range/cone/etc)
	public Region targetRegion, effectRegion;
	//TODO: put into editor!
	public bool displayUnimpededTargetRegion=false;

	//io
	public bool supportKeyboard = true;
	public bool supportMouse = true;
	public bool requireConfirmation = true;
	[HideInInspector]
	public bool awaitingConfirmation = false;

	public float indicatorCycleLength=1.0f;
	virtual public MoveExecutor Executor { get { return null; } }

	bool cycleIndicatorZ = false;
	float indicatorCycleT=0;

	//grid
	[HideInInspector]
	public Overlay overlay;
	public Overlay Overlay { get { return overlay; } }
	protected GridOverlay _GridOverlay { get { return overlay as GridOverlay; } }
	protected RadialOverlay _RadialOverlay { get { return overlay as RadialOverlay; } }

	//TODO: infer from region
	public RadialOverlayType overlayType = RadialOverlayType.Sphere;
	public bool drawOverlayRim = false;
	public bool drawOverlayVolume = false;

	protected float firstClickTime = -1;
	protected float doubleClickThreshold = 0.3f;

	public Map Map { get { return map; } }
	public bool SupportKeyboard { get { return supportKeyboard; } }
	public bool SupportMouse { get { return supportMouse; } }

	public bool RequireConfirmation { get { return requireConfirmation; } }
	public bool AwaitingConfirmation {
		get { return awaitingConfirmation; }
		set { awaitingConfirmation = value; }
	}
	public float IndicatorCycleLength { get { return indicatorCycleLength; } }

	public PathNode[] targetTiles;

	public CharacterController probePrefab;
	[HideInInspector]
	[SerializeField]
	protected CharacterController probe;

	protected bool ShouldDrawPath { get { return targetingMode == TargetingMode.Path; } }
	public bool useOnlyOneWaypoint=true;

	public Material pathMaterial;

	public Vector3 selectedTile=Vector3.zero;
	[HideInInspector]
	[SerializeField]
	Vector3 initialPosition=Vector3.zero;

	[HideInInspector]
	[SerializeField]
	int nodeCount=0;

	[HideInInspector]
	[SerializeField]
	List<PathNode> waypoints;
	[HideInInspector]
	public LineRenderer lines;

	[HideInInspector]
	[SerializeField]
	PathNode endOfPath;

	[HideInInspector]
	public float radiusSoFar=0;

	public bool waypointsAreIncremental=true;
	public bool performTemporarySteps = false;

	public bool immediatelyExecuteDrawnPath=false;
	public bool canCancelWaypoints=true;

	//for path-drawing
	public float newNodeThreshold=0.05f;
	public float NewNodeThreshold { get { return lockToGrid ? 1 : newNodeThreshold; } }

	public Formula maxWaypointDistance;

	public override void Start() {
		base.Start();
		if(targetRegion == null) {
			targetRegion = new Region();
		}
		targetRegion.owner = this;
		if(effectRegion == null) {
			effectRegion = new Region();
			effectRegion.IsEffectRegion = true;
		}
		effectRegion.owner = this;
		/*
		executor = new MoveExecutor();
		executor.owner = this;
		*/
	}
	public override void ResetSkill() {
		skillName = "Attack";
		skillGroup = "Act";
	}
	public virtual void ResetActionSkill() {
		overlayColor = new Color(0.6f, 0.3f, 0.2f, 0.7f);
		highlightColor = new Color(0.9f, 0.6f, 0.4f, 0.85f);

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
	public override void Reset() {
		base.Reset();
		ResetActionSkill();
	}
	public override void Cancel() {
		if(!isActive) { return; }
		FaceDirection(initialFacing);
		//executor.Cancel();
		base.Cancel();
	}

	public virtual void FaceDirection(float ang) {
		character.Facing = ang;
	}

	public override void ActivateSkill() {
		if(isActive) { return; }
		base.ActivateSkill();
		awaitingConfirmation=false;
		selectedTile = character.TilePosition;
		initialPosition = selectedTile;
		initialFacing = character.Facing;
		nodeCount = 0;
		radiusSoFar = 0;
		endOfPath = null;
		switch(targetingMode) {
			case TargetingMode.Self:
				//??
				break;
			case TargetingMode.Pick:
			case TargetingMode.Path:
				//??
				break;
			case TargetingMode.Cardinal://??
			case TargetingMode.Radial://??
			case TargetingMode.SelectRegion://??
				break;
			case TargetingMode.Custom:
				ActivateTargetCustom();
				break;
		}
		targetRegion.owner = this;
		effectRegion.owner = this;

		PresentMoves();
	}
	public override void DeactivateSkill() {
		if(!isActive) { return; }
		overlay = null;
		if(map.IsShowingOverlay(skillName, character.gameObject.GetInstanceID())) {
			map.RemoveOverlay(skillName, character.gameObject.GetInstanceID());
		}
		if(targetingMode == TargetingMode.Custom) {
			DeactivateTargetCustom();
		}
		if(probe != null) {
			Object.Destroy(probe.gameObject);
		}
		endOfPath = null;
		radiusSoFar = 0;
		waypoints = null;
		nodeCount = 0;
		awaitingConfirmation=false;
		base.DeactivateSkill();
	}
	public override void Update() {
		base.Update();
		if(!isActive) { return; }
		targetRegion.owner = this;
		effectRegion.owner = this;
		if(character == null || !character.isActive) { return; }
		if(!arbiter.IsLocalPlayer(character.EffectiveTeamID)) {
			return;
		}
		if(GUIUtility.hotControl != 0) { return; }
		UpdateTarget();
		UpdateCancel();
	}

	protected virtual void UpdatePickOrPath() {
		if(supportMouse) {
			if(Input.GetMouseButton(0)) {
				if(targetingMode == TargetingMode.Pick) {
					cycleIndicatorZ = false;
				}
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
				if(inside && (!(ShouldDrawPath || immediatelyExecuteDrawnPath) || pn != null)) {
					//TODO: better drag controls
//				if(ShouldDrawPath && dragging) {
						//draw path: drag
						//unwind drawn path: drag backwards
//				}

					if((!awaitingConfirmation || !requireConfirmation)) {
						if(!(ShouldDrawPath || immediatelyExecuteDrawnPath)) {
							RegisterPathPoint(hitSpot);
						} else {
							Vector3 srcPos = endOfPath.pos;
							//add positions from pn back to current pos
							List<Vector3> pts = new List<Vector3>();
							while(pn != null && pn.pos != srcPos) {
								pts.Add(pn.pos);
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
							if(!waypointsAreIncremental && !immediatelyExecuteDrawnPath &&
									waypoints.Count > 0 && 
									waypoints[waypoints.Count-1].pos == hitSpot) {
								UnwindToLastWaypoint();
							} else {
								if(overlay.ContainsPosition(hitSpot)) {
									ConfirmWaypoint();
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
			(!awaitingConfirmation || !requireConfirmation)) {
			if(targetingMode == TargetingMode.Pick) {
				cycleIndicatorZ = true;
				indicatorCycleT = 0;
			}
			if(lockToGrid) {
				if((Time.time-lastIndicatorKeyboardMove) > indicatorKeyboardMoveThreshold) {
					Vector2 d = TransformKeyboardAxes(h, v);
					if(Mathf.Abs(d.x) > Mathf.Abs(d.y)) { d.x = Mathf.Sign(d.x); d.y = 0; }
					else { d.x = 0; d.y = Mathf.Sign(d.y); }
					Vector3 newDest = selectedTile;
					if(newDest.x+d.x >= 0 && newDest.y+d.y >= 0 &&
							map.HasTileAt((int)(newDest.x+d.x), (int)(newDest.y+d.y))) {
						lastIndicatorKeyboardMove = Time.time;
						newDest.x += d.x;
						newDest.y += d.y;
						newDest.z = map.NearestZLevel((int)newDest.x, (int)newDest.y, (int)newDest.z);
						if(DestIsBacktrack(newDest)) {
							UnwindPath();
						} else {
							PathNode pn = overlay.PositionAt(newDest);
							if(!(ShouldDrawPath || immediatelyExecuteDrawnPath) || (pn != null && pn.canStop)) {
								RegisterPathPoint(newDest);
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
				float thisDistance = Vector3.Distance(newDest, selectedTile);
				if(thisDistance >= NewNodeThreshold) {
					if(!(ShouldDrawPath || immediatelyExecuteDrawnPath) || (pn != null && pn.canStop)) {
						if(ShouldDrawPath) {
							lines.SetPosition(nodeCount, probe.transform.position);
						}
						RegisterPathPoint(newDest);
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
		if(targetingMode == TargetingMode.Pick &&
				cycleIndicatorZ &&
				(!awaitingConfirmation || !requireConfirmation)) {
			indicatorCycleT += Time.deltaTime;
			if(indicatorCycleT >= indicatorCycleLength) {
				indicatorCycleT -= indicatorCycleLength;
				Vector3 newSel = selectedTile;
				newSel.z = map.NextZLevel((int)newSel.x, (int)newSel.y, (int)newSel.z, true);
				RegisterPathPoint(newSel);
			}
		}
		if(supportKeyboard &&
			Input.GetButtonDown("Confirm") &&
			overlay.ContainsPosition(selectedTile)) {
			if(requireConfirmation &&
				!awaitingConfirmation) {
				awaitingConfirmation = true;
				if(performTemporarySteps) {
					TemporaryExecutePathTo(endOfPath);
				}
			} else {
				ConfirmWaypoint();
			}
		}
	}

	protected virtual void UpdateTarget() {
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");
		switch(targetingMode) {
			case TargetingMode.Self:
				if(supportKeyboard && Input.GetButtonDown("Confirm")) {
					if(awaitingConfirmation || !requireConfirmation) {
						awaitingConfirmation = false;
						Pick(character.TilePosition);
					} else {
						TentativePick(character.TilePosition);
						awaitingConfirmation = true;
					}
				}
				break;
			case TargetingMode.Cardinal:
			case TargetingMode.Radial:
				if(supportKeyboard && (h != 0 || v != 0) &&
					(!awaitingConfirmation || !requireConfirmation)) {
					Vector2 d = TransformKeyboardAxes(h, v);
					if(targetingMode == TargetingMode.Cardinal) {
						if(d.x != 0 && d.y != 0) {
							if(Mathf.Abs(d.x) > Mathf.Abs(d.y)) { d.x = Mathf.Sign(d.x); d.y = 0; }
							else { d.x = 0; d.y = Mathf.Sign(d.y); }
						}
					}
					FaceDirection(Mathf.Atan2(d.y, d.x)*Mathf.Rad2Deg);
				}
				if(supportMouse &&
					Input.GetMouseButton(0)) {
					if(!awaitingConfirmation || !requireConfirmation) {
						Vector2 d = Input.mousePosition - character.TilePosition;
						//convert d to -1..1 range in x and y
						d.x /= Screen.width/2.0f;
						d.x -= 1;
						d.y /= Screen.height/2.0f;
						d.y -= 1;
						d = TransformKeyboardAxes(d.x, d.y);
						if(targetingMode == TargetingMode.Cardinal) {
							if(d.x != 0 && d.y != 0) {
								if(Mathf.Abs(d.x) > Mathf.Abs(d.y)) { d.x = Mathf.Sign(d.x); d.y = 0; }
								else { d.x = 0; d.y = Mathf.Sign(d.y); }
							}
						}
						FaceDirection(Mathf.Atan2(d.y, d.x)*Mathf.Rad2Deg-90);
					}
					if(Input.GetMouseButtonDown(0)) {
						if(Time.time-firstClickTime > doubleClickThreshold) {
							firstClickTime = Time.time;
						} else {
							firstClickTime = -1;
							if(awaitingConfirmation) {
								PickFacing(character.Facing);
								awaitingConfirmation = false;
							} else {
								TentativePickFacing(character.Facing);
								awaitingConfirmation = true;
							}
						}
					}
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
				break;
			case TargetingMode.SelectRegion:
				//TODO: change selected region
				//keyboard: cycle
				//mouse: pick
				break;
			case TargetingMode.Pick:
			case TargetingMode.Path:
				UpdatePickOrPath();
				break;
			case TargetingMode.Custom:
				UpdateTargetCustom();
				break;
		}
	}

	protected virtual void UpdateCancel() {
		if(supportKeyboard &&
				Input.GetButtonDown("Cancel")) {
			if(targetingMode == TargetingMode.Custom) {
				CancelTargetCustom();
			} else if(targetingMode == TargetingMode.Pick ||
								targetingMode == TargetingMode.Path) {
				if(canCancelWaypoints) {
					if(requireConfirmation && awaitingConfirmation) {
						awaitingConfirmation = false;
						targetTiles = new PathNode[0];
						if(performTemporarySteps) {
							TemporaryExecutePathTo(new PathNode(Executor.position, null, 0));
						}
						ResetPosition();
					} else if(waypoints.Count > 0 && !waypointsAreIncremental && !immediatelyExecuteDrawnPath) {
						UnwindToLastWaypoint();
					} else if(endOfPath == null || endOfPath.prev == null) {
						Cancel();
					} else {
						ResetPosition();
					}
				} else {
					//we can assume we've already moved a bit, 
					//so we'll just finish up by stopping here.
					//the semantic isn't exactly cancelling, but it's close enough.
					ExecutePathTo(new PathNode(Executor.position, null, 0));
				}
			} else {
				if(awaitingConfirmation && requireConfirmation) {
					awaitingConfirmation = false;
					targetTiles = new PathNode[0];
					if(lockToGrid && _GridOverlay != null) {
						_GridOverlay.SetSelectedPoints(new Vector4[0]);
					}
				} else {
					//Back out of skill!
					Cancel();
				}
			}
		}
	}

	public override void ApplySkill() {
		//TODO: apply separately to each target
		int hitType = (int)GetParam("hitType", 0);
		currentHitType = hitType;
/*		if(targetEffects.Length == 0) {
			Debug.LogError("No effects in attack skill "+skillName+"!");
		}
*/
		if(targetEffects.Length > 0 && targetEffects[hitType].Length > 0) {
			targets = effectRegion.CharactersForTargetedTiles(targetTiles);
			ApplyEffectsTo(targetEffects[hitType].effects, targets);
		}
		base.ApplySkill();
	}

	public void CancelPick() {
		Cancel();
	}

	virtual protected PathNode[] GetPresentedActionTiles() {
		return displayUnimpededTargetRegion ? 
		  targetRegion.GetTilesInRegion() : 
		  GetValidActionTiles();
	}
	virtual protected PathNode[] GetValidActionTiles() {
		return targetRegion.GetValidTiles();
	}

	//FIXME: crasher on presenting moves for grid-locked skill!

	virtual protected void CreateOverlay() {
		//TODO: let the region create the overlay
		//TODO: overlays for weird region types, including compound
		if(overlay != null) { return; }
		if(lockToGrid) {
			PathNode[] destinations = GetPresentedActionTiles();
			overlay = map.PresentGridOverlay(
				skillName, character.gameObject.GetInstanceID(),
				overlayColor,
				highlightColor,
				destinations
			);
		} else {
			Vector3 charPos = selectedTile;
			//FIXME: minimum ranges and different z up/down ranges don't work with radial overlays
			if(overlayType == RadialOverlayType.Sphere) {
				overlay = map.PresentSphereOverlay(
					skillName, character.gameObject.GetInstanceID(),
					overlayColor,
					charPos,
					targetRegion.radiusMax - radiusSoFar,
					drawOverlayRim,
					drawOverlayVolume,
					invertOverlay
				);
			} else if(overlayType == RadialOverlayType.Cylinder) {
				overlay = map.PresentCylinderOverlay(
					skillName, character.gameObject.GetInstanceID(),
					overlayColor,
					charPos,
					targetRegion.radiusMax - radiusSoFar,
					targetRegion.zDownMax,
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
		initialPosition = character.TilePosition;
		selectedTile = initialPosition;
		if(probePrefab != null) {
			probe = Object.Instantiate(probePrefab, Vector3.zero, Quaternion.identity) as CharacterController;
			probe.transform.parent = map.transform;
			Physics.IgnoreCollision(probe.collider, character.collider);
		}
		waypoints = new List<PathNode>();
		switch(targetingMode) {
			case TargetingMode.Self:
				if(requireConfirmation) {
					TentativePick(character.TilePosition);
				} else {
					Pick(character.TilePosition);
				}
				break;
			case TargetingMode.Pick:
				cycleIndicatorZ = false;
				break;
			case TargetingMode.Cardinal://??
			case TargetingMode.Radial://??
			case TargetingMode.SelectRegion://??
				break;
			case TargetingMode.Path:
				lines = probe.gameObject.AddComponent<LineRenderer>();
				lines.materials = new Material[]{pathMaterial};
				lines.useWorldSpace = true;
				break;
			case TargetingMode.Custom:
				PresentMovesCustom();
				break;
		}
		ResetPosition();
	}
	
	
	protected virtual void ActivateTargetCustom() {
	}
	protected virtual void UpdateTargetCustom() {
	}
	protected virtual void PresentMovesCustom() {
	}
	protected virtual void DeactivateTargetCustom() {
	}
	protected virtual void CancelTargetCustom() {
	}

	public void TentativePick(Vector3 p) {
		selectedTile = p;
		targetTiles = effectRegion.GetValidTiles(p);
		_GridOverlay.SetSelectedPoints(map.CoalesceTiles(targetTiles));
	}

	public void TentativePick(PathNode pn) {
		selectedTile = pn.pos;
		targetTiles = effectRegion.GetValidTiles(pn.pos);
		_GridOverlay.SetSelectedPoints(map.CoalesceTiles(targetTiles));
	}

	public void CancelEffectPreview() {

	}

	public void Pick(Vector3 p) {
		selectedTile = p;
		targetTiles = targetRegion.ActualTilesForTargetedTiles(new PathNode[]{endOfPath});
		targetTiles = effectRegion.GetValidTiles(targetTiles[0].pos);
		ApplySkill();
	}

	public void Pick(PathNode pn) {
		selectedTile = pn.pos;
		targetTiles = targetRegion.ActualTilesForTargetedTiles(new PathNode[]{pn});
		targetTiles = effectRegion.GetValidTiles(targetTiles[0].pos);
		ApplySkill();
	}
	//targeting facings, unlike character facings (for now at least!), may be arbitrary quaternions
	public void TentativePickFacing(Quaternion f) {
		targetTiles = effectRegion.GetValidTiles(f);
		_GridOverlay.SetSelectedPoints(map.CoalesceTiles(targetTiles));
		//TODO: show preview indicator until cancelled
	}
	public void PickFacing(Quaternion f) {
		//TODO: wut???
		targetTiles = effectRegion.GetValidTiles(f);
		ApplySkill();
	}
	public void TentativePickFacing(float angle) {
		TentativePickFacing(Quaternion.Euler(0, angle, 0));
	}
	public void PickFacing(float angle) {
		PickFacing(Quaternion.Euler(0, angle, 0));
	}

	public void UnwindToLastWaypoint() {
		int priorCount = waypoints.Count;
		if(priorCount == 0) {
			ResetPosition();
		} else {
			while(waypoints.Count == priorCount) {
				UnwindPath(1);
			}
		}
	}


	protected bool CanUnwindPath { get {
		return !immediatelyExecuteDrawnPath &&
		(endOfPath.prev != null || (!waypointsAreIncremental && waypoints.Count > 0));
	} }
	public void UnwindPath(int nodes=1) {
		for(int i = 0; i < nodes && CanUnwindPath; i++) {
			Vector3 oldEnd = endOfPath.pos;
			PathNode prev = (endOfPath != null && endOfPath.prev != null) ?
				endOfPath.prev :
				(waypoints.Count > 0 ? waypoints[waypoints.Count-1] : null);
			float thisDistance = Vector2.Distance(
				new Vector2(oldEnd.x, oldEnd.y),
				new Vector2(prev.pos.x, prev.pos.y)
			);
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
			if((endOfPath == null || endOfPath.prev == null) && waypoints.Count > 0 && !waypointsAreIncremental) {
				if(endOfPath == null) {
					PathNode wp=waypoints[waypoints.Count-1], wpp=wp.prev;
					if(ShouldDrawPath) {
						endOfPath = wp.prev;
						thisDistance = Vector2.Distance(
							new Vector2(wp.pos.x, wp.pos.y),
							new Vector2(wpp.pos.x, wpp.pos.y)
						);
					} else {
						//either waypoint-2 or start
						if(waypoints.Count > 1) {
							endOfPath = waypoints[waypoints.Count-2];
						} else {
							endOfPath = new PathNode(initialPosition, null, 0);
						}
						selectedTile = endOfPath.pos;
						thisDistance = wp.xyDistanceFromStart;
					}
					if(lockToGrid) { thisDistance = (int)thisDistance; }
					radiusSoFar -= thisDistance;
				} else {
					endOfPath = waypoints[waypoints.Count-1];
				}
				waypoints.RemoveAt(waypoints.Count-1);
				PathNode startOfPath = endOfPath;
				while(startOfPath.prev != null) {
					startOfPath = startOfPath.prev;
				}
				TemporaryExecutePathTo(startOfPath);
			}
			nodeCount -= 1;
			selectedTile = endOfPath.pos;
			if(performTemporarySteps) {
				TemporaryExecutePathTo(endOfPath);
			}
			if(probe != null) {
				probe.transform.position = map.TransformPointWorld(selectedTile);
			}
			if(ShouldDrawPath) {
				lines.SetVertexCount(nodeCount+1);
				lines.SetPosition(nodeCount, probe.transform.position);
			}
		}
		//update the overlay
		UpdateOverlayParameters();
	}

	virtual protected void ResetToInitialPosition() {
		radiusSoFar = 0;
		Vector3 tp = initialPosition;
		if(lockToGrid) {
			tp.x = (int)Mathf.Round(tp.x);
			tp.y = (int)Mathf.Round(tp.y);
			tp.z = map.NearestZLevel((int)tp.x, (int)tp.y, (int)Mathf.Round(tp.z));
		}
		if(probe != null) {
			probe.transform.position = map.TransformPointWorld(tp);
		}
		selectedTile = initialPosition;
		if(ShouldDrawPath) {
			endOfPath = new PathNode(tp, null, 0);
			lines.SetVertexCount(1);
			lines.SetPosition(0, probe.transform.position);
		} else {
			endOfPath = null;
		}
		UpdateOverlayParameters();
		if(overlay != null && overlay.IsReady && lockToGrid) {
			_GridOverlay.SetSelectedPoints(new Vector4[0]);
		}
	}

	protected void ResetPosition() {
		if(waypoints.Count > 0 && !waypointsAreIncremental && !immediatelyExecuteDrawnPath) {
			UnwindToLastWaypoint();
		} else {
			ResetToInitialPosition();
		}
	}

	protected bool DestIsBacktrack(Vector3 newDest) {
		return !immediatelyExecuteDrawnPath && ShouldDrawPath && (
		(endOfPath != null && endOfPath.prev != null && newDest == endOfPath.prev.pos) ||
		(!waypointsAreIncremental && waypoints.Count > 0 &&
			(((endOfPath.prev == null) &&
			(waypoints[waypoints.Count-1].pos == newDest)) ||

			(endOfPath.prev == null &&
			waypoints[waypoints.Count-1].prev != null &&
			newDest == waypoints[waypoints.Count-1].prev.pos)
			)));
	}

	protected void ConfirmWaypoint() {
		if(!ShouldDrawPath) {
			endOfPath = overlay.PositionAt(selectedTile);
			radiusSoFar += endOfPath.xyDistanceFromStart;
		}
		if(performTemporarySteps) {
			TemporaryExecutePathTo(endOfPath);
		}
		awaitingConfirmation = false;
		if(!PermitsNewWaypoints) {
			if(!waypointsAreIncremental) {
				PathNode p = endOfPath;
				int tries = 0;
				const int tryLimit = 1000;
				while((p.prev != null || waypoints.Count > 0) && tries < tryLimit) {
					tries++;
					if(p.prev == null) {
						p.prev = waypoints[waypoints.Count-1];
						waypoints.RemoveAt(waypoints.Count-1);
					} else {
						p = p.prev;
					}
					while(p.prev != null && p.pos == p.prev.pos && p != p.prev) {
						p.prev = p.prev.prev;
					}
				}
				if(tries >= tryLimit) {
					Debug.LogError("caught infinite node loop");
				}
			}
			if(immediatelyExecuteDrawnPath) {
				endOfPath = new PathNode(selectedTile, null, 0);
			}
			ExecutePathTo(endOfPath);
		} else {
			if(immediatelyExecuteDrawnPath) {
				IncrementalExecutePathTo(new PathNode(endOfPath.pos, null, 0));
			} else if(waypointsAreIncremental) {
				IncrementalExecutePathTo(endOfPath);
			} else {
				TemporaryExecutePathTo(endOfPath);
			}
			waypoints.Add(endOfPath);
			endOfPath = new PathNode(endOfPath.pos, null, radiusSoFar);
			UpdateOverlayParameters();
		}
	}
	virtual protected void TemporaryExecutePathTo(PathNode p) {
		//pick? face? dunno?
		TentativePick(p);
	}

	virtual protected void IncrementalExecutePathTo(PathNode pn) {
		TentativePick(pn); //??
	}
	virtual protected void ExecutePathTo(PathNode pn) {
		Pick(pn);
	}

	protected void UpdateOverlayParameters() {
		if(overlay == null) { return; }
		//HACK: to avoid Unity crashers when I create and update an overlay on the same frame.
		if(!overlay.IsReady) {
			StartCoroutine(UpdateOverlayNextFrame());
			return;
		}
		if(lockToGrid) {
			_GridOverlay.UpdateDestinations(GetPresentedActionTiles());
		} else {
			Vector3 charPos = selectedTile;
			_RadialOverlay.tileRadius = (targetRegion.radiusMax - radiusSoFar);
			_RadialOverlay.UpdateOriginAndRadius(
				map.TransformPointWorld(charPos),
				(targetRegion.radiusMax - radiusSoFar)*map.sideLength
			);
		}
	}

	protected IEnumerator UpdateOverlayNextFrame() {
		yield return new WaitForFixedUpdate();
		UpdateOverlayParameters();
	}

	protected bool PermitsNewWaypoints { get {
		if(immediatelyExecuteDrawnPath) { return false; }
		if(useOnlyOneWaypoint || maxWaypointDistance == null) { return false; }
		return ((radiusSoFar + newNodeThreshold) < maxWaypointDistance.GetValue(this, null, null));
	} }

	protected void RegisterPathPoint(Vector3 newDest, bool backwards=false) {
		float thisDistance = Vector3.Distance(newDest, selectedTile);
		if(lockToGrid) {
			thisDistance = (int)thisDistance;
		}
		selectedTile = newDest;
		if(!ShouldDrawPath) {
			endOfPath = new PathNode(selectedTile, null, 0);
		} else {
			radiusSoFar += thisDistance;
			endOfPath = new PathNode(selectedTile, endOfPath, radiusSoFar);
			//add a line to this point
			nodeCount += 1;
			lines.SetVertexCount(nodeCount+1);
			lines.SetPosition(nodeCount, map.TransformPointWorld(selectedTile));
			if(performTemporarySteps) {
				TemporaryExecutePathTo(endOfPath);
			}
		}
		if(immediatelyExecuteDrawnPath) {
			IncrementalExecutePathTo(new PathNode(newDest, null, 0));
		}
		if(ShouldDrawPath) {
			//update the overlay
			UpdateOverlayParameters();
			if(lockToGrid) {
				Vector4[] selPts = _GridOverlay.selectedPoints ?? new Vector4[0];
				_GridOverlay.SetSelectedPoints(selPts.Concat(
					new Vector4[]{new Vector4(newDest.x, newDest.y, newDest.z, 1)}
				).ToArray());
			}
		} else {
			if(lockToGrid) {
				_GridOverlay.SetSelectedPoints(
					new Vector4[]{new Vector4(newDest.x, newDest.y, newDest.z, 1)}
				);
			}
		}
		if(probe != null) {
			probe.transform.position = map.TransformPointWorld(selectedTile);
		}
	}
}
