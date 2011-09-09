using UnityEngine;
using System.Collections;

public class Character : MonoBehaviour {
	
	public bool isActive=false;
	
	Map map=null;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		if(map == null) {
			if(this.transform != null) {
				map = this.transform.parent.GetComponent<Map>();
			}
			if(map == null) { 
				Debug.Log("Characters must be children of Map objects!");
				return; 
			}
		}
		if(Input.GetMouseButtonDown(0)) {
			Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if(this.collider.Raycast(r, out hit, 100)) {
				if(!isActive) {
					isActive = true;
					//figure out our valid moves

					//then ask the map to present an overlay on those tiles
					map.PresentOverlay(
						"move", this.gameObject.GetInstanceID(), 
						new Color(0.2f, 0.3f, 0.9f, 0.7f),
						new Vector4[]{
							new Vector4(0,0,0,1),
							new Vector4(1,0,0,1),
							new Vector4(0,1,0,1),
							new Vector4(3,3,0,1),
							new Vector4(3,2,0,8),
						}
					);
					//when we go inactive
					Debug.Log("Now I been clicked!");
				}
			} else {
				if(isActive) {
					if(map.IsShowingOverlay("move", this.gameObject.GetInstanceID())) {
						map.RemoveOverlay("move", this.gameObject.GetInstanceID());
					}
					isActive = false;
					Debug.Log("Inactive now!");
				}
			}
		}
	}	
}
