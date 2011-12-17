using UnityEngine;
using System.Collections;

[System.Serializable]
public class MoveIO {
	public bool supportKeyboard = true;
	public bool supportMouse = true;
	
	public bool requireConfirmation = true;
	public bool awaitingConfirmation=false;

	public bool lockToGrid = false;
	public bool performTemporaryMoves = false;

	public float indicatorCycleLength=1.0f;

	[System.NonSerialized]
	public MoveSkill owner;
	
	[HideInInspector]
	public bool isActive;
	
	public virtual void Activate () {
		isActive = true;
	}
	
	public virtual void Update () {

	}
	
	public virtual void PresentMoves() {

	}
	
	public virtual void Deactivate() {
		isActive = false;
	}
		
}
