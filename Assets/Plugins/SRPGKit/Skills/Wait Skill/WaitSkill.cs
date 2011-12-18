using UnityEngine;
using System.Collections;

[System.Serializable]
public class WaitSkill : ActionSkill {
	float firstClickTime = -1;
	float doubleClickThreshold = 0.3f;

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
	
	public GameObject waitArrows;
	
	public override void Reset() {
		base.Reset();
		skillName = "Wait";
		skillSorting = 100000;
		if(waitArrows == null) {
			waitArrows = Resources.LoadAssetAtPath("Assets/SRPGKit/Prefabs/Wait Arrows.prefab", typeof(GameObject)) as GameObject;
		}
		targetingMode = TargetingMode.Cardinal;
		SetParam("range.xy.min", 0);
		SetParam("range.xy.max", 0);
		SetParam("range.z.up.min", 0);
		SetParam("range.z.up.max", 0);
		SetParam("range.z.down.min", 0);
		SetParam("range.z.down.max", 0);
		SetParam("radius.xy", 0);
		SetParam("radius.z.up", 0);
		SetParam("radius.z.down", 0);
		StatEffect facingEffect = new StatEffect();
		facingEffect.effectType = StatEffectType.ChangeFacing;
		facingEffect.target = StatEffectTarget.Applier;
		facingEffect.value = Formula.Lookup("arg.angle.xy");
		StatEffect endTurnEffect = new StatEffect();
		endTurnEffect.effectType = StatEffectType.EndTurn;
		endTurnEffect.target = StatEffectTarget.Applier;
		targetEffects = new StatEffectGroup[]{
			new StatEffectGroup{effects=new StatEffect[]{
				facingEffect, endTurnEffect
			}}
		};
	}
	
	public override void ActivateSkill() {
		if(isActive) { return; }
		base.ActivateSkill();
		instantiatedWaitArrows = (Object.Instantiate(waitArrows) as GameObject).transform;
		instantiatedWaitArrows.parent = map.transform;
		instantiatedWaitArrows.position = character.transform.position;
		MoveToLayer(instantiatedWaitArrows, LayerMask.NameToLayer("Wait Arrows"));
		WaitAtArrow(ArrowForFacing(character.Facing));
	}	
	public override void DeactivateSkill() {
		if(!isActive) { return; }
		instantiatedWaitArrows.Find("XP").localScale = new Vector3(1,1,1);
		instantiatedWaitArrows.Find("YP").localScale = new Vector3(1,1,1);
		instantiatedWaitArrows.Find("XN").localScale = new Vector3(1,1,1);
		instantiatedWaitArrows.Find("YN").localScale = new Vector3(1,1,1);
		Object.Destroy(instantiatedWaitArrows.gameObject);
		instantiatedWaitArrows = null;
		base.DeactivateSkill();
	}
	public override void Update() {
		base.Update();
		if(!isActive) { return; }
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
							ApplySkill();
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
		WaitAtArrow(ArrowForFacing(character.Facing));
	}
	public Arrow ArrowForFacing(Quaternion q) {
		const float TAU = 360;
		float mapY = map.transform.eulerAngles.y;
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
		currentArrow = ArrowForFacing(character.Facing);
		currentArrowTransform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
		Quaternion dir;
		switch(a) {
			case Arrow.XP: dir = Quaternion.Euler(0, 0*TAU/4, 0); break;
			case Arrow.YP: dir = Quaternion.Euler(0, 1*TAU/4, 0); break;
			case Arrow.XN: dir = Quaternion.Euler(0, 2*TAU/4, 0); break;
			case Arrow.YN: dir = Quaternion.Euler(0, 3*TAU/4, 0); break;
			default: Debug.LogError("Not my arrow!"); return;
		}
		FaceDirection(dir);
	}
	
	void MoveToLayer(Transform root, int layer) {
	  root.gameObject.layer = layer;
	  foreach(Transform child in root) {
	    MoveToLayer(child, layer);
		}
	}
	
	
}
