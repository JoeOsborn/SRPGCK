using UnityEngine;
using System.Collections;

[System.Serializable]
public class WaitIO {
	[HideInInspector]
	[SerializeField]
	public bool supportKeyboard = true;
	[HideInInspector]
	[SerializeField]
	public bool supportMouse = true;
	[HideInInspector]
	[SerializeField]
	protected bool requireConfirmation = true;
	[HideInInspector]
	public bool RequireConfirmation {
		get { return requireConfirmation; } 
		set { requireConfirmation = value; }
	}
	[HideInInspector]
	[SerializeField]
	protected bool awaitingConfirmation = false;
	public bool AwaitingConfirmation { 
		get { return awaitingConfirmation; } 
		set { awaitingConfirmation = value; }
	}

	float firstClickTime = -1;
	float doubleClickThreshold = 0.3f;

	[HideInInspector]
	[SerializeField]
	public GameObject waitArrows;
	[HideInInspector]
  [System.NonSerialized]
	public Transform instantiatedWaitArrows;
	public enum Arrow {
		YP,
		XP,
		YN,
		XN
	};
	[HideInInspector]
	[SerializeField]
	public Arrow currentArrow = Arrow.YP;

	[HideInInspector]
	[System.NonSerialized]
	public WaitSkill owner;
	
	public Arrow ArrowForFacing(Quaternion q) {
		const float TAU = 360;
		float mapY = owner.map.transform.eulerAngles.y;
		float localY = q.eulerAngles.y - mapY;
		while(localY >= TAU) { localY -= TAU; }
		while(localY < 0) { localY += TAU; }
		if(localY < TAU/8 || localY >= 7*TAU/8) {
			return Arrow.XP;
		} else if(localY >= TAU/8 && localY < 3*TAU/8) {
			return Arrow.YP;
		} else if(localY >= 3*TAU/8 && localY < 5*TAU/8) {
			return Arrow.XN;
		} else if(localY >= 5*TAU/8 && localY < 7*TAU/8) {
			return Arrow.YN;
		}
		Debug.LogError("No matching direction for Q");
		return Arrow.XN;
	}
	
	protected Transform currentArrowTransform { get {
		switch(currentArrow) {
			case Arrow.XP: return instantiatedWaitArrows.Find("XP");
			case Arrow.YP: return instantiatedWaitArrows.Find("YP");
			case Arrow.XN: return instantiatedWaitArrows.Find("XN");
			case Arrow.YN: return instantiatedWaitArrows.Find("YN");
		}
		return null;
	} }
	
	public void WaitAtArrow(Arrow a) {
		const float TAU = 360.0f;
		currentArrowTransform.localScale = new Vector3(1,1,1);
		currentArrow = ArrowForFacing(owner.character.Facing);
		currentArrowTransform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
		Quaternion dir;
		switch(a) {
			case Arrow.XP: dir = Quaternion.Euler(0, 0*TAU/4, 0); break;
			case Arrow.YP: dir = Quaternion.Euler(0, 1*TAU/4, 0); break;
			case Arrow.XN: dir = Quaternion.Euler(0, 2*TAU/4, 0); break;
			case Arrow.YN: dir = Quaternion.Euler(0, 3*TAU/4, 0); break;
			default: Debug.LogError("Not my arrow!"); return;
		}
		owner.WaitInDirection(dir);
	}
	
	void MoveToLayer(Transform root, int layer) {
	  root.gameObject.layer = layer;
	  foreach(Transform child in root) {
	    MoveToLayer(child, layer);
		}
	}
	
	public void Activate() {
		awaitingConfirmation = false;
		instantiatedWaitArrows = (Object.Instantiate(waitArrows) as GameObject).transform;
		instantiatedWaitArrows.parent = owner.map.transform;
		instantiatedWaitArrows.position = owner.character.transform.position;
		MoveToLayer(instantiatedWaitArrows, LayerMask.NameToLayer("Wait Arrows"));
		WaitAtArrow(ArrowForFacing(owner.character.Facing));
	}	
	
	public void Deactivate() {
		instantiatedWaitArrows.Find("XP").localScale = new Vector3(1,1,1);
		instantiatedWaitArrows.Find("YP").localScale = new Vector3(1,1,1);
		instantiatedWaitArrows.Find("XN").localScale = new Vector3(1,1,1);
		instantiatedWaitArrows.Find("YN").localScale = new Vector3(1,1,1);
		Object.Destroy(instantiatedWaitArrows.gameObject);
		instantiatedWaitArrows = null;
	}
	
	public void Update() {
		//look for clicks on our four arrows
		//look for arrow key presses
		if(supportMouse && Input.GetMouseButton(0) && (!awaitingConfirmation || !requireConfirmation)) {
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hitInfo;
			//irksome that I have to bitshift the layer here, but whatever
			bool anyHit = Physics.Raycast(r, out hitInfo, Mathf.Infinity, 1<<instantiatedWaitArrows.gameObject.layer);
			Transform hitArrow = hitInfo.transform;
			Arrow hitArrowValue = Arrow.XP;
			if(anyHit) {
				if(hitArrow == instantiatedWaitArrows.Find("XP")) {
					hitArrowValue = Arrow.XP;
				} else if(hitArrow == instantiatedWaitArrows.Find("YP")) {
					hitArrowValue = Arrow.YP;				
				} else if(hitArrow == instantiatedWaitArrows.Find("XN")) {
					hitArrowValue = Arrow.XN;
				} else if(hitArrow == instantiatedWaitArrows.Find("YN")) {
					hitArrowValue = Arrow.YN;
				} else {
					anyHit = false;
				}	
			}
			if(anyHit) {
				WaitAtArrow(hitArrowValue);
				if(Input.GetMouseButtonDown(0) && Time.time-firstClickTime < doubleClickThreshold) {
					firstClickTime = -1;
					if(anyHit) {
						if(!requireConfirmation) {
				    	WaitAtArrow(hitArrowValue);
							awaitingConfirmation = false;
							owner.FinishWaitPick();
						} else {
				    	WaitAtArrow(hitArrowValue);
							awaitingConfirmation = true;
						}
					}
				} else {
					if(Input.GetMouseButtonDown(0)) {
						firstClickTime = Time.time;
					}
				}
			}
		}
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");
		if(supportKeyboard && 
			(h != 0 || v != 0) && 
		  (!awaitingConfirmation || !requireConfirmation)) {
			Vector2 d = owner.map.TransformKeyboardAxes(h, v);
			if(d.x != 0 && d.y != 0) {
				if(Mathf.Abs(d.x) > Mathf.Abs(d.y)) { d.x = Mathf.Sign(d.x); d.y = 0; }
				else { d.x = 0; d.y = Mathf.Sign(d.y); }
			}
			if(d.x > 0) {
				WaitAtArrow(Arrow.XP);
			} else if(d.x < 0) {
				WaitAtArrow(Arrow.XN);
			} else if(d.y > 0) {
				WaitAtArrow(Arrow.YP);
			} else if(d.y < 0) {
				WaitAtArrow(Arrow.YN);
			}
		}
		if(supportKeyboard && 
			 Input.GetButtonDown("Confirm")) {
			if(awaitingConfirmation || !requireConfirmation) {
				WaitAtArrow(currentArrow);
  	  	awaitingConfirmation = false;
				owner.FinishWaitPick();
			} else if(requireConfirmation) {
				WaitAtArrow(currentArrow);
				awaitingConfirmation = true;
			}
		}
		if(supportKeyboard && Input.GetButtonDown("Cancel")) {
			if(awaitingConfirmation && requireConfirmation) {
				awaitingConfirmation = false;
			} else {
				//Back out of move phase!
				owner.CancelWaitPick();
			}
		}
	}
}
