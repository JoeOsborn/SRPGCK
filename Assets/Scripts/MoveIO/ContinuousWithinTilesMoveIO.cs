using UnityEngine;
using System.Collections;

[RequireComponent (typeof(CharacterController))]
public class ContinuousWithinTilesMoveIO : MoveIO {
	public bool supportKeyboard = true;
	public bool supportMouse = true;

	public float moveSpeed = 12;

	Overlay overlay;
	MoveExecutor mover;
	CharacterController cc;
	
	Vector3 moveDest=Vector3.zero;
	
	override public void Start() {
		base.Start();
		mover = GetComponent<MoveExecutor>();
		cc = GetComponent<CharacterController>();
		//HACK: 0.09f here is a hack for the charactercollider rather than 5.0
		transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y+0.09f, transform.localPosition.z);
	}

	override public void Update () {
		base.Update();
		if(!character.isActive) { return; }
		//click to move within area, move the character itself around
		//keyboard to move within area, move the character itself around, prevent movement outside of overlay area
		//end with mouse down on character or return/space key
		if(supportMouse && Input.GetMouseButton(0)) {
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			Vector3 hitSpot;
			bool inside = overlay.Raycast(r, out hitSpot);
			if(inside) {
				moveDest = hitSpot;
				mover.TemporaryMoveTo(moveDest);
			}
		}
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");
		if(supportKeyboard && (h != 0 || v != 0)) {
			//move in terms of world coords on player
			Transform cameraTransform = Camera.main.transform;
			Vector3 forward = cameraTransform.TransformDirection(Vector3.forward);
			forward.y = 0;
			forward = forward.normalized;
			Vector3 right = new Vector3(forward.z, 0, -forward.x);
			Vector3 targetDirection = h * right + v * forward;
			cc.SimpleMove(targetDirection*moveSpeed);
			//another approach (instead of ContainsPosition): place invisible box colliders at the edges of tiles which shouldn't be crossed
			//HACK: 5.09f here is a hack for the charactercollider rather than 5.0
			Vector3 newDest = map.InverseTransformPointWorld(character.transform.position-new Vector3(0,5.09f,0));
/*			Debug.Log("Dest: " + newDest + " Inside? " + overlay.ContainsPosition(newDest));*/
			//TODO: something with isGrounded to prevent falling off the world
			if(overlay.ContainsPosition(newDest)) {
				moveDest = newDest;
				mover.TemporaryMoveTo(moveDest);
			} else {
				//moveDest is still the old one
				mover.TemporaryMoveTo(moveDest);
			}
		}
		if(supportKeyboard && Input.GetButtonDown("Confirm")) {
			PerformMove(moveDest);
		}
	}
	
	public void OnGUI() {
		if(supportMouse && character != null && character.isActive) {
			if(GUILayout.Button("End Turn")) {
				//if the mouse clicks on this character, end turn
				PerformMove(moveDest);
			}
		}
	}
	
	override public void PresentMoves() {
		Vector3[] destinations = GetComponent<MoveStrategy>().GetValidMoves();
		Vector4[] bounds = map.CoalesceTiles(destinations);
		overlay = map.PresentOverlay(
			"move", this.gameObject.GetInstanceID(), 
			new Color(0.2f, 0.3f, 0.9f, 0.7f),
			bounds
		);
	}
		
	override public void Deactivate() {
		overlay = null;
		if(map.IsShowingOverlay("move", this.gameObject.GetInstanceID())) {
			map.RemoveOverlay("move", this.gameObject.GetInstanceID());
		}	
	}
}

