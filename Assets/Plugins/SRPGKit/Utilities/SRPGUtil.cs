using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum FacingLock {
	FreeAngle=0,
	Cardinal=1,
	Ordinal=2,
	CardinalAndOrdinal=3
}

public enum LockedFacing {
	XP=0,
	YP=90,
	XN=180,
	YN=270,
	XPYP=45,
	XPYN=315,
	XNYP=135,
	XNYN=225,
	Invalid=-1
}

public static class SRPGUtil {
	static public Vector3 Trunc(Vector3 v) {
		return new Vector3((int)v.x, (int)v.y, (int)v.z);
	}

	static public Vector3 Round(Vector3 v) {
		return new Vector3(Mathf.Round(v.x), Mathf.Round(v.y), Mathf.Round(v.z));
	}

	static public LockedFacing LockFacing(float f, FacingLock lockType=FacingLock.Cardinal, float mapY=0) {
		const float TAU = 360;
		float localY = (f - mapY);
		while(localY >= TAU) { localY -= TAU; }
		while(localY < 0) { localY += TAU; }
		switch(lockType) {
			case FacingLock.FreeAngle:
				return (LockedFacing)f;
			case FacingLock.Cardinal:
				if(localY < TAU/8 || localY >= 7*TAU/8) {
					return LockedFacing.XP;
				} else if(localY >= TAU/8 && localY < 3*TAU/8) {
					return LockedFacing.YP;
				} else if(localY >= 3*TAU/8 && localY < 5*TAU/8) {
					return LockedFacing.XN;
				} else if(localY >= 5*TAU/8 && localY < 7*TAU/8) {
					return LockedFacing.YN;
				}
				break;
			case FacingLock.Ordinal:
				if(localY >= 0 && localY < TAU/4.0f) {
					return LockedFacing.XPYP;
				} else if(localY >= TAU/4.0f && localY < TAU/2.0f) {
					return LockedFacing.XNYP;
				} else if(localY >= TAU/2.0f && localY < 3*TAU/4.0f) {
					return LockedFacing.XNYN;
				} else if(localY >= 3*TAU/4.0f && localY < TAU) {
					return LockedFacing.XPYN;
				}
				break;
			case FacingLock.CardinalAndOrdinal:
				if(localY < TAU/16 || localY >= 15*TAU/16) {
					return LockedFacing.XP;
				} else if(localY >=   TAU/16 && localY < 3*TAU/16) {
					return LockedFacing.XPYP;
				} else if(localY >= 3*TAU/16 && localY < 5*TAU/16) {
					return LockedFacing.YP;
				} else if(localY >= 5*TAU/16 && localY < 7*TAU/16) {
					return LockedFacing.XNYP;
				} else if(localY >= 7*TAU/16 && localY < 9*TAU/16) {
					return LockedFacing.XN;
				} else if(localY >= 9*TAU/16 && localY <11*TAU/16) {
					return LockedFacing.XNYN;
				} else if(localY >=11*TAU/16 && localY <13*TAU/16) {
					return LockedFacing.YN;
				} else if(localY >=13*TAU/16 && localY <15*TAU/16) {
					return LockedFacing.XPYN;
				}
				break;
		}
		Debug.LogError("No matching direction for Q");
		return LockedFacing.Invalid;
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

	static public void ResizeList<T>(List<T> l, int c) where T:new() {
		while(c < l.Count) {
			l.RemoveAt(l.Count-1);
		}
		while(c > l.Count) {
			l.Add(new T());
		}
	}

	static public string JoinStr(this IEnumerable<string> strs, string join) {
		return string.Join(join, strs.ToArray());
	}
	static public string JoinStr(this IEnumerable<object> objs, string join) {
		return string.Join(join, objs.Select(o => o.ToString()).ToArray());
	}
	static public string JoinStr(this string[] strs, string join) {
		return string.Join(join, strs);
	}
	static public string JoinStr(this object[] objs, string join) {
		return string.Join(join, objs.Select(o => o.ToString()).ToArray());
	}
	static public string JoinStr(this StatChangeType[] scts, string join) {
		return string.Join(join, scts.Select(o => o.ToString()).ToArray());
	}
}
