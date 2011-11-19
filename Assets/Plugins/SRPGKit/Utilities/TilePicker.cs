using UnityEngine;

public interface ITilePickerOwner {
	Map Map { get; }
	void Pick(TilePicker tp, Vector3 p);
	void Pick(TilePicker tp, PathNode pn);
	void TentativePick(TilePicker tp, Vector3 p);
	void TentativePick(TilePicker tp, PathNode pn);
	void CancelPick(TilePicker tp);
}

public class TilePicker {
	public bool supportKeyboard = true;
	public bool supportMouse = true;
	
	public bool requireConfirmation = true;
	
	public bool awaitingConfirmation = false;
	
	float firstClickTime = -1;
	float doubleClickThreshold = 0.3f;
	
	bool cycleIndicatorZ = false;
	float indicatorCycleT=0;
	
	float lastIndicatorKeyboardMove=0;
	float indicatorKeyboardMoveThreshold=0.3f;
	
	Vector2 indicatorXY=Vector2.zero;
	float indicatorZ=0;
	public float indicatorCycleLength=1.0f;
	
	public Map map;
	
	public GridOverlay overlay;
	
	public MoveExecutor moveExecutor;
	
	public ITilePickerOwner owner;
	
	public void Update() {
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
			      	owner.Pick(this, pn);
						} else {
							owner.TentativePick(this, indicatorSpot);
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
			Vector2 d = owner.Map.TransformKeyboardAxes(h, v);
			Debug.Log("Got "+d);
			if(Mathf.Abs(d.x) > Mathf.Abs(d.y)) { d.x = Mathf.Sign(d.x); d.y = 0; }
			else { d.x = 0; d.y = Mathf.Sign(d.y); }
			Debug.Log("Became "+d);
			if(indicatorXY.x+d.x >= 0 && indicatorXY.y+d.y >= 0 &&
				 map.HasTileAt((int)(indicatorXY.x+d.x), (int)(indicatorXY.y+d.y))) {
				lastIndicatorKeyboardMove = Time.time;
				indicatorXY.x += d.x;
				indicatorXY.y += d.y;
				indicatorZ = map.NearestZLevel((int)indicatorXY.x, (int)indicatorXY.y, (int)indicatorZ);
			}
		}
		if(supportKeyboard && 
			 Input.GetButtonDown("Confirm")) {
			Vector3 indicatorSpot = new Vector3(indicatorXY.x, indicatorXY.y, indicatorZ);
			PathNode pn = overlay.PositionAt(indicatorSpot);
			if(pn != null && pn.canStop) {
				if(awaitingConfirmation || !requireConfirmation) {
					owner.Pick(this, pn);
  	    	awaitingConfirmation = false;
				} else if(requireConfirmation) {
					owner.TentativePick(this, indicatorSpot);
					awaitingConfirmation = true;
				}
			}
		}
		if(supportKeyboard && Input.GetButtonDown("Cancel")) {
			if(awaitingConfirmation && requireConfirmation) {
				owner.TentativePick(this, map.InverseTransformPointWorld(moveExecutor.position));
				awaitingConfirmation = false;
			} else {
				owner.CancelPick(this);
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
	public void PresentOverlay(GridOverlay overlay, Vector3 charPos) {
		this.overlay = overlay;
		if(requireConfirmation) {
			awaitingConfirmation = false;
		}
		FocusOnPoint(charPos);
	}
	public void Clear() {
		overlay = null;
		awaitingConfirmation = false;
	}
	
	public void FocusOnPoint(Vector3 pos) {
		cycleIndicatorZ = false;
		indicatorXY = new Vector2(Mathf.Floor(pos.x), Mathf.Floor(pos.y));
		indicatorZ = map.NearestZLevel((int)indicatorXY.x, (int)indicatorXY.y, (int)Mathf.Floor(pos.z));
		MapTile t = map.TileAt((int)indicatorXY.x, (int)indicatorXY.y, (int)indicatorZ);
		overlay.selectedPoint = new Vector4((int)indicatorXY.x, (int)indicatorXY.y, t.z, t.maxZ);
	}
}