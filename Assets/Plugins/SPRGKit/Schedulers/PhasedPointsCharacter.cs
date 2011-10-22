using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PhasedPointsCharacter : MonoBehaviour {	
	
	protected Character character;
	
	[SerializeField]
	protected int _usesThisPhase;
	[SerializeField]
	protected float _limiter;
		
	virtual public void Start() {
		character = GetComponent<Character>();
	}
	
	virtual public void Update() {
		if(character == null) { character = GetComponent<Character>(); }
	}
	
	virtual public int UsesThisPhase {
		get {
			return _usesThisPhase;
		}
		set {
			_usesThisPhase = value;
		}
	}
	virtual public float Limiter {
		get {
			return _limiter;
		}
		set {
			_limiter = value;
		}
	}
	
	virtual public float MaxTurnAP {
		get {
			return ((TeamPhasedPointsScheduler)character.map.scheduler).defaultLimiterMax;
		}
	}
	virtual public float MaxTurnTime {
		get {
			return ((TeamPhasedPointsScheduler)character.map.scheduler).defaultLimiterMax;
		}
	}
	virtual public float TurnDiminishScale {
		get {
			return ((TeamPhasedPointsScheduler)character.map.scheduler).defaultLimiterDiminishScale;
		}
	}
	virtual public float PerUnitMovementAPCost {
		get {
			return ((TeamPhasedPointsScheduler)character.map.scheduler).defaultMoveAPCost;
		}
	}
	virtual public float PerSecondAPCost {
		get {
			return ((TeamPhasedPointsScheduler)character.map.scheduler).defaultAPLossPerSecond;
		}
	}
}
