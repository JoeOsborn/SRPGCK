using UnityEngine;
using System.Collections;

public class CTScheduler : Scheduler {
	public double defaultSpeed = 6.0;
	public double defaultMaxCT = 100;
	public double defaultPerTileCTCost = 0;
	public double defaultPerMoveCTCost = 30;
	public double defaultPerActionCTCost = 40;
	public double defaultPerActivationCTCost = 30;
	
	public bool coalesceCTDecrements = false;
	[SerializeField]
	protected double pendingCTDecrement = 0;
	
	[SerializeField]
	protected Hashtable cts;
	[SerializeField]
	protected bool hasMoved=false;
	[SerializeField]
	protected bool hasActed=false;
	
		
	override public void Start () {
		base.Start();
		if(cts == null) { cts = new Hashtable(); }
	}
	
	override public void Activate(Character c, object ctx=null) {
		base.Activate(c, ctx);
		hasMoved = false;
		hasActed = false;
		pendingCTDecrement = 0;
		//(for now): ON `activate`, MOVE
		activeCharacter.GetComponent<MoveIO>().PresentMoves();
	}
	
	override public void Deactivate(Character c, object ctx=null) {
		base.Deactivate(c, ctx);
		//reduce c's CT by base turn cost (30)
		double cost = c.GetCustomData<double>("SchedulerCTActivationCost", defaultPerActivationCTCost);
		if(coalesceCTDecrements) {
			pendingCTDecrement += cost;
			cts[c] = (double)Mathf.Max((float)((double)cts[c]-pendingCTDecrement), 0);
			pendingCTDecrement = 0;
		} else {
			cts[c] = (double)Mathf.Max((float)((double)cts[c]-cost), 0);
		}
	}
	
	override public void EndMovePhase(Character c) {
		base.EndMovePhase(c);
		//reduce c's CT by any-movement cost (30)
		double cost = c.GetCustomData<double>("SchedulerCTMovementCost", defaultPerMoveCTCost);
		if(coalesceCTDecrements) {
			pendingCTDecrement += cost;
		} else {
			cts[c] = (double)Mathf.Max((float)((double)cts[c]-cost), 0);
		}
		hasMoved = true;
	}
	
	override public void CharacterMoved(Character c, Vector3 src, Vector3 dest) {
		base.CharacterMoved(c, src, dest);
		//reduce c's CT by per-tile movement cost (0)
		double cost = c.GetCustomData<double>("SchedulerCTPerTileCost", defaultPerTileCTCost);
		if(coalesceCTDecrements) {
			pendingCTDecrement += cost;
		} else {
			cts[c] = (double)Mathf.Max((float)((double)cts[c]-cost), 0);
		}
	}
	//after c acts, reduce c's CT by per-act cost (40)
	
	public void OnGUI() {
		if(activeCharacter != null) {
			GUILayout.BeginArea(new Rect(
				8, 8, 
				128, 128
			));
			GUILayout.Label("Current Character:");
			GUILayout.Label(activeCharacter.gameObject.name);
			GUILayout.Label("CT: "+Mathf.Floor((float)((double)cts[activeCharacter])));
			if(GUILayout.Button("End Turn")) {
				Deactivate(activeCharacter);
			}
			GUILayout.EndArea();
		}
	}
	
	override public void Update () {
		base.Update();
		//if there is no active unit
		if(activeCharacter == null) {
		  //TODO: take the first scheduled attack with CT > 100 and trigger it
			//else, take the first unit with CT > 100, if any, and activate it
			foreach(Character c in characters) {
				double maxCT = c.GetCustomData<double>("SchedulerCTMax", defaultMaxCT);
				if(!cts.ContainsKey(c)) {
					cts[c] = (double)0;
				}
				if((double)(cts[c]) >= maxCT) {
					cts[c] = (double)maxCT;
					Activate(c);
					return;
				}
			}
			//TODO: else, tick up every attack by their effective speed
			//else, tick up CT on everybody by their effective speed
			foreach(Character c in characters) {
				double maxCT = c.GetCustomData<double>("SchedulerCTMax", defaultMaxCT);
				double speed = c.GetCustomData<double>("SchedulerCTSpeed", defaultSpeed);
				if(!cts.ContainsKey(c)) {
					cts[c] = (double)0;
				}
				cts[c] = (double)Mathf.Min((float)(((double)cts[c])+speed), (float)maxCT);
			}
		}
		//otherwise, do nothing
	}
}
