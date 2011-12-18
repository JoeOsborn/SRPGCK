using UnityEngine;
using System.Collections;

public enum RadialOverlayType {
	Sphere,
	Cylinder
};

public class RadialOverlay : Overlay {
	
	//TODO: clip by walls -- reachability mask? but what about the outer volume?
	
	public RadialOverlayType type=RadialOverlayType.Sphere;
	public Vector3 origin=Vector3.zero;
	public float radius=10, tileRadius=1, height=10;
	public bool drawRim=false;
	public bool drawOuterVolume=false;
	public bool invert=false;
	
	protected GameObject rimObject;
	protected Material rimShadeMaterial;
	
	override public void Update() {
		base.Update();
		this.transform.localPosition = new Vector3(0,0.01f,0);
		if(drawOuterVolume && rimObject == null) {
			if(type == RadialOverlayType.Sphere) {
				rimObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				rimObject.transform.parent = transform;
				rimObject.transform.localScale = new Vector3(radius*2, radius*2, radius*2);
				rimObject.transform.position = origin;
			} else if(type == RadialOverlayType.Cylinder) {
				rimObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
				rimObject.transform.parent = transform;
				rimObject.transform.localScale = new Vector3(radius*2, height, radius*2);
				rimObject.transform.position = origin;
			}
			rimObject.GetComponent<Collider>().isTrigger = true;
			if(rimShadeMaterial != null) {
				rimObject.GetComponent<MeshRenderer>().material = rimShadeMaterial;
			}
		}
	}
	
	public void UpdateOriginAndRadius(Vector3 pos, float rad) {
		origin = pos;
		radius = rad;
		if(shadeMaterial != null) {
			shadeMaterial.SetVector("_Origin", new Vector4(origin.x, origin.y, origin.z, 1));
			shadeMaterial.SetFloat("_Radius", radius);		
		}
	}
	
	override protected void CreateShadeMaterial () {
		if(color == Color.clear) {
			return;
		}

		Shader shader=null;
		if(type == RadialOverlayType.Sphere) { 
			shader = Shader.Find("Custom/SphereOverlayClip"); 
		} else if(type == RadialOverlayType.Cylinder) { 
			if(invert) {
				shader = Shader.Find("Custom/CylinderOverlayClipInverted");
			} else {
				shader = Shader.Find("Custom/CylinderOverlayClip");
			}
		}
		if(shader == null) { return; }
		shadeMaterial = new Material(shader);
		UpdateOriginAndRadius(origin, radius);
		if(type == RadialOverlayType.Sphere) {
			shadeMaterial.SetFloat("_Invert", invert ? 1 : 0);
		} else if(type == RadialOverlayType.Cylinder) { 
			shadeMaterial.SetFloat("_Height", height);
		}
		shadeMaterial.SetFloat("_DrawRim", drawOuterVolume ? 0 : (drawRim ? 1 : 0));
		shadeMaterial.SetColor("_Color", color);

		if(drawOuterVolume) {
			Shader rimShader = Shader.Find("Custom/RadialOverlayOuterVolume");
			rimShadeMaterial = new Material(rimShader);
			rimShadeMaterial.SetColor("_Color", color);
			shadeMaterial.SetFloat("_Invert", invert ? 1 : 0);
		}
	}
	
	override public PathNode PositionAt(Vector3 hitSpot) {
		FindMap();
		//TODO: reachable?
		Vector3 tileOrigin = map.InverseTransformPointWorld(origin);
		float distance = Vector3.Distance(hitSpot, tileOrigin);
		if(distance > tileRadius) {
			return null;
		}
		return new PathNode(hitSpot, null, distance);
	}
}
