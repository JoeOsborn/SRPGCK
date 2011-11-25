using UnityEngine;
using System.Collections;

[System.Serializable]
public class ContinuousMoveIO : MoveIO {
	public bool supportKeyboard = true;
	public bool supportMouse = true;
	
	CharacterController cc;
	
	public RadialOverlayType overlayType = RadialOverlayType.Sphere;

	RadialOverlay overlay;
	
	float firstClickTime = -1;
	[SerializeField]
	float doubleClickThreshold = 0.3f;
	
	public float moveSpeed=12.0f;
	
	public Color overlayColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
	public bool drawOverlayRim = false;
	public bool drawOverlayVolume = false;
	public bool invertOverlay = false; //draw an overlay on the map's exterior

	Vector3 moveDest=Vector3.zero;
	
	override public void Activate() {
		base.Activate();
		cc = owner.character.GetComponent<CharacterController>();
		//HACK: 0.09f here is a hack for the charactercontroller collider rather than 5.0
		owner.character.transform.localPosition = new Vector3(
			owner.character.transform.localPosition.x, 
			owner.character.transform.localPosition.y+0.09f, 
			owner.character.transform.localPosition.z
		);
	}
	
	override public void Update () {
		base.Update();
		if(owner.character == null || !owner.character.isActive) { return; }
		if(!isActive) { return; }
		if(!owner.arbiter.IsLocalPlayer(owner.character.EffectiveTeamID)) {
			return;
		}
		if(supportMouse && Input.GetMouseButton(0)) {
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			Vector3 hitSpot;
			bool inside = overlay.Raycast(r, out hitSpot);
			//TODO: move with interpolation
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
			
		}
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");
		if(supportKeyboard && (h != 0 || v != 0)) {
			Transform cameraTransform = Camera.main.transform;
			Vector3 forward = cameraTransform.TransformDirection(Vector3.forward);
			forward.y = 0;
			forward = forward.normalized;
			Vector3 right = new Vector3(forward.z, 0, -forward.x);
			Vector3 offset = h * right + v * forward;

			cc.SimpleMove(offset*moveSpeed);
			//another approach (instead of ContainsPosition): place invisible box colliders at the edges of tiles which shouldn't be crossed
			//HACK: 5.09f here is a hack for the charactercollider rather than 5.0
			Vector3 newDest = owner.map.InverseTransformPointWorld(owner.character.transform.position-new Vector3(0,5.09f,0));

			PathNode pn = overlay.PositionAt(newDest);
			if(pn != null && pn.canStop) {
				moveDest = newDest;
				IncrementalMove(moveDest);
			} else if(overlay.ContainsPosition(moveDest)) {
				//moveDest is still the old one
				IncrementalMove(moveDest);
			}
		}
		if(supportKeyboard && Input.GetButtonDown("Confirm")) {
			PathNode pn = overlay.PositionAt(moveDest);
			if(pn != null && pn.canStop) {
				PerformMove(moveDest);
			}
		}
	}
	
	override public void PresentMoves() {
		base.PresentMoves();
		//TODO: convert this or create a new moveIO that's really trivial and doesn't do any movement on its own, instead watching for movements in its transform and using IncrementalMove appropriately. may also need to be paired with a custom trivial MoveExecutor.
		/*
		[SerializeField]
		Vector3 lastPosition;
		
		lastPosition = transform.position;
		*/
		
		RadialMoveStrategy ms = owner.strategy as RadialMoveStrategy;
		Vector3 charPos = owner.map.InverseTransformPointWorld(owner.character.transform.position);
		Debug.Log("show at "+charPos);
		if(overlayType == RadialOverlayType.Sphere) {
			overlay = owner.map.PresentSphereOverlay(
						"move", owner.character.gameObject.GetInstanceID(), 
						overlayColor,
						charPos,
						ms.GetMoveRadius(),
						drawOverlayRim,
						drawOverlayVolume,
						invertOverlay
					);
		} else if(overlayType == RadialOverlayType.Cylinder) {
			overlay = owner.map.PresentCylinderOverlay(
						"move", owner.character.gameObject.GetInstanceID(), 
						new Color(0.3f, 0.3f, 0.3f, 0.3f),
						charPos,
						ms.GetMoveRadius(),
						ms.GetJumpHeight(),
						drawOverlayRim,
						drawOverlayVolume,
						invertOverlay
					);
		}
		moveDest = charPos;
	}
	
	override protected void FinishMove() {
		overlay = null;
		if(owner.map.IsShowingOverlay("move", owner.character.gameObject.GetInstanceID())) {
			owner.map.RemoveOverlay("move", owner.character.gameObject.GetInstanceID());
		}	
		base.FinishMove();
	}
}
