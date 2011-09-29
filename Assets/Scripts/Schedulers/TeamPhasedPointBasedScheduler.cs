using UnityEngine;
using System.Collections;

public enum TurnLimitMode {
	AP,
	Time
}

public class TeamPhasedPointBasedScheduler : Scheduler {
	public TurnLimitMode limitMode=TurnLimitMode.Time;
	
	public double defaultLimiterMax=10;
	public double defaultLimiterDiminishScale=0.75;
	public double defaultMoveAPCost=1; //per tile moved
	public double defaultAPLossPerSecond=0;
	
	public int teamCount=2;

	public int currentTeam=0;
	
	public int pointsPerPhase=8;

	[HideInInspector]
	public int pointsRemaining=0;
	
	[HideInInspector]
	public Hashtable characterUses;
	
	[HideInInspector]
	public double limiter;
	
	override public void Start () {
		characterUses = new Hashtable();
		pointsRemaining = pointsPerPhase;
		limiter = 0;
		foreach(Character c in characters) {
			if(c.GetEffectiveTeamID() == currentTeam) {
				characterUses[c] = 0;
			}
			c.SendMessage("BeginTurn", currentTeam, SendMessageOptions.DontRequireReceiver);
		}
	}
	
	public void EndPhase() {
		if(activeCharacter != null) {
			Deactivate(activeCharacter);
		}
		foreach(Character c in characters) {
			c.SendMessage("EndPhase", currentTeam, SendMessageOptions.DontRequireReceiver);
		}
		currentTeam++;
		if(currentTeam >= teamCount) {
			currentTeam = 0;
		}
		characterUses.Clear();
		pointsRemaining = pointsPerPhase;
		foreach(Character c in characters) {
			if(c.GetEffectiveTeamID() == currentTeam) {
				characterUses[c] = 0;
			}
			c.SendMessage("BeginPhase", currentTeam, SendMessageOptions.DontRequireReceiver);
		}
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
				Deactivate(activeCharacter);
			}
		}
		GUILayout.EndArea();
	}
	
	public void DecreaseAP(double amt) {
		if(activeCharacter != null && limitMode == TurnLimitMode.AP) {
			limiter -= amt;
		}
	}
	
	override public void Activate(Character c, object ctx=null) {
		if(c == null) { return; }
		int uses = characterUses.ContainsKey(c) ? (int)characterUses[c] : 0;
		if(limitMode == TurnLimitMode.AP) {
			//set C's AP based on uses
			limiter = c.HasCustomData("SchedulerTurnAPMax") ? c.GetCustomData<double>("SchedulerTurnAPMax") : defaultLimiterMax;
		} else if(limitMode == TurnLimitMode.Time) {
			//set timer based on uses
			limiter = c.HasCustomData("SchedulerTurnTimeMax") ? c.GetCustomData<double>("SchedulerTurnTimeMax") : defaultLimiterMax;
		}
		double downscaleFactor = c.HasCustomData("SchedulerTurnDiminishScale") ? c.GetCustomData<double>("SchedulerTurnDiminishScale") : defaultLimiterDiminishScale;
		limiter *= Mathf.Pow((float)downscaleFactor, uses);
		Debug.Log("starting AP: "+limiter);
		base.Activate(c, ctx);
		characterUses[c] = uses+1;
	}
	
	override public void CharacterMoved(Character c, Vector3 from, Vector3 to) {
		//FIXME: should get a path node instead of an instantaneous vector, since we don't really want straight-line distance
		if(limitMode == TurnLimitMode.AP) {
			double moveAPCost = c.HasCustomData("SchedulerTurnMoveAPCost") ? c.GetCustomData<double>("SchedulerTurnMoveAPCost") : defaultMoveAPCost;
			Debug.Log("Distance: "+Vector3.Distance(to, from));
			DecreaseAP(moveAPCost * Vector3.Distance(to, from));
			Debug.Log("new AP: "+limiter);
		}
	}
	
	override public float GetMaximumTraversalDistance() {
		double moveAPCost = activeCharacter.HasCustomData("SchedulerTurnMoveAPCost") ? activeCharacter.GetCustomData<double>("SchedulerTurnMoveAPCost") : defaultMoveAPCost;
		return (float)(moveAPCost * limiter);
	}
	
	override public void EndMovePhase(Character c) {
//		Deactivate(c);
	}
	
	override public void Update () {
		base.Update();
		if(activeCharacter == null) {
			if(pointsRemaining == 0) {
				EndPhase();
			}
		}
		if(activeCharacter) {
			if(limitMode == TurnLimitMode.Time) {
				limiter -= Time.deltaTime;
			} else if(limitMode == TurnLimitMode.AP) {
				double waitAPCost = activeCharacter.HasCustomData("SchedulerTurnAPLossPerSecond") ? activeCharacter.GetCustomData<double>("SchedulerTurnAPLossPerSecond") : defaultAPLossPerSecond;
				DecreaseAP(waitAPCost * Time.deltaTime);
			}
			if(limiter <= 0) {
				Deactivate(activeCharacter);
			}
		}
		if(Input.GetMouseButtonDown(0)) {
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
