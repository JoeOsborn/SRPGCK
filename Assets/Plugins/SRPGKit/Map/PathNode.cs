using UnityEngine;

public enum PathDecision {
	Invalid,
	PassOnly,
	Normal
};

[System.Serializable]
public class PathNode {
	public Vector3 pos=Vector3.zero;
	public PathNode prev=null;
	public float distance=0;
	public bool canStop=true;
	public bool isLeap=false;

	public bool isWall=false;
	public bool isEnemy=false;

	public float altitude = 0;
	public float velocity = 0;

	public PathNode(Vector3 ps, PathNode pr, float dist) {
		pos = ps; prev = pr; distance = dist;
	}
	public Vector3 position { get { return pos; } }
	public int dz {
		get { return (int)Mathf.Abs(signedDZ); }
	}
	public int signedDZ {
		get { return SignedDZFrom(prev != null ? prev.pos : pos); }
	}
	public float xyDistance {
		get { return XYDistanceFrom(prev != null ? prev.pos : pos); }
	}
	public float xyDistanceFromStart {
		get {
			float dist=0;
			PathNode s = this;
			while(s.prev != null) {
				dist += Vector2.Distance(
					new Vector2(s.pos.x, s.pos.y),
					new Vector2(s.prev.pos.x, s.prev.pos.y)
				);
				s = s.prev;
			}
			return dist;
		}
	}
	public int SignedDZFrom(Vector3 prevPos) {
		return (int)(pos.z - prevPos.z);
	}
	public float XYDistanceFrom(Vector3 prevPos) {
		return (int)(Mathf.Abs(pos.x - prevPos.x)+Mathf.Abs(pos.y - prevPos.y));
	}
	public override bool Equals(object obj) {
    if(obj is PathNode) {
      return this.Equals((PathNode)obj);
    }
    return false;
  }
  public bool Equals(PathNode p) {
    return p != null && pos == p.pos;
  }
  public override int GetHashCode() {
    return pos.GetHashCode();
  }
	public override string ToString() {
		return "PN:"+pos+" with cost "+distance+(prev != null ? " via "+prev.pos : "")+"; wall? "+isWall+" enemy? "+isEnemy + " can stop? "+canStop+" leap? "+isLeap;
	}
};
