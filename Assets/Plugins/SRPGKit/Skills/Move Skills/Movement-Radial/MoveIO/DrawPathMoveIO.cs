using UnityEngine;
using System.Collections;
using System.Linq;

[System.Serializable]
public class DrawPathMoveIO : MoveIO {
	public float moveSpeed=10.0f;
	
	public Color overlayColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);

	[SerializeField]
	Vector3 moveDest=Vector3.zero;
	
	public int nodeCount=0;

	[SerializeField]
	PathNode endOfPath;
	
	public CharacterController probePrefab;
	[SerializeField]
	CharacterController probe;
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
	
	public float xyRangeSoFar=0;
	
	protected void UpdateOverlay() {
		if(lockToGrid) {
			PathNode[] destinations = owner.Strategy.GetValidMoves(
				moveDest, 
				0, owner.Strategy.xyRangeMax-xyRangeSoFar, 
				0, owner.Strategy.zRangeDownMax, 
				0, owner.Strategy.zRangeUpMax
			);
			if(overlay != null) {
				(overlay as GridOverlay).UpdateDestinations(destinations);
			} else {
				overlay = owner.map.PresentGridOverlay(
					owner.skillName, owner.character.gameObject.GetInstanceID(), 
					overlayColor,
					overlayColor,
					destinations
				);
			}
		} else {
			Vector3 charPos = moveDest;
			if(overlay != null) {
				(overlay as RadialOverlay).tileRadius = (owner.Strategy.xyRangeMax - xyRangeSoFar);
				(overlay as RadialOverlay).UpdateOriginAndRadius(
					owner.map.TransformPointWorld(charPos), 
					(owner.Strategy.xyRangeMax - xyRangeSoFar)*owner.map.sideLength
				);
			} else {
				if(overlayType == RadialOverlayType.Sphere) {
					overlay = owner.map.PresentSphereOverlay(
								owner.skillName, owner.character.gameObject.GetInstanceID(), 
								overlayColor,
								charPos,
								owner.Strategy.xyRangeMax - xyRangeSoFar,
								drawOverlayRim,
								drawOverlayVolume,
								invertOverlay
							);
				} else if(overlayType == RadialOverlayType.Cylinder) {
					overlay = owner.map.PresentCylinderOverlay(
								owner.skillName, owner.character.gameObject.GetInstanceID(), 
								overlayColor,
								charPos,
								owner.Strategy.xyRangeMax - xyRangeSoFar,
								owner.Strategy.zRangeDownMax,
								drawOverlayRim,
								drawOverlayVolume,
								invertOverlay
							);
				}	
			}
		}	
	}
	
	override public void Activate() {
		base.Activate();
		moveDest = owner.character.TilePosition;
		xyRangeSoFar = 0;
		xyRangeSoFar = 0;
		nodeCount = 0;
		endOfPath = null;
	}
	
	override public void Deactivate() {
		base.Deactivate();
		Object.Destroy(probe.gameObject);
		endOfPath = null;
		xyRangeSoFar = 0;
		nodeCount = 0;
	}
	
	override public void Update () {
		base.Update();
		if(owner.character == null || !owner.character.isActive) { return; }
		if(!isActive) { return; }
		if(!owner.arbiter.IsLocalPlayer(owner.character.EffectiveTeamID)) {
			return;
		}
/*		if(supportMouse && Input.GetMouseButton(0)) {
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			Vector3 hitSpot;
			bool inside = overlay.Raycast(r, out hitSpot);
			if(inside && overlay.ContainsPosition(hitSpot)) {
				moveDest = hitSpot;
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
		}*/
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");
		if(supportKeyboard && (h != 0 || v != 0)) {
			if(lockToGrid) {
			  if((Time.time-lastIndicatorKeyboardMove) > indicatorKeyboardMoveThreshold) {
					Vector2 d = owner.map.TransformKeyboardAxes(h, v);
					if(Mathf.Abs(d.x) > Mathf.Abs(d.y)) { d.x = Mathf.Sign(d.x); d.y = 0; }
					else { d.x = 0; d.y = Mathf.Sign(d.y); }
					Vector3 newDest = moveDest;
					if(newDest.x+d.x >= 0 && newDest.y+d.y >= 0 &&
					 	 owner.map.HasTileAt((int)(newDest.x+d.x), (int)(newDest.y+d.y))) {
						lastIndicatorKeyboardMove = Time.time;
						newDest.x += d.x;
						newDest.y += d.y;
						newDest.z = owner.map.NearestZLevel((int)newDest.x, (int)newDest.y, (int)newDest.z);
						if(endOfPath != null && endOfPath.prev != null && newDest == endOfPath.prev.pos) {
							UnwindPath();
						} else {
							PathNode pn = overlay.PositionAt(newDest);
							if(pn != null && pn.canStop) {
								probe.transform.position = owner.map.TransformPointWorld(newDest);
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
				
				Vector3 newDest = owner.map.InverseTransformPointWorld(probe.transform.position);
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
			PerformMoveToPathNode(endOfPath);
		}
	}
	
	protected void UpdatePath(Vector3 newDest, bool backwards=false) {
		float thisDistance = Vector3.Distance(newDest, moveDest);
		if(lockToGrid) {
			thisDistance = (int)thisDistance;
			Vector4[] selPts = (overlay as GridOverlay).selectedPoints ?? new Vector4[0];
			(overlay as GridOverlay).SetSelectedPoints(selPts.Concat(
				new Vector4[]{new Vector4(newDest.x, newDest.y, newDest.z, 1)}
			).ToArray());
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
		lines.SetPosition(nodeCount, owner.map.TransformPointWorld(moveDest));
		//update the overlay
		UpdateOverlay();
	}
	
	public void UnwindPath(int nodes=1) {
		for(int i = 0; i < nodes && endOfPath.prev != null; i++) {
			Vector3 oldEnd = endOfPath.pos;
			float thisDistance = Vector3.Distance(oldEnd, endOfPath.prev.pos);
			if(lockToGrid) {
				thisDistance = (int)thisDistance;
				Vector4[] selPts = (overlay as GridOverlay).selectedPoints ?? new Vector4[0];
				(overlay as GridOverlay).SetSelectedPoints(selPts.Except(new Vector4[]{new Vector4(oldEnd.x, oldEnd.y, oldEnd.z, 1)}).ToArray());
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
			probe.transform.position = owner.map.TransformPointWorld(moveDest);
			lines.SetPosition(nodeCount, probe.transform.position);
		}
		//update the overlay
		UpdateOverlay();	
	}
	
	override public void PresentMoves() {
		base.PresentMoves();
		moveDest = owner.character.TilePosition;
		probe = Object.Instantiate(probePrefab, owner.map.TransformPointWorld(moveDest), Quaternion.identity) as CharacterController;
		probe.transform.parent = owner.map.transform;
		Physics.IgnoreCollision(probe.collider, owner.character.collider);
		Vector3 tp = owner.character.TilePosition;
		if(lockToGrid) {
			tp.x = (int)Mathf.Round(tp.x);
			tp.x = (int)Mathf.Round(tp.y);
			tp.z = owner.map.NearestZLevel((int)tp.x, (int)tp.y, (int)Mathf.Round(tp.z));
		}
		probe.transform.position = owner.map.TransformPointWorld(tp);
		endOfPath = new PathNode(tp, null, 0);
		lines = probe.gameObject.AddComponent<LineRenderer>();
		lines.useWorldSpace = true;
		lines.SetVertexCount(1);
		lines.SetPosition(0, probe.transform.position);
		UpdateOverlay();
	}
	
	override protected void FinishMove() {
		overlay = null;
		if(owner.map.IsShowingOverlay(owner.skillName, owner.character.gameObject.GetInstanceID())) {
			owner.map.RemoveOverlay(owner.skillName, owner.character.gameObject.GetInstanceID());
		}	
		base.FinishMove();
	}
}
