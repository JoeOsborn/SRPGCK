using UnityEngine;
using System.Collections;

public class Prop : MonoBehaviour {

	Vector3 lastPosition = Vector3.zero;

	// Use this for initialization
	void Start () {
		lastPosition = transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		if(lastPosition == Vector3.zero) {
			lastPosition = transform.position;
		}
		if(transform.position != lastPosition) {
			Map m = transform.parent.GetComponent<Map>();
			if(m != null) { m.InvalidateOverlayMesh(); }
		}
	}
}
