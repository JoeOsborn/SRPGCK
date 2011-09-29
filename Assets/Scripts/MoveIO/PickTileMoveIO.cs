using UnityEngine;
using System.Collections.Generic;

public class PickTileMoveIO : MoveIO {
	public bool supportKeyboard = true;
	public bool supportMouse = true;
	
	public Transform indicator;

	Overlay overlay;
	
	float firstClickTime = -1;
	float doubleClickThreshold = 0.3f;
	
	bool cycleIndicatorZ = false;
	float indicatorCycleT=0;
	float indicatorCycleLength=1.0f;
	
	float lastIndicatorKeyboardMove=0;
	float indicatorKeyboardMoveThreshold=0.3f;
	
	Vector2 indicatorXY=Vector2.zero;
	float indicatorZ=0;
	
	Transform instantiatedIndicator;
	
	
	override public void Start() {
		base.Start();
		instantiatedIndicator = Instantiate(indicator) as Transform;
		instantiatedIndicator.gameObject.active = false;
	}
	
	override public void Update () {
		base.Update();
		if(character == null || !character.isActive) { return; }
		//self.isActive?
		if(supportMouse && Input.GetMouseButtonDown(0)) {
			cycleIndicatorZ = false;
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			Vector3 hitSpot;
			bool inside = overlay.Raycast(r, out hitSpot);
			hitSpot.x = Mathf.Floor(hitSpot.x)+0.5f;
			hitSpot.y = Mathf.Floor(hitSpot.y)+0.5f;
			hitSpot.z = Mathf.Floor(hitSpot.z+0.5f);
			if(Time.time-firstClickTime > doubleClickThreshold) {
				indicatorXY = new Vector2(hitSpot.x, hitSpot.y);
				indicatorZ = hitSpot.z;
				firstClickTime = Time.time;
			} else {
				firstClickTime = -1;
				if(inside) {
					Vector3 indicatorSpot = new Vector3(indicatorXY.x, indicatorXY.y, indicatorZ);
					PathNode pn = overlay.PositionAt(indicatorSpot);
					if(pn != null && pn.canStop) {
						PerformMove(indicatorSpot);
						map.scheduler.EndMovePhase(character);
					}
				}
			}
		}
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");
		if(supportKeyboard && (h != 0 || v != 0) && Time.time-lastIndicatorKeyboardMove > indicatorKeyboardMoveThreshold) {
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
		if(supportKeyboard && Input.GetButtonDown("Confirm")) {
			Vector3 indicatorSpot = new Vector3(indicatorXY.x, indicatorXY.y, indicatorZ);
			PathNode pn = overlay.PositionAt(indicatorSpot);
			if(pn != null && pn.canStop) {
				PerformMove(indicatorSpot);
				map.scheduler.EndMovePhase(character);
			}
		}
		//FIXME: fix my cycling
		if(cycleIndicatorZ) {
			indicatorCycleT += Time.deltaTime;
			if(indicatorCycleT >= indicatorCycleLength) {
				indicatorCycleT -= indicatorCycleLength;
				indicatorZ = map.NextZLevel((int)indicatorXY.x, (int)indicatorXY.y, (int)indicatorZ, true);
			}
		}
		
		instantiatedIndicator.position = map.TransformPointWorld(new Vector3(indicatorXY.x, indicatorXY.y, indicatorZ))+new Vector3(0,0.1f,0);
	}
	
	override public void PresentMoves() {
		MoveStrategy ms = GetComponent<MoveStrategy>();
		PathNode[] destinations = ms.GetValidMoves();
		overlay = map.PresentOverlay(
			"move", this.gameObject.GetInstanceID(), 
			new Color(0.2f, 0.3f, 0.9f, 0.7f),
			destinations
		);
		Vector3 charPos = map.InverseTransformPointWorld(transform.position);
		cycleIndicatorZ = false;
		indicatorXY = new Vector2(Mathf.Floor(charPos.x)+0.5f, Mathf.Floor(charPos.y)+0.5f);
		indicatorZ = Mathf.Floor(charPos.z);
		instantiatedIndicator.gameObject.active = true;
		instantiatedIndicator.parent = map.transform;
	}
	
	override public void Deactivate() {
		instantiatedIndicator.gameObject.active = false;
		overlay = null;
		if(map.IsShowingOverlay("move", this.gameObject.GetInstanceID())) {
			map.RemoveOverlay("move", this.gameObject.GetInstanceID());
		}	
	}
}
