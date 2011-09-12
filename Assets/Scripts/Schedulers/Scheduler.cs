using UnityEngine;
using System.Collections.Generic;

public class Scheduler : MonoBehaviour {
	public List<Character> characters;
	public Character activeCharacter;
	
	public void AddCharacter(Character c) {
		if(!characters.Contains(c)) {
			characters.Add(c);
		}
	}
	
	public void RemoveCharacter(Character c) {
		characters.Remove(c);
	}
	
	public void Activate(Character c) {
		activeCharacter = c;
		c.SendMessage("Activate", null, SendMessageOptions.RequireReceiver);
	}
	
	public void Deactivate(Character c) {
		if(activeCharacter == c) { activeCharacter = null; }
		c.SendMessage("Deactivate", null, SendMessageOptions.RequireReceiver);
	}
	
	void Start () {
	
	}
	
	void Update () {
		//SCHEDULER: "If there is no active character and we have clicked on a character, activate it"
		// (later, limit by phase and/or use a network adaptor)
		if(activeCharacter != null && activeCharacter.isActive) { return; }
		if(activeCharacter != null && !activeCharacter.isActive) { Deactivate(activeCharacter); }
		if(Input.GetMouseButtonDown(0)) {
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit[] hits = Physics.RaycastAll(r);
			float closestDistance = Mathf.Infinity;
			Character closestCharacter = null;
			foreach(RaycastHit h in hits) {
				if(h.transform.GetComponent<Character>()!=null) {
					if(h.distance < closestDistance) {
						closestDistance = h.distance;
						closestCharacter = h.transform.GetComponent<Character>();
					}
				}
			}
			if(closestCharacter != null && !closestCharacter.isActive) {
				Activate(closestCharacter);
			}
		}
	}
}
