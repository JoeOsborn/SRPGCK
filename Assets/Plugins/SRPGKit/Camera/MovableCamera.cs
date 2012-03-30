using UnityEngine;
using System.Collections;

[AddComponentMenu("SRPGCK/Movable Camera")]
[ExecuteInEditMode]
public class MovableCamera : MonoBehaviour {
	//we can do camera shake here later, too
	float distance=30; //camera.transform.y and camera.orthosize (if ortho)
	public float targetDistance=30;
	float tilt=30; //euler.x
	public float targetTilt=30;
	float rotation=45; //euler.y
	public float targetRotation=45;
	Vector3 pivot=Vector3.zero; //transform.position
	public Vector3 targetPivot=Vector3.zero;
	public float pivotMoveSpeed=60;
	public float cameraMoveSpeed=45;
	public float tiltSpeed=60;
	public float rotateSpeed=120;
	
	Vector3 lastPivot;
	float lastDistance;
	float lastRotation;
	float lastTilt;

	// Use this for initialization
	void Start () {
		lastPivot = new Vector3(-1,-1,-1);
		lastDistance = -1;
		lastRotation = -1;
		lastTilt = -1;
	}
	
	// Update is called once per frame
	void Update() {
		Camera c = transform.Find("Main Camera").camera;
		Camera oc = c.transform.Find("Overlay Camera").camera;
		if(c == null) { return; }
		if(Application.isPlaying) {
			//DEBUG
/*			if(shouldShake){Shake(Random.Range(2.5f, 10),Random.Range(1.0f, 3));}*/
			//END DEBUG
			//consider replacing these with MoveTo tweens
			float dt = Time.deltaTime;
			if(iTween.Count(gameObject)==0) { 
				pivot = Vector3.MoveTowards(pivot, targetPivot, pivotMoveSpeed*dt);
			}
			distance = Mathf.MoveTowards(distance, targetDistance, cameraMoveSpeed*dt);
			tilt = Mathf.MoveTowardsAngle(tilt, targetTilt, tiltSpeed*dt);
			rotation = Mathf.MoveTowardsAngle(rotation, targetRotation, rotateSpeed*dt);
		} else {
			pivot = targetPivot;
			distance = targetDistance;
			tilt = targetTilt;
			rotation = targetRotation;
		}
		if(pivot != lastPivot) {
			transform.localPosition = pivot;
			lastPivot = pivot;
		}
		if(distance != lastDistance) {
			c.transform.position = -c.transform.forward * (distance*3);
			if(c.orthographic) {
				c.orthographicSize = distance;
				oc.orthographic = true;
				oc.orthographicSize = distance;
			}
			lastDistance = distance;
		}
		if(rotation != lastRotation) {
			transform.localRotation = Quaternion.Euler(0, rotation, 0);
			lastRotation = rotation;
		}
		if(tilt != lastTilt) {
			c.transform.localRotation = Quaternion.Euler(tilt, 0, 0);
			lastTilt = tilt;
		}
	}
	//DEBUG
/*	public bool shouldShake=false;*/
	//END DEBUG
	//Note: while shaking, pivot changes will not be respected.
	public void Shake(float intensity=5, float duration=1) {
		iTween.ShakePosition(gameObject, new Vector3(intensity, intensity, intensity), duration);
		//DEBUG
/*		shouldShake = false;*/
		//END DEBUG
	}
}
