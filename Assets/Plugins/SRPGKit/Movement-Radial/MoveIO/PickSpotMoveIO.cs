using UnityEngine;
using System.Collections;

public class PickSpotMoveIO : MoveIO {
	public bool supportKeyboard = true;
	public bool supportMouse = true;
	
	public Transform indicator;
	
	public RadialOverlayType overlayType = RadialOverlayType.Sphere;

	RadialOverlay overlay;
	
	float firstClickTime = -1;
	[SerializeField]
	float doubleClickThreshold = 0.3f;
	
	[SerializeField]
	bool cycleIndicatorZ = false;
	[SerializeField]
	float indicatorCycleT=0;
	[SerializeField]
	float indicatorCycleLength=1.0f;
	
	public float indicatorMoveSpeed=8.0f;
	
	public Color overlayColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
	public bool drawOverlayRim = false;
	public bool drawOverlayVolume = false;
	public bool invertOverlay = false; //draw an overlay on the map's exterior
		
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
		if(!isActive) { return; }
		if(!map.arbiter.IsLocalPlayer(character.EffectiveTeamID)) {
			return;
		}
		//self.isActive?
		if(supportMouse && Input.GetMouseButton(0)) {
			cycleIndicatorZ = false;
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			Vector3 hitSpot;
			bool inside = overlay.Raycast(r, out hitSpot);
			if(inside) {
				indicatorXY = new Vector2(hitSpot.x, hitSpot.y);
				indicatorZ = hitSpot.z;
			}
			if(Input.GetMouseButtonDown(0)) {
				if(Time.time-firstClickTime > doubleClickThreshold) {
					firstClickTime = Time.time;
				} else {
					firstClickTime = -1;
					if(inside) {
						Vector3 indicatorSpot = new Vector3(indicatorXY.x, indicatorXY.y, indicatorZ);
						PathNode pn = overlay.PositionAt(indicatorSpot);
						if(pn != null && pn.canStop) {
							PerformMove(indicatorSpot);
						}
					}
				}
			}
		}
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");
		if(supportKeyboard && (h != 0 || v != 0)) {
			cycleIndicatorZ = true;
			indicatorCycleT = 0;
			
			float dx = 0;
			if(h == 1) {
				dx = Time.deltaTime*indicatorMoveSpeed;
			} else if(h == -1) {
				dx = -Time.deltaTime*indicatorMoveSpeed;
			}
			float dy = 0;
			if(v == 1) {
				dy = Time.deltaTime*indicatorMoveSpeed;
			} else if(v == -1) {
				dy = -Time.deltaTime*indicatorMoveSpeed;
			}
			
			Transform cameraTransform = Camera.main.transform;
			Vector3 forward = cameraTransform.TransformDirection(Vector3.forward);
			forward.y = 0;
			forward = forward.normalized;
			Vector3 right = new Vector3(forward.z, 0, -forward.x);
			Vector3 offset = dx * right + dy * forward;

			if(indicatorXY.x+dx >= 0 && indicatorXY.y+dy >= 0 &&
				 map.HasTileAt((int)(indicatorXY.x+dx), (int)(indicatorXY.y+dy))) {
				indicatorXY.x += offset.x;
				indicatorXY.y += offset.z;
				indicatorZ = map.NearestZLevel((int)indicatorXY.x, (int)indicatorXY.y, (int)indicatorZ);
			}
		}
		if(supportKeyboard && Input.GetButtonDown("Confirm")) {
			Vector3 indicatorSpot = new Vector3(indicatorXY.x, indicatorXY.y, indicatorZ);
			PathNode pn = overlay.PositionAt(indicatorSpot);
			if(pn != null && pn.canStop) {
				PerformMove(indicatorSpot);
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
	
	override protected void PresentMoves() {
		base.PresentMoves();
		Debug.Log("Present");
		RadialMoveStrategy ms = GetComponent<RadialMoveStrategy>();
		Vector3 charPos = map.InverseTransformPointWorld(transform.position);
		Debug.Log("show at "+charPos);
		//TODO: base radius in part on points-scheduler available points
		if(overlayType == RadialOverlayType.Sphere) {
			overlay = map.PresentSphereOverlay(
						"move", this.gameObject.GetInstanceID(), 
						overlayColor,
						charPos,
						ms.GetMoveRadius(),
						drawOverlayRim,
						drawOverlayVolume,
						invertOverlay
					);
		} else if(overlayType == RadialOverlayType.Cylinder) {
			overlay = map.PresentCylinderOverlay(
						"move", this.gameObject.GetInstanceID(), 
						new Color(0.3f, 0.3f, 0.3f, 0.3f),
						charPos,
						ms.GetMoveRadius(),
						ms.GetJumpHeight(),
						drawOverlayRim,
						drawOverlayVolume,
						invertOverlay
					);
		}
		cycleIndicatorZ = false;
		indicatorXY = new Vector2(charPos.x, charPos.y);
		indicatorZ = map.NearestZLevel((int)indicatorXY.x, (int)indicatorXY.y, (int)Mathf.Floor(charPos.z));
		instantiatedIndicator.gameObject.active = true;
		instantiatedIndicator.parent = map.transform;
		instantiatedIndicator.position = map.TransformPointWorld(new Vector3(indicatorXY.x, indicatorXY.y, indicatorZ))+new Vector3(0,0.1f,0);
	}
	
	override protected void FinishMove() {
		instantiatedIndicator.gameObject.active = false;
		overlay = null;
		if(map.IsShowingOverlay("move", this.gameObject.GetInstanceID())) {
			map.RemoveOverlay("move", this.gameObject.GetInstanceID());
		}	
		base.FinishMove();
	}
}
