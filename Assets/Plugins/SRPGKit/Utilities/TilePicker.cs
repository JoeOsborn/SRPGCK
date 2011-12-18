using UnityEngine;

public interface ITilePickerOwner {
	Map Map { get; }
	void Pick(TilePicker tp, Vector3 p);
	void Pick(TilePicker tp, PathNode pn);
	void TentativePick(TilePicker tp, Vector3 p);
	void TentativePick(TilePicker tp, PathNode pn);
	void CancelPick(TilePicker tp);

	bool SupportKeyboard { get; }
	bool SupportMouse { get; }
	bool RequireConfirmation { get; }
	bool AwaitingConfirmation { get; set; }
	float IndicatorCycleLength { get; }
	GridOverlay Overlay { get; }
	MoveExecutor Executor { get; }
}

[System.Serializable]
public class TilePicker {
	float firstClickTime = -1;
	float doubleClickThreshold = 0.3f;
	
	bool cycleIndicatorZ = false;
	float indicatorCycleT=0;
	
	float lastIndicatorKeyboardMove=0;
	float indicatorKeyboardMoveThreshold=0.3f;
	
	[SerializeField]
	Vector2 indicatorXY;	
	[SerializeField]
	float indicatorZ=0;

	Vector3 lastIndicator=Vector3.zero;
	
	public ITilePickerOwner owner;
	
	public bool AwaitingConfirmation {
		get { return owner.AwaitingConfirmation; }
		set { owner.AwaitingConfirmation = value; if(!AwaitingConfirmation) { UpdateSelection(); } }
	}
	
	public void Update() {
		if(owner.SupportMouse && Input.GetMouseButton(0) && (!AwaitingConfirmation || !owner.RequireConfirmation)) {
			cycleIndicatorZ = false;
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			Vector3 hitSpot;
			bool inside = owner.Overlay.Raycast(r, out hitSpot);
			hitSpot.x = Mathf.Floor(hitSpot.x+0.5f);
			hitSpot.y = Mathf.Floor(hitSpot.y+0.5f);
			hitSpot.z = Mathf.Floor(hitSpot.z+0.5f);
			indicatorXY = new Vector2(hitSpot.x, hitSpot.y);
			indicatorZ = owner.Map.NearestZLevel((int)indicatorXY.x, (int)indicatorXY.y, (int)hitSpot.z);
			if(Input.GetMouseButtonDown(0) && Time.time-firstClickTime < doubleClickThreshold) {
				firstClickTime = -1;
				Vector3 indicatorSpot = new Vector3(indicatorXY.x, indicatorXY.y, indicatorZ);
				if(inside && owner.Overlay.ContainsPosition(indicatorSpot)) {
					PathNode pn = owner.Overlay.PositionAt(indicatorSpot);
					if(pn != null && pn.canStop) {
						if(!owner.RequireConfirmation) {
			      	owner.Pick(this, pn);
						} else {
							owner.TentativePick(this, indicatorSpot);
							AwaitingConfirmation = true;
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
		if(owner.SupportKeyboard && 
			(h != 0 || v != 0) && 
		  ((Time.time-lastIndicatorKeyboardMove) > indicatorKeyboardMoveThreshold) &&
		  (!AwaitingConfirmation || !owner.RequireConfirmation)) {
			cycleIndicatorZ = true;
			indicatorCycleT = 0;
			Vector2 d = owner.Map.TransformKeyboardAxes(h, v);
			if(Mathf.Abs(d.x) > Mathf.Abs(d.y)) { d.x = Mathf.Sign(d.x); d.y = 0; }
			else { d.x = 0; d.y = Mathf.Sign(d.y); }
			if(indicatorXY.x+d.x >= 0 && indicatorXY.y+d.y >= 0 &&
				 owner.Map.HasTileAt((int)(indicatorXY.x+d.x), (int)(indicatorXY.y+d.y))) {
				lastIndicatorKeyboardMove = Time.time;
				indicatorXY.x += d.x;
				indicatorXY.y += d.y;
				indicatorZ = owner.Map.NearestZLevel((int)indicatorXY.x, (int)indicatorXY.y, (int)indicatorZ);
			}
		}
		if(owner.SupportKeyboard && 
			 Input.GetButtonDown("Confirm")) {
			Vector3 indicatorSpot = new Vector3(indicatorXY.x, indicatorXY.y, indicatorZ);
			PathNode pn = owner.Overlay.PositionAt(indicatorSpot);
			if(pn != null && pn.canStop) {
				if(AwaitingConfirmation || !owner.RequireConfirmation) {
					owner.Pick(this, pn);
  	    	AwaitingConfirmation = false;
					UpdateSelection();
				} else if(owner.RequireConfirmation) {
					owner.TentativePick(this, indicatorSpot);
					AwaitingConfirmation = true;
				}
			}
		}
		if(owner.SupportKeyboard && Input.GetButtonDown("Cancel")) {
			if(AwaitingConfirmation && owner.RequireConfirmation) {
				if(owner.Executor != null) {
					owner.TentativePick(this, owner.Map.InverseTransformPointWorld(owner.Executor.position));
				}
				AwaitingConfirmation = false;
				UpdateSelection();
			} else {
				owner.CancelPick(this);
			}
		}
		if(cycleIndicatorZ && (!AwaitingConfirmation || !owner.RequireConfirmation)) {
			indicatorCycleT += Time.deltaTime;
			if(indicatorCycleT >= owner.IndicatorCycleLength) {
				indicatorCycleT -= owner.IndicatorCycleLength;
				indicatorZ = owner.Map.NextZLevel((int)indicatorXY.x, (int)indicatorXY.y, (int)indicatorZ, true);
			}
		}
		if(!AwaitingConfirmation) {
			Vector3 newIndicator = new Vector3((int)indicatorXY.x, (int)indicatorXY.y, (int)indicatorZ);
			if(newIndicator != lastIndicator) {
				UpdateSelection();
			}
			lastIndicator = newIndicator;
		}
	}
	void UpdateSelection() {
		if(owner.Overlay != null) {
			MapTile t = owner.Map.TileAt((int)indicatorXY.x, (int)indicatorXY.y, (int)indicatorZ);
			if(t != null) {
				owner.Overlay.SetSelectedPoints(new Vector4[]{new Vector4((int)indicatorXY.x, (int)indicatorXY.y, t.z, t.maxZ)});
			}
		}	
	}
	public void FocusOnPoint(Vector3 pos) {
		cycleIndicatorZ = false;
		indicatorXY = new Vector2(Mathf.Floor(pos.x), Mathf.Floor(pos.y));
		indicatorZ = owner.Map.NearestZLevel((int)indicatorXY.x, (int)indicatorXY.y, (int)Mathf.Floor(pos.z));
		UpdateSelection();
	}
}