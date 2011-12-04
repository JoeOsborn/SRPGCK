using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CTCharacter : MonoBehaviour {
	[HideInInspector]
	[SerializeField]
	protected float _speed=6;
	[HideInInspector]
	[SerializeField]
	protected float _ct=0;

	[HideInInspector]
	[SerializeField]
	protected bool _hasMoved=false;
	[HideInInspector]
	[SerializeField]
	protected bool _hasActed=false;
	
	virtual public float CT {
		get {
			return _ct;
		}
		set {
			_ct = value;
		}
	}
	virtual public float Speed {
		get {
			return _speed;
		}
		set {
			_speed = value;
		}
	}

	virtual public bool HasMoved {
		get {
			return _hasMoved;
		}
		set {
			_hasMoved = value;
		}
	}

	virtual public bool HasActed {
		get {
			return _hasActed;
		}
		set {
			_hasActed = value;
		}
	}
	
	virtual public float PerActivationCTCost {
		get {
			return ((CTScheduler)GetComponent<Character>().map.scheduler).defaultPerActivationCTCost;
		}
	}
	virtual public float PerMoveCTCost {
		get {
			return ((CTScheduler)GetComponent<Character>().map.scheduler).defaultPerMoveCTCost;
		}
	}
	virtual public float PerActionCTCost {
		get {
			return ((CTScheduler)GetComponent<Character>().map.scheduler).defaultPerActionCTCost;
		}
	}
	virtual public float PerTileCTCost {
		get {
			return ((CTScheduler)GetComponent<Character>().map.scheduler).defaultPerTileCTCost;
		}
	}
	virtual public float MaxCT {
		get {
			return ((CTScheduler)GetComponent<Character>().map.scheduler).defaultMaxCT;
		}
	}
}
