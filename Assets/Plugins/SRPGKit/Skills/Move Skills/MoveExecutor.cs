using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MoveExecutor {
	[System.NonSerialized]
	public MoveSkill owner;
	
	public Vector3 transformOffset { get { 
		return owner.transformOffset; 
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
	
	public delegate void MoveFinished(Vector3 src, PathNode endNode, bool finishedNicely);

	protected Vector3 moveOrigin;
	protected float moveTimeRemaining=0;
	protected MoveFinished moveCallback;
	
	public float XYSpeed = 12;
	public float ZSpeedUp = 15;
	public float ZSpeedDown = 20;
	
	[HideInInspector]
	public MoveType currentMoveType;
	
	virtual protected void ClearPath() {
		if(animNodes != null) {
			animNodes.Clear();
		}
		pathIndex = -1;
	}
	
	virtual public void Cancel() {
		owner.character.TriggerAnimation("idle");
		transformPosition = position;
	}
	
	virtual protected void CreatePath(PathNode pn) {
		if(animNodes == null) { animNodes = new List<PathNode>(); }
		animNodes.Clear();
		PathNode cur = pn;
		do {
			animNodes.Add(cur);
			cur = cur.prev;
		} while(cur != null && animNodes.Count < 50);
		if(animNodes.Count >= 50) {
			Debug.Log("too many nodes!");
		}
		pathIndex = animNodes.Count-1;
		if(animNodes.Count > 1) {
			currentMoveType = MoveTypeForMove(pn.pos, animNodes[pathIndex-1].pos);
			owner.character.Facing = FacingForMove(pn.pos, animNodes[pathIndex-1].pos);
		} else {
			currentMoveType = MoveType.None;
		}
	}

	virtual public void TemporaryMoveTo(PathNode pn, MoveFinished callback, float timeout=10.0f) {
		temporaryDestNode = pn;
		moveOrigin = temporaryPosition;
		moveCallback = callback;
		if(!animateTemporaryMovement) {
			temporaryPosition = temporaryDestination;
			transformPosition = temporaryPosition;
			moveCallback(moveOrigin, temporaryDestNode, true);
			moveCallback = null;
			ClearPath();
		} else {
			moveTimeRemaining = timeout;
			CreatePath(pn);
		}
	}
	
	public Vector3 temporaryDestination {
		get { return owner.map.TransformPointWorld(temporaryDestNode.pos); }
	}
	
	public Vector3 destination {
		get { return owner.map.TransformPointWorld(destNode.pos); }
	}
	
	virtual public void IncrementalMoveTo(PathNode pn, MoveFinished callback, float timeout=10.0f) {
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
	
	virtual public void MoveTo(PathNode pn, MoveFinished callback, float timeout=10.0f) {
		IncrementalMoveTo(pn, callback, timeout);
	}
	
	public Vector3 transformPosition {
		get { return owner.character.transform.position-transformOffset; }
		set { owner.character.transform.position = value+transformOffset; }
	}
	
	public enum MoveType {
		None,
		
		Step,
		Hop, 
		Jump,
		Fall,
		Leap,

		Knockback,
		KnockbackFall
	};
	
	/*animations:	
		step (|dy| <= 1) (simultaneous slide)
		hop (|dy| == 2) (simultaneous slide)
		jump (dy > 2) (jump, then slide)
		fall (dy < -2) (slide, then fall)
		leap (|dx/dz| > 1) (slide with arc)
		
		knockback (|dy| <= 1) (simultaneous slide)
		knockback-fall (|dy| > 1) (slide, then fall)*/
	
	public MoveType MoveTypeForMove(Vector3 to, Vector3 from) {
		float dx = to.x-from.x;
		float dy = to.y-from.y;
		float dz = owner.map.AbsDZForMove(to, from);
		float adz = Mathf.Abs(dz);
		if(Mathf.Abs(dx)+Mathf.Abs(dy) > 1) { return MoveType.Leap; }
		if(adz <= 1) { return MoveType.Step; }
		if(adz == 2) { return MoveType.Hop; }
		if(dz > 2) { return MoveType.Jump; }
		if(dz < -2) { return MoveType.Fall; }
		return MoveType.None;
	}
	
	public Quaternion FacingForMove(Vector3 to, Vector3 from) {
		return Quaternion.LookRotation(to-from);
	}
	
	virtual public void Activate() {
		Vector3 startPos = owner.map.InverseTransformPointWorld(transformPosition);
		startPos.x = Mathf.Round(startPos.x);
		startPos.y = Mathf.Round(startPos.y);
		startPos.z = Mathf.Round(startPos.z);
		destNode = new PathNode(startPos, null, 0);
		position = destination;
		temporaryDestNode = destNode;
		temporaryPosition = temporaryDestination;
		transformPosition = position;	
	}
	virtual public void Deactivate() {
	}
	
	virtual public void Update () {
		if(owner == null) { return; }
		Vector3 tp = transformPosition;
		if(moveTimeRemaining > 0 && animNodes != null && animNodes.Count > 0) {
			moveTimeRemaining -= Time.deltaTime;
			Vector3 animDest = owner.map.TransformPointWorld(animNodes[pathIndex].pos);
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
					owner.character.Facing = FacingForMove(pn.pos, animNodes[pathIndex+1].pos);
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
			  		moveCallback(moveOrigin, temporaryDestNode, true);
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
						owner.character.TriggerAnimation("leaping");
						break;
					case MoveType.Knockback:
						newPos.x = Mathf.MoveTowards(newPos.x, animDest.x, XYSpeed*dt);
						newPos.y = Mathf.MoveTowards(newPos.y, animDest.y, zspeed*dt);
						newPos.z = Mathf.MoveTowards(newPos.z, animDest.z, XYSpeed*dt);
						owner.character.TriggerAnimation("knockback");
						break;
					case MoveType.Step:
						newPos.x = Mathf.MoveTowards(newPos.x, animDest.x, XYSpeed*dt);
						newPos.y = Mathf.MoveTowards(newPos.y, animDest.y, zspeed*dt);
						newPos.z = Mathf.MoveTowards(newPos.z, animDest.z, XYSpeed*dt);
						owner.character.TriggerAnimation("stepping");
						break;
					case MoveType.Hop:
						newPos.x = Mathf.MoveTowards(newPos.x, animDest.x, XYSpeed*dt);
						newPos.y = Mathf.MoveTowards(newPos.y, animDest.y, zspeed*2*dt);
						newPos.z = Mathf.MoveTowards(newPos.z, animDest.z, XYSpeed*dt);
						owner.character.TriggerAnimation("hopping");
						break;
					case MoveType.Jump:
						if(d.y != 0) {
							newPos.y = Mathf.MoveTowards(newPos.y, animDest.y, zspeed*dt);
							owner.character.TriggerAnimation("jumping");
						} else {
							newPos.x = Mathf.MoveTowards(newPos.x, animDest.x, XYSpeed*dt);
							newPos.z = Mathf.MoveTowards(newPos.z, animDest.z, XYSpeed*dt);
							owner.character.TriggerAnimation("jumpsliding");
						}
						break;
					case MoveType.Fall:
						if(d.x != 0 || d.z != 0) {
							newPos.x = Mathf.MoveTowards(newPos.x, animDest.x, XYSpeed*dt);
							newPos.z = Mathf.MoveTowards(newPos.z, animDest.z, XYSpeed*dt);
							owner.character.TriggerAnimation("fallsliding");
						} else {
							newPos.y = Mathf.MoveTowards(newPos.y, animDest.y, zspeed*dt);
							owner.character.TriggerAnimation("falling");
						}
						break;
					case MoveType.KnockbackFall:
						if(d.x != 0 || d.z != 0) {
							newPos.x = Mathf.MoveTowards(newPos.x, animDest.x, XYSpeed*dt);
							newPos.z = Mathf.MoveTowards(newPos.z, animDest.z, XYSpeed*dt);
							owner.character.TriggerAnimation("knockbackfallsliding");
						} else {
							newPos.y = Mathf.MoveTowards(newPos.y, animDest.y, zspeed*dt);
							owner.character.TriggerAnimation("knockbackfalling");
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
				moveCallback(moveOrigin, temporaryDestNode, false);
				moveCallback = null;
			}
			ClearPath();
		}
	}
}