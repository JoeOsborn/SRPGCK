using UnityEngine;
using System.Collections.Generic;

public class MoveExecutor : MonoBehaviour {
	[System.NonSerialized]
	public Map map;
	
	public Vector3 transformOffset = new Vector3(0, 5, 0);
	
	public Vector3 position;
	protected PathNode destNode;
	public Vector3 temporaryPosition;
	protected PathNode temporaryDestNode;
	
	protected List<PathNode> animNodes;
	int pathIndex;
	
	public bool animateTemporaryMovement=false;
	
	public delegate void MoveFinished(Vector3 src, PathNode endNode, bool finishedNicely);

	protected Vector3 moveOrigin;
	protected float moveTimeRemaining=0;
	protected MoveFinished moveCallback;
	
	public float XYSpeed = 6;
	public float ZSpeed = 10;

	virtual public void Start () {

	}
	
	virtual protected void ClearPath() {
		if(animNodes != null) {
			animNodes.Clear();
		}
		pathIndex = -1;
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
		get { return map.TransformPointWorld(temporaryDestNode.pos); }
	}
	
	public Vector3 destination {
		get { return map.TransformPointWorld(destNode.pos); }
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
		get { return this.transform.position-transformOffset; }
		set { this.transform.position = value+transformOffset; }
	}
	
	virtual public void Update () {
		Vector3 tp = transformPosition;
		if(map == null) {
			if(this.transform.parent != null) {
				map = this.transform.parent.GetComponent<Map>();
			}
			if(map == null) { 
				Debug.Log("Characters must be children of Map objects!");
				return;
			}
			Vector3 startPos = map.InverseTransformPointWorld(tp);
			startPos.x = Mathf.Round(startPos.x);
			startPos.y = Mathf.Round(startPos.y);
			startPos.z = Mathf.Round(startPos.z);
			destNode = new PathNode(startPos, null, 0);
			position = destination;
			temporaryDestNode = destNode;
			temporaryPosition = temporaryDestination;
			transformPosition = tp = position;
		}
		if(moveTimeRemaining > 0 && animNodes != null && animNodes.Count > 0) {
			moveTimeRemaining -= Time.deltaTime;
			Vector3 animDest = map.TransformPointWorld(animNodes[pathIndex].pos);
			float dsquared = (animDest-tp).sqrMagnitude;
			float dt = Time.deltaTime;
			float AnimatedMoveSquareDistanceThreshold = (XYSpeed*dt)*(XYSpeed*dt)+(ZSpeed*dt)*(ZSpeed*dt);
			if(dsquared < AnimatedMoveSquareDistanceThreshold) {
				if(pathIndex > 0) {
					pathIndex--;
					
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
				Vector3 d = animDest - tp;
				//slide character in x/y/z towards animDest by own xyspeed/zspeed
				float dx = Mathf.Min(Mathf.Abs(d.x), XYSpeed*dt)*Mathf.Sign(d.x);
				float dy = Mathf.Min(Mathf.Abs(d.y),  ZSpeed*dt)*Mathf.Sign(d.y);
				float dz = Mathf.Min(Mathf.Abs(d.z), XYSpeed*dt)*Mathf.Sign(d.z);
				transformPosition = new Vector3(tp.x + dx, tp.y + dy, tp.z + dz);
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
