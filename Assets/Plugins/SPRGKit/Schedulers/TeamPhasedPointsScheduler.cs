using UnityEngine;
using System.Collections;

public enum TurnLimitMode {
	AP,
	Time
}

public class TeamPhasedPointsScheduler : Scheduler {
	public TurnLimitMode limitMode=TurnLimitMode.Time;
	
	public bool onlyDrainAPForXYDistance = true;
	
	public float defaultLimiterMax=10;
	public float defaultLimiterDiminishScale=0.75f;
	public float defaultMoveAPCost=1; //per tile move cost
	public float defaultAPLossPerSecond=0;
		
	public int teamCount=2;

	public int currentTeam=0;
	
	public int pointsPerPhase=8;

	[HideInInspector]
	public int pointsRemaining=0;
	
	override public void Start () {
		pointsRemaining = pointsPerPhase;
		foreach(Character c in characters) {
			PhasedPointsCharacter ppc = c.GetComponent<PhasedPointsCharacter>();
			ppc.UsesThisPhase = 0;
			ppc.Limiter = 0;
		}
	}
	
	public void EndPhase() {
		if(activeCharacter != null) {
			Deactivate(activeCharacter);
		}
		map.BroadcastMessage("PhaseEnded", currentTeam, SendMessageOptions.DontRequireReceiver);
		currentTeam++;
		if(currentTeam >= teamCount) {
			currentTeam = 0;
		}
		pointsRemaining = pointsPerPhase;
		foreach(Character c in characters) {
			if(c.GetEffectiveTeamID() == currentTeam) {
				PhasedPointsCharacter ppc = c.GetComponent<PhasedPointsCharacter>();
				ppc.UsesThisPhase = 0;
			}
		}
		map.BroadcastMessage("PhaseBegan", currentTeam, SendMessageOptions.DontRequireReceiver);
	}
	
	public void OnGUI() {
		GUILayout.BeginArea(new Rect(
			8, 8, 
			96, 128
		));
		GUILayout.Label("Current Team: "+currentTeam);
		GUILayout.Label("Points Left: "+pointsRemaining);
		if(GUILayout.Button("End Phase")) {
			EndPhase();
		}
		if(activeCharacter != null) {
			if(GUILayout.Button("End Move")) {
				EndMovePhase(activeCharacter);
			}
		}
		GUILayout.EndArea();
	}
	
	public void DecreaseAP(float amt) {
		if(activeCharacter != null && limitMode == TurnLimitMode.AP) {
			PhasedPointsCharacter ppc = activeCharacter.GetComponent<PhasedPointsCharacter>();
			ppc.Limiter -= amt;
		}
	}
	
	override public void AddCharacter(Character c) {
		base.AddCharacter(c);
		if(c.GetComponent<PhasedPointsCharacter>() == null) {
			c.gameObject.AddComponent<PhasedPointsCharacter>();
		}
	}
	
	override public void Activate(Character c, object ctx=null) {
		if(c == null) { return; }
		PhasedPointsCharacter ppc = c.GetComponent<PhasedPointsCharacter>();
		int uses = ppc.UsesThisPhase;
		if(limitMode == TurnLimitMode.AP) {
			//set C's AP based on uses
			ppc.Limiter = ppc.MaxTurnAP;
		} else if(limitMode == TurnLimitMode.Time) {
			//set timer based on uses
			ppc.Limiter = ppc.MaxTurnTime;
		}
		float downscaleFactor = ppc.TurnDiminishScale;
		ppc.Limiter *= Mathf.Pow(downscaleFactor, uses);
		if(limitMode == TurnLimitMode.AP) {
			//???: Is it okay for the scheduler to determine the move strategy's max range?
			//???: What about characters' intrinsic stats and so on?
			MoveStrategy ms = c.GetComponent<MoveStrategy>();
			ms.xyRange = GetMaximumTraversalDistance(c);
		}
	 	//FIXME: can we do something here for time-based traversal distance limitation?
		//???: What about characters' intrinsic movement stats and so on?
		Debug.Log("starting AP: "+ppc.Limiter);
		base.Activate(c, ctx);
		ppc.UsesThisPhase = uses+1;
		//(for now): ON `activate`, MOVE
		activeCharacter.SendMessage("PresentMoves", null);
	}
	
	override public void CharacterMovedIncremental(Character c, Vector3 from, Vector3 to) {
		CharacterMoved(c, from, to);
	}
	
	override public void CharacterMoved(Character c, Vector3 from, Vector3 to) {
		//FIXME: should get path nodes instead of an instantaneous vector, since we don't really want straight-line distance
		Debug.Log("moved to "+to);
		PhasedPointsCharacter ppc = c.GetComponent<PhasedPointsCharacter>();
		if(limitMode == TurnLimitMode.AP) {
			float moveAPCost = ppc.PerUnitMovementAPCost;
			float distance = 0;
			if(onlyDrainAPForXYDistance) {
				distance = Vector2.Distance(new Vector2(to.x, to.y), new Vector2(from.x, from.y));
			} else {
				distance = Vector3.Distance(to, from);
			}
			Debug.Log("Distance: "+distance);
			DecreaseAP(moveAPCost * distance);
			Debug.Log("new AP: "+ppc.Limiter);
		}
	}
	
	//NEXT: maybe this is best phrased as a call from the scheduler to the MoveStrategy letting it know
	//some relevant information? Or else a call from the MoveStrategy to the scheduler -- either way,
	//the MoveStrategy must be AP-savvy in order to get the correct paths.
	public float GetMaximumTraversalDistance(Character c=null) {
		//FIXME: only right for AP-limited scheduler
		if(c == null) { c = activeCharacter; }
		if(c == null) { return 0; }
		PhasedPointsCharacter ppc = c.GetComponent<PhasedPointsCharacter>();
		float moveAPCost = ppc.PerUnitMovementAPCost;
		return (float)(ppc.Limiter/moveAPCost);
	}
	
	override public void EndMovePhase(Character c) {
		base.EndMovePhase(c);
		Deactivate(c);
	}
	
	override public void Update () {
		base.Update();
		if(activeCharacter == null) {
			if(pointsRemaining == 0) {
				EndPhase();
			}
		}
		if(activeCharacter) {
			PhasedPointsCharacter ppc = activeCharacter.GetComponent<PhasedPointsCharacter>();
			if(limitMode == TurnLimitMode.Time) {
				ppc.Limiter -= Time.deltaTime;
			} else if(limitMode == TurnLimitMode.AP) {
				float waitAPCost = ppc.PerSecondAPCost;
				DecreaseAP(waitAPCost * Time.deltaTime);
			}
			if(ppc.Limiter <= 0) {
				Deactivate(activeCharacter);
			}
		}
		if(activeCharacter == null && Input.GetMouseButtonDown(0)) {
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit[] hits = Physics.RaycastAll(r);
			float closestDistance = Mathf.Infinity;
			Character closestCharacter = null;
			foreach(RaycastHit h in hits) {
				Character thisChar = h.transform.GetComponent<Character>();
				if(thisChar!=null) {
					if(h.distance < closestDistance) {
						closestDistance = h.distance;
						closestCharacter = h.transform.GetComponent<Character>();
					}
				}
			}
			if(closestCharacter != null && !closestCharacter.isActive && closestCharacter.GetEffectiveTeamID() == currentTeam) {
				Activate(closestCharacter);
				pointsRemaining--;
			}
		}
	}
}
