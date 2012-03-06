using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class MoveExecutor {
	public bool lockToGrid=true;

	public Character character;
	public Map map;

	public Vector3 transformOffset { get {
		return character.transformOffset;
	} }

	[HideInInspector]
	public Vector3 position;
	protected PathNode destNode;
	[HideInInspector]
	public Vector3 temporaryPosition;
	protected PathNode temporaryDestNode;

	protected List<PathNode> animNodes;
	int pathIndex;

	public bool animateTemporaryMovement=false;
	public bool specialMoving=false;

	public delegate void MoveFinished(Vector3 src, PathNode endNode, bool finishedNicely);

	public Vector3 moveOrigin;
	protected float moveTimeRemaining=0;
	protected MoveFinished moveCallback;

	public float XYSpeed = 12;
	public float ZSpeedUp = 15;
	public float ZSpeedDown = 20;

	public bool isActive=false;

	[HideInInspector]
	public MoveType currentMoveType;

	virtual protected void ClearPath() {
		if(animNodes != null) {
			animNodes.Clear();
		}
		pathIndex = -1;
	}

	virtual public void Cancel() {
		character.TriggerAnimation("idle");
		transformPosition = position;
	}

	virtual protected void CreatePath(PathNode pn) {
		if(animNodes == null) { animNodes = new List<PathNode>(); }
		animNodes.Clear();
		// Debug.Log("animate to "+pn);
		PathNode cur = pn;
		do {
			if(animNodes.Any(p => Object.ReferenceEquals(p,cur))) {
				Debug.LogError("infinite node loop");
				break;
			}
			// Debug.Log("by "+cur);
			animNodes.Add(cur);
			cur = cur.prev;
		} while(cur != null);
		pathIndex = animNodes.Count-1;
		if(animNodes.Count > 1) {
			currentMoveType = MoveTypeForMove(animNodes[pathIndex-1].pos, animNodes[pathIndex].pos);
			character.Facing = FacingForMove(animNodes[pathIndex-1].pos, animNodes[pathIndex].pos);
		} else {
			currentMoveType = MoveType.None;
		}
	}

	virtual public void TemporaryMoveTo(PathNode pn, MoveFinished callback, float timeout=10.0f, bool special = false) {
		Debug.Log("temporary move to "+pn);
		specialMoving = special;
		temporaryDestNode = pn;
		moveOrigin = temporaryPosition;
		moveCallback = callback;
		if(!animateTemporaryMovement) {
			temporaryPosition = temporaryDestination;
			transformPosition = temporaryPosition;
			moveCallback(map.InverseTransformPointWorld(moveOrigin), temporaryDestNode, true);
			specialMoving = false;
			moveCallback = null;
			ClearPath();
		} else {
			moveTimeRemaining = timeout;
			CreatePath(pn);
		}
	}

	public Vector3 temporaryDestination {
		get { return map.TransformPointWorld(temporaryDestNode.pos); }
	}

	public Vector3 destination {
		get { return map.TransformPointWorld(destNode.pos); }
	}

	virtual public void IncrementalMoveTo(PathNode pn, MoveFinished callback, float timeout=10.0f, bool special=false) {
		Debug.Log("incremental move to "+pn);
		specialMoving = special;
		transformPosition = position;
		destNode = pn;
	  temporaryDestNode = pn;
		moveOrigin = position;
		moveCallback = callback;
		moveTimeRemaining = timeout;
		CreatePath(pn);
	}

	public bool IsMoving { get {
		return pathIndex >= 0 &&
		  animNodes != null &&
			animNodes.Count > 0 &&
			animNodes[0].pos != transformPosition;
	} }

	virtual public void MoveTo(PathNode pn, MoveFinished callback, float timeout=10.0f, bool special=false) {
		Debug.Log("move to "+pn);
		IncrementalMoveTo(pn, callback, timeout, special);
	}

	virtual public void SpecialMoveTo(PathNode pn, MoveFinished callback, float timeout=10.0f) {
		Debug.Log("special move to "+pn);
		MoveTo(pn, callback, timeout, true);
	}

	public Vector3 transformPosition {
		get { return character.transform.position-transformOffset; }
		set { character.transform.position = value+transformOffset; }
	}

	public enum MoveType {
		None,

		Step,
		Hop,
		Jump,
		Fall,
		Leap,

		Special,
		SpecialFall
	};

	/*animations:
		step (|dy| <= 1) (simultaneous slide)
		hop (|dy| == 2) (simultaneous slide)
		jump (dy > 2) (jump, then slide)
		fall (dy < -2) (slide, then fall)
		leap (|dx/dz| > 1) (slide with arc)

		special (|dy| <= 1) (simultaneous slide)
		special-fall (|dy| > 1) (slide, then fall)*/

	public MoveType MoveTypeForMove(Vector3 to, Vector3 from) {
		float dx = to.x-from.x;
		float dy = to.y-from.y;
		float dz = map.AbsDZForMove(to, from);
		float adz = Mathf.Abs(dz);
		if(specialMoving) {
			if(dz != 0) { return MoveType.SpecialFall; }
			if(dx != 0 || dy != 0) { return MoveType.Special; }
			return MoveType.None;
		}
		if(Mathf.Abs(dx)+Mathf.Abs(dy) > 1) { return MoveType.Leap; }
		if(adz <= 1) { return MoveType.Step; }
		if(adz == 2) { return MoveType.Hop; }
		if(dz > 2) { return MoveType.Jump; }
		if(dz < -2) { return MoveType.Fall; }
		return MoveType.None;
	}

	public float FacingForMove(Vector3 to, Vector3 from) {
		return Mathf.Atan2(to.y-from.y, to.x-from.x)*Mathf.Rad2Deg;
	}

	virtual public void Activate() {
		isActive = true;
		Vector3 startPos = character.TilePosition;
		if(lockToGrid) {
			startPos.x = Mathf.Round(startPos.x);
			startPos.y = Mathf.Round(startPos.y);
			startPos.z = Mathf.Round(startPos.z);
		}
		destNode = new PathNode(startPos, null, 0);
		position = destination;
		moveOrigin = position;
		temporaryDestNode = destNode;
		temporaryPosition = temporaryDestination;
		transformPosition = position;
	}
	virtual public void Deactivate() {
		isActive = false;
	}

	virtual public void Update () {
		if(character == null || map == null) { return; }
		Vector3 tp = transformPosition;
		if(moveTimeRemaining > 0 && animNodes != null && animNodes.Count > 0) {
			moveTimeRemaining -= Time.deltaTime;
			Vector3 animDest = map.TransformPointWorld(animNodes[pathIndex].pos);
			Vector3 d = animDest-tp;
			float dsquared = d.sqrMagnitude;
			float dt = Time.deltaTime;
			float zspeed = d.y < 0 ? ZSpeedDown : ZSpeedUp;
			float AnimatedMoveSquareDistanceThreshold = (XYSpeed*dt)*(XYSpeed*dt)+(zspeed*dt)*(zspeed*dt);
			if(dsquared < AnimatedMoveSquareDistanceThreshold) {
				if(pathIndex > 0) {
					pathIndex--;
					PathNode pn = animNodes[pathIndex];
					currentMoveType = MoveTypeForMove(pn.pos, animNodes[pathIndex+1].pos);
					character.Facing = FacingForMove(pn.pos, animNodes[pathIndex+1].pos);
				} else {
					transformPosition = tp = temporaryDestination;
					if(position != destination) {
					  position = destination;
//						Debug.Log("permanent move");
					} else {
//						Debug.Log("temporary move");
					}
				  temporaryPosition = temporaryDestination;
			  	if(moveCallback != null) {
			  		moveCallback(map.InverseTransformPointWorld(moveOrigin), temporaryDestNode, true);
						specialMoving = false;
			  		moveCallback = null;
			  	}
					ClearPath();
				}
			} else {
				Vector3 newPos = tp;
				switch(currentMoveType) {
					default:
					case MoveType.Leap:
						newPos.x = Mathf.MoveTowards(newPos.x, animDest.x, XYSpeed*2*dt);
						newPos.y = Mathf.MoveTowards(newPos.y, animDest.y, zspeed*dt);
						newPos.z = Mathf.MoveTowards(newPos.z, animDest.z, XYSpeed*2*dt);
						character.TriggerAnimation("leaping");
						break;
					case MoveType.Special:
						newPos.x = Mathf.MoveTowards(newPos.x, animDest.x, XYSpeed*dt);
						newPos.y = Mathf.MoveTowards(newPos.y, animDest.y, zspeed*dt);
						newPos.z = Mathf.MoveTowards(newPos.z, animDest.z, XYSpeed*dt);
						character.TriggerAnimation("special");
						break;
					case MoveType.Step:
						newPos.x = Mathf.MoveTowards(newPos.x, animDest.x, XYSpeed*dt);
						newPos.y = Mathf.MoveTowards(newPos.y, animDest.y, zspeed*dt);
						newPos.z = Mathf.MoveTowards(newPos.z, animDest.z, XYSpeed*dt);
						character.TriggerAnimation("stepping");
						break;
					case MoveType.Hop:
						newPos.x = Mathf.MoveTowards(newPos.x, animDest.x, XYSpeed*dt);
						newPos.y = Mathf.MoveTowards(newPos.y, animDest.y, zspeed*2*dt);
						newPos.z = Mathf.MoveTowards(newPos.z, animDest.z, XYSpeed*dt);
						character.TriggerAnimation("hopping");
						break;
					case MoveType.Jump:
						if(d.y != 0) {
							newPos.y = Mathf.MoveTowards(newPos.y, animDest.y, zspeed*dt);
							character.TriggerAnimation("jumping");
						} else {
							newPos.x = Mathf.MoveTowards(newPos.x, animDest.x, XYSpeed*dt);
							newPos.z = Mathf.MoveTowards(newPos.z, animDest.z, XYSpeed*dt);
							character.TriggerAnimation("jumpsliding");
						}
						break;
					case MoveType.Fall:
						if(d.x != 0 || d.z != 0) {
							newPos.x = Mathf.MoveTowards(newPos.x, animDest.x, XYSpeed*dt);
							newPos.z = Mathf.MoveTowards(newPos.z, animDest.z, XYSpeed*dt);
							character.TriggerAnimation("fallsliding");
						} else {
							newPos.y = Mathf.MoveTowards(newPos.y, animDest.y, zspeed*dt);
							character.TriggerAnimation("falling");
						}
						break;
					case MoveType.SpecialFall:
						if(d.x != 0 || d.z != 0) {
							newPos.x = Mathf.MoveTowards(newPos.x, animDest.x, XYSpeed*dt);
							newPos.z = Mathf.MoveTowards(newPos.z, animDest.z, XYSpeed*dt);
							if(d.y < 0) {
								character.TriggerAnimation("special-fallsliding");
							} else if(d.y > 0) {
								character.TriggerAnimation("special-jumpsliding");
							}
						} else {
							newPos.y = Mathf.MoveTowards(newPos.y, animDest.y, zspeed*dt);
							character.TriggerAnimation("special");
						}
						break;
				}
				transformPosition = newPos;
			}
		} else if(position != destination || temporaryPosition != temporaryDestination) {
			position = destination;
			temporaryPosition = temporaryDestination;
			Debug.Log("failsafe");
			transformPosition = position;
			if(moveCallback != null) {
				moveCallback(map.InverseTransformPointWorld(moveOrigin), temporaryDestNode, false);
				specialMoving = false;
				moveCallback = null;
			}
			ClearPath();
		}
	}
}
