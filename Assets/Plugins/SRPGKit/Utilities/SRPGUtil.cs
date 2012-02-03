using UnityEngine;
using System.Collections;

public class SRPGUtil : MonoBehaviour {
	static public Vector3 Trunc(Vector3 v) {
		return new Vector3((int)v.x, (int)v.y, (int)v.z);
	}

	static public Vector3 Round(Vector3 v) {
		return new Vector3(Mathf.Round(v.x), Mathf.Round(v.y), Mathf.Round(v.z));
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
