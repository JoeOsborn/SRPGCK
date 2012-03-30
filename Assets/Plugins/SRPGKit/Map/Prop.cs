using UnityEngine;
using System.Collections;

[AddComponentMenu("SRPGCK/Prop")]
public class Prop : MonoBehaviour {
	Vector3 lastPosition = Vector3.zero;

	void Start () {
		lastPosition = transform.position;
	}

	void FixedUpdate() {
		if(lastPosition == Vector3.zero) {
			lastPosition = transform.position;
		}
		if(transform.position != lastPosition) {
			Map m = transform.parent.GetComponent<Map>();
			if(m != null) { m.InvalidateOverlayMesh(); }
		}
	}
}
