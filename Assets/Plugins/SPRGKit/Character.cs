using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Character : MonoBehaviour {	
	[System.NonSerialized]
	public Map map=null;

	//I believe this is a stored property here in character, not merely a query to scheduler for whether active==this (what if you can have multiple active dudes?)
	public bool isActive=false;

	public int teamID;
	
	// Use this for initialization
	void Start () {
	}
	
	//can be modulated by charm, etc
	public int EffectiveTeamID { get {
		return teamID;
	} }
	
	public void Activate() {
		isActive = true;
	}
	
	public void Deactivate() {
		isActive = false;
	}
	
	void Update () {
		if(map == null) {
			if(this.transform.parent != null) {
				map = this.transform.parent.GetComponent<Map>();
				if(map != null && map.scheduler != null) {
					map.scheduler.AddCharacter(this);
				}
			}
			if(map == null) { 
				Debug.Log("Characters must be children of Map objects!");
				return; 
			}
		}
		//Five things are going on here:
		//we're ACTIVATING/DEACTIVATING
		//we're CALCULATING WHERE WE CAN MOVE
		//we're DISPLAYING WHERE WE CAN MOVE
		//we're REQUESTING A MOVE DESTINATION
		//and, eventually, we'll be MOVING in an animated way
		//ideally, we will move as many of these as possible out of the Character script.
		//ACTIVATING/DEACTIVATING should live in a "Scheduler" GameObject and script (though deactivation requests may be sent by other scripts to the scheduler)
		//CALCULATING should live in a MoveStrategy script on the Character gameobject
		//DISPLAYING should live in a MoveFeedback script on the Character gameobject
		//REQUESTING should live in a MoveInput script on the Character gameobject
		//MOVING should live in a MoveExecutor script on the Character gameobject
		//continuous movement (e.g. via keyboard) could be a MoveInput with a trivial Executor (pos=dest) and a collision-detecting strategy and any type of MoveFeedback
		//  if it's timed continuous movement, the Scheduler might halt the move by its own prerogative.

		//MoveStrategy concerns the rules regarding "If I am character X in state Y at position Z, what tiles T[] can I reach?"
		//  It also validates the move and returns a success status, for use in the MoveExecutor (consider FFT's "Teleport", which may fail)
		//MoveIO concerns the rules and processes and state that display:
			//"Reachable Tiles" T[], provided by MoveStrategy
			//"Candidate Tiles" Tc[], optionally requested by MoveInput
			//"Decided Tiles" Td[], optionally requested by MoveExecutor. May or may not hide T[] and/or Tc[].
		//MoveIO also interprets player input (by mouse/keyboard/etc), optionally requests the temporary display of Tc[] during the decision-making process, and once finished requests that the MoveExecutor perform the actual move.
		//MoveExecutor optionally sets Td[] on the MoveFeedback while performing the actual move (pursuant to MoveStrategy's success flag). 
		//  MoveExecutors may be reversible.
		//  This is where the character's Unity transform is manipulated and the map is updated to reflect the character's new position.
		
		//For example, an FFT character would have a MoveStrategy that did a flood-fill on the map with Move and Jump calculations (or, if they had Fly or Teleport, other MoveStrategies), a MoveIO script using an Overlay and an update loop that's click-based, and a MoveExecutor that animates from pos to dest, taking jumps and so on into account -- all this wrapped in a CT-based scheduler.
		//Valkyria Chronicles would use a phase-based scheduler with user-decided turns based on resource expenditure (rather than just "use all units"), a movement strategy that offers a circular radius around the player, movement feedback that shows that radius (or shows, in lighter tint, the terrain reachable before the player's action points expire; with a darker tint, the unreachable terrain), MoveInput that works off of keyboard, mouse, or controller, and a MoveExecutor that merely updates the character's position and lets the map know about it.
		//This system is opt-in. Valkyria movement could work simply with a Scheduler and a custom script that moves the character according to keyboard input while it's active, updating the map (if any) as needed.
	}	
	void OnDestroy() {
		if(map != null && map.scheduler != null) {
			map.scheduler.RemoveCharacter(this);
		}
	}
}
