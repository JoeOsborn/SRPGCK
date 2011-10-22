using UnityEngine;
using System.Collections;

public class GridOverlay : Overlay {
	public Vector4[] positions;
	public PathNode[] destinations;
	public Color selectedColor;
	
	protected Material selectedHighlightMaterial;
	
	public Vector4 selectedPoint;
	Vector4 lastSelectedPoint=new Vector4(-1,-1,-1,-1);
	
	// Update is called once per frame
	override public void Update() {
		base.Update();
		this.transform.localPosition = new Vector3(0,0.01f,0);
		if(lastSelectedPoint != selectedPoint) {
			selectedHighlightMaterial.SetVector("_SelectedPoint", selectedPoint);
//			Debug.Log("shm:"+selectedHighlightMaterial+", selpt:"+selectedPoint);
		}
		lastSelectedPoint = selectedPoint;
	}
	override protected void CreateShadeMaterial () {
		if(color == Color.clear) {
			return;
		}
		Shader shader = Shader.Find("Custom/GridOverlayClip");
		shadeMaterial = new Material(shader);
		//set shadeMaterial properties
		shadeMaterial.SetVector("_MapWorldOrigin", new Vector4(map.transform.position.x, map.transform.position.y, map.transform.position.z, 1) + new Vector4(-map.sideLength/2.0f,0.01f,-map.sideLength/2.0f,0));
		shadeMaterial.SetVector("_MapTileSize", new Vector4(map.sideLength, map.tileHeight, map.sideLength, 1));
		int mw = Mathf.NextPowerOfTwo((int)map.size.x);
		int mh = Mathf.NextPowerOfTwo((int)map.size.y);
		shadeMaterial.SetVector("_MapSizeInTiles", new Vector4(mw, 64, mh, 1));
		lastSelectedPoint = selectedPoint;
		shadeMaterial.SetColor("_Color", color);

		Texture2D boxTex = new Texture2D(
			mw, mh,
			TextureFormat.ARGB32, false
		);
		boxTex.filterMode = FilterMode.Point;
		boxTex.wrapMode = TextureWrapMode.Repeat;
		boxTex.anisoLevel = 1;

		Color32[] blank = new Color32[(int)mw*(int)mh];
		for(int i = 0; i < blank.Length; i++) {
			blank[i].r = 0;
			blank[i].g = 0;
			blank[i].b = 0;
			blank[i].a = 0;
		}
		boxTex.SetPixels32(blank);

		//pack overlay positions into texture
		for(int i = 0; i < positions.Length; i++) {
			Vector4 p = positions[i];
			float minZ = p.z/64.0f;
			float maxZ = (p.z+p.w)/64.0f;
			Color col = boxTex.GetPixel((int)p.x, (int)p.y);
			if(col.a == 0 && col.r == 0) {
				col.a = minZ;
				col.r = maxZ;
			} else if(col.a <= minZ && col.r >= maxZ) {
				//skip
			} else {
				if(col.g == 0 && col.b == 0) {
					col.g = minZ;
					col.b = maxZ;
				} else if(col.g <= minZ && col.b >= maxZ) {
					//skip
				} else {
					Debug.Log("min:"+minZ+", max:"+maxZ+" not in "+col.a+".."+col.r+" nor "+col.g+".."+col.b+".");
					Debug.Log("Only two discontinuous ranges are supported for overlays at present.");
				}
			}
			boxTex.SetPixel((int)p.x, (int)p.y, col);
		}
		boxTex.Apply(false, true);

		shadeMaterial.SetTexture("_Boxes", boxTex);
		
		Shader highlightShader = Shader.Find("Custom/GridOverlayHighlightSelected");
		selectedHighlightMaterial = new Material(highlightShader);
		selectedHighlightMaterial.SetVector("_MapWorldOrigin", new Vector4(map.transform.position.x, map.transform.position.y, map.transform.position.z, 1) + new Vector4(-map.sideLength/2.0f,0.01f,-map.sideLength/2.0f,0));
		selectedHighlightMaterial.SetVector("_MapTileSize", new Vector4(map.sideLength, map.tileHeight, map.sideLength, 1));
		//FIXME: consider giving selected point a w component for height range.
		selectedHighlightMaterial.SetVector("_SelectedPoint", selectedPoint);
		selectedHighlightMaterial.SetColor("_SelectedColor", selectedColor);
	}
	
	override protected void AddShadeMaterial() {
		MeshRenderer mr = this.gameObject.AddComponent<MeshRenderer>();
		mr.materials = new Material[]{ shadeMaterial, selectedHighlightMaterial };
	}
	

	public bool ContainsPosition(Vector3 hitSpot) {
		return PositionAt(hitSpot) != null;
	}

	public PathNode PositionAt(Vector3 hitSpot) {
		const float ZEpsilon = 0.0015f;
		foreach(Vector4 p in positions) {
			if(p.x == Mathf.Floor(hitSpot.x) &&
			   p.y == Mathf.Floor(hitSpot.y)) {
				if(hitSpot.z >= p.z-ZEpsilon && hitSpot.z <= p.z+p.w+ZEpsilon) {
					foreach(PathNode pn in destinations) {
						if(pn.pos.x == p.x &&
							 pn.pos.y == p.y &&
							 pn.pos.z >= p.z-ZEpsilon &&
							 pn.pos.z <= p.z+p.w+ZEpsilon) {
							return pn;
						}
					}
				}
			}
		}
		return null;	
	}

	public bool Raycast(Ray r, out Vector3 hitSpot) {
		MeshCollider mc = GetComponent<MeshCollider>();
		hitSpot = Vector3.zero;
		if(mc == null) { return false; }
		RaycastHit hit;
		if(mc.Raycast(r, out hit, 1000)) {
			//make sure the normal here is upwards
/*			Debug.Log("NORM:"+hit.normal+", DOT:"+Vector3.Dot(hit.normal, Vector3.up));*/
			if(Vector3.Dot(hit.normal, Vector3.up) > 0.3) {
				hitSpot = map.InverseTransformPointWorld(new Vector3(
					hit.point.x, 
					hit.point.y, 
					hit.point.z
				));
			}
			return true;
		}
		return false;
	}
}
