using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("")]
public class RoundPointsCharacter : MonoBehaviour {	
	
	protected Character character;
	
	[SerializeField]
	protected int _usesThisRound;
	[SerializeField]
	protected float _limiter;
		
	virtual public void Start() {
		character = GetComponent<Character>();
	}
	
	virtual public void Update() {
		if(character == null) { character = GetComponent<Character>(); }
	}
	
	virtual public int UsesThisRound {
		get {
			return _usesThisRound;
		}
		set {
			_usesThisRound = value;
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
			return ((TeamRoundsPointsScheduler)character.map.scheduler).defaultLimiterMax;
		}
	}
	virtual public float MaxTurnTime {
		get {
			return ((TeamRoundsPointsScheduler)character.map.scheduler).defaultLimiterMax;
		}
	}
	virtual public float TurnDiminishScale {
		get {
			return ((TeamRoundsPointsScheduler)character.map.scheduler).defaultLimiterDiminishScale;
		}
	}
	virtual public float PerUnitMovementAPCost {
		get {
			return ((TeamRoundsPointsScheduler)character.map.scheduler).defaultMoveAPCost;
		}
	}
	virtual public float PerSecondAPCost {
		get {
			return ((TeamRoundsPointsScheduler)character.map.scheduler).defaultAPLossPerSecond;
		}
	}
}
