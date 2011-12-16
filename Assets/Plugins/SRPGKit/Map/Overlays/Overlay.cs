using UnityEngine;
using System.Collections.Generic;

public abstract class Overlay : MonoBehaviour {
	public string category;
	public int identifier;
	public Color color = Color.clear;
	
	protected Map map;
	protected Material shadeMaterial;
	
	// Use this for initialization
	virtual public void Start () {
		
	}
	
	virtual protected void CreateShadeMaterial() {
		
	}
	
	virtual public void OverlayMeshInvalidated() {
		Destroy(this.renderer);
	}
	
	protected void FindMap() {
		if(map == null) {
			if(this.transform.parent != null) {
				map = this.transform.parent.GetComponent<Map>();
			}
			if(map == null) { 
				Debug.LogError("Overlay must be child of a map");
			}
		}	
	}
	
	virtual public void Update() {
		FindMap();
		if(shadeMaterial == null) {
			CreateShadeMaterial();
		}
		if(this.renderer == null && shadeMaterial != null) {
			//add a mesh renderer
			MeshFilter mf = gameObject.GetComponent<MeshFilter>();
			if(mf == null) { mf = gameObject.AddComponent<MeshFilter>(); }
			mf.mesh = map.OverlayMesh;
			AddShadeMaterial();
			MeshCollider mc = this.gameObject.GetComponent<MeshCollider>();
			if(mc == null) { mc = this.gameObject.AddComponent<MeshCollider>(); }			
			mc.convex = false;
		}
	}
	
	virtual protected void AddShadeMaterial() {
		MeshRenderer mr = this.gameObject.AddComponent<MeshRenderer>();
		mr.material = shadeMaterial;
	}
	
	virtual public bool ContainsPosition(Vector3 hitSpot) {
		return PositionAt(hitSpot) != null;
	}

	public abstract PathNode PositionAt(Vector3 hitSpot);
}
