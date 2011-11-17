using UnityEngine;
using System.Collections.Generic;

public class PickTileMoveIO : MoveIO {
	public bool supportKeyboard = true;
	public bool supportMouse = true;
	
	public bool requireConfirmation = true;
	
	public bool awaitingConfirmation = false;
	
	public GridOverlay overlay;
	
	float firstClickTime = -1;
	float doubleClickThreshold = 0.3f;
	
	bool cycleIndicatorZ = false;
	float indicatorCycleT=0;
	public float indicatorCycleLength=1.0f;
	
	float lastIndicatorKeyboardMove=0;
	float indicatorKeyboardMoveThreshold=0.3f;
	
	Vector2 indicatorXY=Vector2.zero;
	float indicatorZ=0;
	
	override public void Update () {
		base.Update();
		if(character == null || !character.isActive) { return; }
		if(!isActive) { return; }
		if(!map.arbiter.IsLocalPlayer(character.EffectiveTeamID)) {
			return;
		}
		if(GUIUtility.hotControl != 0) { return; }
		MoveExecutor me = GetComponent<MoveExecutor>();
		if(me.IsMoving) { return; }
		if(supportMouse && Input.GetMouseButton(0) && (!awaitingConfirmation || !requireConfirmation)) {
			cycleIndicatorZ = false;
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			Vector3 hitSpot;
			bool inside = overlay.Raycast(r, out hitSpot);
			hitSpot.x = Mathf.Floor(hitSpot.x+0.5f);
			hitSpot.y = Mathf.Floor(hitSpot.y+0.5f);
			hitSpot.z = Mathf.Floor(hitSpot.z+0.5f);
			indicatorXY = new Vector2(hitSpot.x, hitSpot.y);
			indicatorZ = map.NearestZLevel((int)indicatorXY.x, (int)indicatorXY.y, (int)hitSpot.z);
			if(Input.GetMouseButtonDown(0) && Time.time-firstClickTime < doubleClickThreshold) {
				firstClickTime = -1;
				Vector3 indicatorSpot = new Vector3(indicatorXY.x, indicatorXY.y, indicatorZ);
				if(inside && overlay.ContainsPosition(indicatorSpot)) {
					PathNode pn = overlay.PositionAt(indicatorSpot);
					if(pn != null && pn.canStop) {
						if(!requireConfirmation) {
			      	PerformMoveToPathNode(pn);
						} else {
							TemporaryMove(indicatorSpot);
							awaitingConfirmation = true;
						}
					}
				}
			} else {
				if(Input.GetMouseButtonDown(0)) {
					firstClickTime = Time.time;
				}
			}
		}
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");
		if(supportKeyboard && 
			(h != 0 || v != 0) && 
		  ((Time.time-lastIndicatorKeyboardMove) > indicatorKeyboardMoveThreshold) &&
		  (!awaitingConfirmation || !requireConfirmation)) {
			cycleIndicatorZ = true;
			indicatorCycleT = 0;
			float dx = (h == 0 ? 0 : Mathf.Sign(h));
			float dy = (v == 0 ? 0 : Mathf.Sign(v));
			if(indicatorXY.x+dx >= 0 && indicatorXY.y+dy >= 0 &&
				 map.HasTileAt((int)(indicatorXY.x+dx), (int)(indicatorXY.y+dy))) {
				lastIndicatorKeyboardMove = Time.time;
				indicatorXY.x += dx;
				indicatorXY.y += dy;
				indicatorZ = map.NearestZLevel((int)indicatorXY.x, (int)indicatorXY.y, (int)indicatorZ);
			}
		}
		if(supportKeyboard && 
			 Input.GetButtonDown("Confirm")) {
			Vector3 indicatorSpot = new Vector3(indicatorXY.x, indicatorXY.y, indicatorZ);
			PathNode pn = overlay.PositionAt(indicatorSpot);
			if(pn != null && pn.canStop) {
				if(awaitingConfirmation || !requireConfirmation) {
	      	PerformMoveToPathNode(pn);
	      	awaitingConfirmation = false;
				} else if(requireConfirmation) {
					TemporaryMove(indicatorSpot);
					awaitingConfirmation = true;
				}
			}
		}
		if(supportKeyboard && Input.GetButtonDown("Cancel")) {
			if(awaitingConfirmation && requireConfirmation) {
				TemporaryMove(map.InverseTransformPointWorld(me.position));
				awaitingConfirmation = false;
			} else {
				//Back out of move phase!
			}
		}
		if(cycleIndicatorZ && (!awaitingConfirmation || !requireConfirmation)) {
			indicatorCycleT += Time.deltaTime;
			if(indicatorCycleT >= indicatorCycleLength) {
				indicatorCycleT -= indicatorCycleLength;
				indicatorZ = map.NextZLevel((int)indicatorXY.x, (int)indicatorXY.y, (int)indicatorZ, true);
			}
		}
		if(overlay != null) {
			MapTile t = map.TileAt((int)indicatorXY.x, (int)indicatorXY.y, (int)indicatorZ);
			if(t != null) {
				overlay.selectedPoint = new Vector4((int)indicatorXY.x, (int)indicatorXY.y, t.z, t.maxZ);
			}
		}
	}
	
	public Vector3 IndicatorPosition {
		get { return new Vector3(indicatorXY.x, indicatorXY.y, indicatorZ); }
	}	
	
	override protected void PresentMoves() {
		base.PresentMoves();
//		Debug.Log("present");
		if(requireConfirmation) {
			awaitingConfirmation = false;
		}
		GridMoveStrategy ms = GetComponent<GridMoveStrategy>();
		PathNode[] destinations = ms.GetValidMoves();
		MoveExecutor me = GetComponent<MoveExecutor>();
		Vector3 charPos = map.InverseTransformPointWorld(me.position);
		overlay = map.PresentGridOverlay(
			"move", this.gameObject.GetInstanceID(), 
			new Color(0.2f, 0.3f, 0.9f, 0.7f),
			new Color(0.4f, 0.6f, 0.9f, 0.85f),
			destinations
		);
		FocusOnPoint(charPos);
	}
	
	override protected void FinishMove() {
		overlay = null;
		if(map.IsShowingOverlay("move", this.gameObject.GetInstanceID())) {
			map.RemoveOverlay("move", this.gameObject.GetInstanceID());
		}	
		base.FinishMove();
	}
	
	public void FocusOnPoint(Vector3 pos) {
		cycleIndicatorZ = false;
		indicatorXY = new Vector2(Mathf.Floor(pos.x), Mathf.Floor(pos.y));
		indicatorZ = map.NearestZLevel((int)indicatorXY.x, (int)indicatorXY.y, (int)Mathf.Floor(pos.z));
		MapTile t = map.TileAt((int)indicatorXY.x, (int)indicatorXY.y, (int)indicatorZ);
		overlay.selectedPoint = new Vector4((int)indicatorXY.x, (int)indicatorXY.y, t.z, t.maxZ);
	}
	
	public override void TemporaryMoveToPathNode(PathNode pn) {
		FocusOnPoint(pn.pos);
		base.TemporaryMoveToPathNode(pn);
	}

	public override void IncrementalMoveToPathNode(PathNode pn) {
		FocusOnPoint(pn.pos);
		base.IncrementalMoveToPathNode(pn);
	}
	
	public override void PerformMoveToPathNode(PathNode pn) {
		FocusOnPoint(pn.pos);
		base.PerformMoveToPathNode(pn);
	}	
	
}
