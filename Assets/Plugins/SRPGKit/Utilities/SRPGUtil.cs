using UnityEngine;
using System.Collections;

public enum LockedFacing {
	XP,
	YP,
	XN,
	YN
}

public class SRPGUtil : MonoBehaviour {
	static public Vector3 Trunc(Vector3 v) {
		return new Vector3((int)v.x, (int)v.y, (int)v.z);
	}

	static public Vector3 Round(Vector3 v) {
		return new Vector3(Mathf.Round(v.x), Mathf.Round(v.y), Mathf.Round(v.z));
	}

	static public LockedFacing LockFacing(float f, float mapY=0) {
		const float TAU = 360;
		float localY = (f - mapY);
		while(localY >= TAU) { localY -= TAU; }
		while(localY < 0) { localY += TAU; }
		if(localY < TAU/8 || localY >= 7*TAU/8) {
			return LockedFacing.XP;
		} else if(localY >= TAU/8 && localY < 3*TAU/8) {
			return LockedFacing.YP;
		} else if(localY >= 3*TAU/8 && localY < 5*TAU/8) {
			return LockedFacing.XN;
		} else if(localY >= 5*TAU/8 && localY < 7*TAU/8) {
			return LockedFacing.YN;
		}
		Debug.LogError("No matching direction for Q");
		return LockedFacing.XN;
	}

	static public float WrapAngle(float f) {
		if(float.IsNaN(f)) { Debug.LogError("NAN!"); }
		if(float.IsInfinity(f)) { Debug.LogError("INFINITY!"); }
		float r = f;
		while(r < 0) { r += 360; }
		if(r >= 360) { r -= 360; }
		return r;
	}

	static public bool AngleBetween(float a, float mn, float mx) { //ccw
		float ang = WrapAngle(a);
		float min = WrapAngle(mn);
		float max = WrapAngle(mx);
		//does a ccw (+) sweep from min to max pass through ang?
		//0..ang..360
		if(min <= max && (ang >= min && ang <= max)) { return true; }
		//270..ang..90
		if(min >= max && (ang <= max || ang >= min)) {
			return true;
		}
		return false;
	}
}
