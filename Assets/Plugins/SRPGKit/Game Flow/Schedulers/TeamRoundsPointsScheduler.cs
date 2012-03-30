using UnityEngine;
using System.Collections;

public enum TurnLimitMode {
	AP,
	Time
}

[AddComponentMenu("SRPGCK/Arbiter/Scheduler/Team Rounds with Points")]
public class TeamRoundsPointsScheduler : Scheduler {
	public TurnLimitMode limitMode=TurnLimitMode.AP;

	public bool onlyDrainAPForXYDistance = true;

	public float defaultLimiterMax=10;
	public float defaultLimiterDiminishScale=0.75f;
	public float defaultMoveAPCost=1; //per tile move cost
	public float defaultAPLossPerSecond=0;

	public int teamCount=2;

	public int currentTeam=0;

	public int pointsPerRound=8;

	public int pointsRemaining=0;

	override public void Start () {
		pointsRemaining = pointsPerRound;
		foreach(Character c in characters) {
			RoundPointsCharacter ppc = c.GetComponent<RoundPointsCharacter>();
			ppc.UsesThisRound = 0;
			ppc.Limiter = 0;
		}
	}

	public void EndRound() {
		if(activeCharacter != null) {
			Deactivate(activeCharacter);
		}
		map.BroadcastMessage("RoundEnded", currentTeam, SendMessageOptions.DontRequireReceiver);
		currentTeam++;
		if(currentTeam >= teamCount) {
			currentTeam = 0;
		}
		pointsRemaining = pointsPerRound;
		foreach(Character c in characters) {
			if(c.EffectiveTeamID == currentTeam) {
				RoundPointsCharacter ppc = c.GetComponent<RoundPointsCharacter>();
				ppc.UsesThisRound = 0;
			}
		}
		map.BroadcastMessage("RoundBegan", currentTeam, SendMessageOptions.DontRequireReceiver);
	}

	public void DecreaseAP(float amt) {
		if(activeCharacter != null && limitMode == TurnLimitMode.AP) {
			RoundPointsCharacter ppc = activeCharacter.GetComponent<RoundPointsCharacter>();
			ppc.Limiter -= amt;
		}
	}

	override public void AddCharacter(Character c) {
		base.AddCharacter(c);
		if(c.GetComponent<RoundPointsCharacter>() == null) {
			c.gameObject.AddComponent<RoundPointsCharacter>();
		}
	}

	override public void Activate(Character c, object ctx=null) {
		if(c == null) { return; }
		RoundPointsCharacter ppc = c.GetComponent<RoundPointsCharacter>();
		int uses = ppc.UsesThisRound;
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
			//FIXME: It is NOT OKAY for the scheduler to determine the move region's max range?
			//???: What about characters' intrinsic stats and so on?
			//???: may be better to pass this through a param instead...
			// Region ms = c.moveSkill.targetSettings[0].targetRegion;
			// ms.radiusMaxF = Formula.Constant(GetMaximumTraversalDistance(c));
		}
	 	//FIXME: can we do something here for time-based traversal distance limitation?
		//???: What about characters' intrinsic movement stats and so on?
		Debug.Log("starting AP: "+ppc.Limiter);
		base.Activate(c, ctx);
		ppc.UsesThisRound = uses+1;
		//(for now): ON `activate`, MOVE
	//	activeCharacter.moveSkill.ActivateSkill();
		pointsRemaining--;
	}

	override public void CharacterMovedIncremental(Character c, Vector3 from, Vector3 to, PathNode endOfPath) {
		//FIXME: might be broken for certain types of incremental tile-locked moves
/*		Debug.Log("moved to "+to);*/
		RoundPointsCharacter ppc = c.GetComponent<RoundPointsCharacter>();
		if(limitMode == TurnLimitMode.AP) {
			float moveAPCost = ppc.PerUnitMovementAPCost;
			float distance = 0;
			if(onlyDrainAPForXYDistance) {
				distance = Vector2.Distance(new Vector2(to.x, to.y), new Vector2(from.x, from.y));
			} else {
				distance = Vector3.Distance(to, from);
			}
			Debug.Log("Inc distance: "+distance+" from "+from+" to "+to);
			DecreaseAP(moveAPCost * distance);
			Debug.Log("new AP: "+ppc.Limiter);
		}
	}

	override public void CharacterMoved(Character c, Vector3 from, Vector3 to, PathNode endOfPath) {
/*		Debug.Log("moved to "+to);*/
		RoundPointsCharacter ppc = c.GetComponent<RoundPointsCharacter>();
		if(limitMode == TurnLimitMode.AP) {
			float moveAPCost = ppc.PerUnitMovementAPCost;
			float distance = 0;
			if(onlyDrainAPForXYDistance) {
				distance = endOfPath.xyDistanceFromStart;
			} else {
				distance = Mathf.Floor(endOfPath.distance);
			}
			Debug.Log("Distance: "+distance+" from "+from+" to "+to);
			DecreaseAP(moveAPCost * distance);
			Debug.Log("new AP: "+ppc.Limiter);
		}
	}

	//NEXT: maybe this is best phrased as a call from the scheduler to the Region letting it know
	//some relevant information? Or else a call from the Region to the scheduler -- either way,
	//the Region must be AP-savvy in order to get the correct paths.
	public float GetMaximumTraversalDistance(Character c=null) {
		//FIXME: only right for AP-limited scheduler
		if(c == null) { c = activeCharacter; }
		if(c == null) { return 0; }
		RoundPointsCharacter ppc = c.GetComponent<RoundPointsCharacter>();
		float moveAPCost = ppc.PerUnitMovementAPCost;
		return (float)(ppc.Limiter/moveAPCost);
	}

	override public void SkillApplied(SkillDef s) {
		base.SkillApplied(s);
	}
	public void WillSpecialMoveCharacter(CharacterSpecialMoveReport csmr) {
		Pause();
	}
	public void DidSpecialMoveCharacter(CharacterSpecialMoveReport csmr) {
		Resume();
	}

	override public void FixedUpdate() {
		base.FixedUpdate();
		if(paused) { return; }
		if(activeCharacter == null) {
			if(pointsRemaining == 0) {
				EndRound();
			}
		}
		if(activeCharacter != null) {
			RoundPointsCharacter ppc = activeCharacter.GetComponent<RoundPointsCharacter>();
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
	}
	public void Update() {
		if(paused) { return; }
		if(GetComponent<Arbiter>().IsLocalTeam(currentTeam) && activeCharacter == null && Input.GetMouseButtonDown(0)) {
			//TODO: need another caller for Activate()
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
			if(closestCharacter != null && !closestCharacter.isActive && closestCharacter.EffectiveTeamID == currentTeam) {
				Activate(closestCharacter);
			}
		}
	}
}
