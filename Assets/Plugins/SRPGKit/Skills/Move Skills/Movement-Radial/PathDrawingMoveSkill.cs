using UnityEngine;
using System.Collections;
using System.Linq;

public class PathDrawingMoveSkill : MoveSkill {
	public float moveSpeed=10.0f;
	
	public Color overlayColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
	public Color highlightColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
	public Material pathMaterial;

	[SerializeField]
	Vector3 moveDest=Vector3.zero;
	
	[SerializeField]
	int nodeCount=0;

	[SerializeField]
	PathNode endOfPath;
	
	public CharacterController probePrefab;
	[SerializeField]
	CharacterController probe;
	[HideInInspector]
	public LineRenderer lines;
	
	public float newNodeThreshold=0.05f;
	
	public float NewNodeThreshold { get { return lockToGrid ? 1 : newNodeThreshold; } }
	
	//if lockToGrid
	float lastIndicatorKeyboardMove=0;
	float indicatorKeyboardMoveThreshold=0.3f;

	//if !lockToGrid
	public RadialOverlayType overlayType = RadialOverlayType.Sphere;
	[SerializeField]
	Overlay overlay;
	//TODO: expose to editor
	public bool drawOverlayRim = false;
	public bool drawOverlayVolume = false;
	//TODO: support for grid-locking
	public bool invertOverlay = true; //draw an overlay on the map's exterior
	
	[HideInInspector]
	public float xyRangeSoFar=0;
	
	protected GridOverlay _GridOverlay { get { return overlay as GridOverlay; } }	
	protected RadialOverlay _RadialOverlay { get { return overlay as RadialOverlay; } }	
	
	protected void UpdateOverlay() {
		if(lockToGrid) {
			PathNode[] destinations = Strategy.GetValidMoves(
				moveDest, 
				0, Strategy.xyRangeMax-xyRangeSoFar, 
				0, Strategy.zRangeDownMax, 
				0, Strategy.zRangeUpMax
			);
			if(overlay != null) {
				_GridOverlay.UpdateDestinations(destinations);
			} else {
				overlay = map.PresentGridOverlay(
					skillName, character.gameObject.GetInstanceID(), 
					overlayColor,
					highlightColor,
					destinations
				);
			}
		} else {
			Vector3 charPos = moveDest;
			if(overlay != null) {
				_RadialOverlay.tileRadius = (Strategy.xyRangeMax - xyRangeSoFar);
				_RadialOverlay.UpdateOriginAndRadius(
					map.TransformPointWorld(charPos), 
					(Strategy.xyRangeMax - xyRangeSoFar)*map.sideLength
				);
			} else {
				if(overlayType == RadialOverlayType.Sphere) {
					overlay = map.PresentSphereOverlay(
						skillName, character.gameObject.GetInstanceID(), 
						overlayColor,
						charPos,
						Strategy.xyRangeMax - xyRangeSoFar,
						drawOverlayRim,
						drawOverlayVolume,
						invertOverlay
					);
				} else if(overlayType == RadialOverlayType.Cylinder) {
					overlay = map.PresentCylinderOverlay(
						skillName, character.gameObject.GetInstanceID(), 
						overlayColor,
						charPos,
						Strategy.xyRangeMax - xyRangeSoFar,
						Strategy.zRangeDownMax,
						drawOverlayRim,
						drawOverlayVolume,
						invertOverlay
					);
				}
			}
		}
	}
	
	override public void ActivateSkill() {
		moveDest = character.TilePosition;
		xyRangeSoFar = 0;
		xyRangeSoFar = 0;
		nodeCount = 0;
		endOfPath = null;
		awaitingConfirmation=false;
		base.ActivateSkill();
	}
	
	override public void DeactivateSkill() {
		base.DeactivateSkill();
		Object.Destroy(probe.gameObject);
		endOfPath = null;
		xyRangeSoFar = 0;
		nodeCount = 0;
		awaitingConfirmation=false;
		if(map.IsShowingOverlay(skillName, character.gameObject.GetInstanceID())) {
			map.RemoveOverlay(skillName, character.gameObject.GetInstanceID());
		}	
		overlay = null;
	}
	
	override public void Update () {
		base.Update();
		if(character == null || !character.isActive) { return; }
		if(!isActive) { return; }
		if(!arbiter.IsLocalPlayer(character.EffectiveTeamID)) {
			return;
		}
		
/*		if(supportMouse && Input.GetMouseButton(0)) {
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			Vector3 hitSpot;
			bool inside = overlay.Raycast(r, out hitSpot);
			if(inside && overlay.ContainsPosition(hitSpot)) {
				moveDest = hitSpot;
				//move the probe here
				IncrementalMove(moveDest);
				if(Input.GetMouseButtonDown(0)) {
					if(Time.time-firstClickTime > doubleClickThreshold) {
						firstClickTime = Time.time;
					} else  {
						firstClickTime = -1;
						PerformMove(moveDest);
					}
				}
			}
		}
		*/
		
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");
		if(supportKeyboard && (h != 0 || v != 0)) {
			if(lockToGrid) {
			  if((Time.time-lastIndicatorKeyboardMove) > indicatorKeyboardMoveThreshold) {
					Vector2 d = map.TransformKeyboardAxes(h, v);
					if(Mathf.Abs(d.x) > Mathf.Abs(d.y)) { d.x = Mathf.Sign(d.x); d.y = 0; }
					else { d.x = 0; d.y = Mathf.Sign(d.y); }
					Vector3 newDest = moveDest;
					if(newDest.x+d.x >= 0 && newDest.y+d.y >= 0 &&
					 	 map.HasTileAt((int)(newDest.x+d.x), (int)(newDest.y+d.y))) {
						lastIndicatorKeyboardMove = Time.time;
						newDest.x += d.x;
						newDest.y += d.y;
						newDest.z = map.NearestZLevel((int)newDest.x, (int)newDest.y, (int)newDest.z);
						if(endOfPath != null && endOfPath.prev != null && newDest == endOfPath.prev.pos) {
							UnwindPath();
						} else {
							PathNode pn = overlay.PositionAt(newDest);
							if(pn != null && pn.canStop) {
								probe.transform.position = map.TransformPointWorld(newDest);
								UpdatePath(newDest);
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
				probe.SimpleMove(offset*moveSpeed);
				
				Vector3 newDest = map.InverseTransformPointWorld(probe.transform.position);
				PathNode pn = overlay.PositionAt(newDest);
				if(pn != null && pn.canStop) {
					lines.SetPosition(nodeCount, probe.transform.position);
					float thisDistance = Vector3.Distance(newDest, moveDest);
					if(thisDistance >= NewNodeThreshold) {
						UpdatePath(newDest);
					}				
				} else {
					probe.transform.position = lastProbePos;
				}
			}
		}
		if(supportKeyboard && Input.GetButtonDown("Confirm")) {
			if(requireConfirmation && !awaitingConfirmation) {
				awaitingConfirmation = true;
				if(performTemporaryMoves) {
					TemporaryMoveToPathNode(endOfPath);
				}
			} else {
				awaitingConfirmation = false;
				PerformMoveToPathNode(endOfPath);
			}
		}
		if(supportKeyboard && Input.GetButtonDown("Cancel")) {
			if(requireConfirmation && awaitingConfirmation) {
				awaitingConfirmation = false;
				if(performTemporaryMoves) {
					TemporaryMove(Executor.position);
				}
				ResetPosition();
			} else {
				Cancel();
			}
		}
	}
	
	protected void UpdatePath(Vector3 newDest, bool backwards=false) {
		float thisDistance = Vector3.Distance(newDest, moveDest);
		if(lockToGrid) {
			thisDistance = (int)thisDistance;
		}
		moveDest = newDest;
		xyRangeSoFar += thisDistance;
		endOfPath = new PathNode(moveDest, endOfPath, xyRangeSoFar);
		if(performTemporaryMoves) {
			TemporaryMoveToPathNode(endOfPath);
		}
		//add a line to this point
		nodeCount += 1;
		lines.SetVertexCount(nodeCount+1);
		lines.SetPosition(nodeCount, map.TransformPointWorld(moveDest));
		//update the overlay
		UpdateOverlay();
		if(lockToGrid) {
			Vector4[] selPts = _GridOverlay.selectedPoints ?? new Vector4[0];
			_GridOverlay.SetSelectedPoints(selPts.Concat(
				new Vector4[]{new Vector4(newDest.x, newDest.y, newDest.z, 1)}
			).ToArray());
		}
	}
	
	public void UnwindPath(int nodes=1) {
		for(int i = 0; i < nodes && endOfPath.prev != null; i++) {
			Vector3 oldEnd = endOfPath.pos;
			float thisDistance = Vector3.Distance(oldEnd, endOfPath.prev.pos);
			if(lockToGrid) {
				thisDistance = (int)thisDistance;
				Vector4[] selPts = _GridOverlay.selectedPoints ?? new Vector4[0];
				_GridOverlay.SetSelectedPoints(selPts.Except(new Vector4[]{new Vector4(oldEnd.x, oldEnd.y, oldEnd.z, 1)}).ToArray());
			}
			xyRangeSoFar -= thisDistance;
			endOfPath = endOfPath.prev;
			moveDest = endOfPath.pos;
			if(performTemporaryMoves) {
				TemporaryMoveToPathNode(endOfPath);
			}
			//add a line to this point
			nodeCount -= 1;
			lines.SetVertexCount(nodeCount+1);
			probe.transform.position = map.TransformPointWorld(moveDest);
			lines.SetPosition(nodeCount, probe.transform.position);
		}
		//update the overlay
		UpdateOverlay();	
	}
	
	override protected void PresentMoves() {
		base.PresentMoves();
		moveDest = character.TilePosition;
		probe = Object.Instantiate(probePrefab, Vector3.zero, Quaternion.identity) as CharacterController;
		probe.transform.parent = map.transform;
		Physics.IgnoreCollision(probe.collider, character.collider);
		lines = probe.gameObject.AddComponent<LineRenderer>();
		lines.materials = new Material[]{pathMaterial};
		lines.useWorldSpace = true;
		ResetPosition();
	}
	
	protected void ResetPosition() {
		Vector3 tp = character.TilePosition;
		if(lockToGrid) {
			tp.x = (int)Mathf.Round(tp.x);
			tp.x = (int)Mathf.Round(tp.y);
			tp.z = map.NearestZLevel((int)tp.x, (int)tp.y, (int)Mathf.Round(tp.z));
		}
		probe.transform.position = map.TransformPointWorld(tp);
		endOfPath = new PathNode(tp, null, 0);
		lines.SetVertexCount(1);
		lines.SetPosition(0, probe.transform.position);
		UpdateOverlay();
		if(lockToGrid) {
			_GridOverlay.SetSelectedPoints(new Vector4[0]);
		}
	}	
	
}
