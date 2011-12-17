using UnityEngine;
using System.Collections;

public class GridOverlay : Overlay {
	public Vector4[] positions;
	public PathNode[] destinations;
	public Color selectedColor;
	
	protected Material selectedHighlightMaterial;
	
	public Vector4[] selectedPoints;
	
	Texture2D indicatorTex, overlayTex;
	
	Color32[] blank;
	
	// Update is called once per frame
	override public void Update() {
		base.Update();
		this.transform.localPosition = new Vector3(0,0.01f,0);
	}
	public void SetSelectedPoints(Vector4[] points) {
		selectedPoints = points;
		if(map == null || selectedColor == Color.clear || selectedHighlightMaterial == null) { return; }
/*		Debug.Log("set sel ct "+points.Length);*/
		selectedHighlightMaterial.SetTexture("_Boxes", BoundsTextureFor(indicatorTex, selectedPoints));
	}
	public void UpdateDestinations(PathNode[] dests) {
		FindMap();
		destinations = dests;
		positions = map.CoalesceTiles(destinations);
		overlayTex = BoundsTextureFor(overlayTex, positions);
		shadeMaterial.SetTexture("_Boxes", overlayTex);	
		//SetSelectedPoints(selectedPoints);
	}
	protected Texture2D BoundsTextureFor(Texture2D inTex, Vector4[] points) {
		if(points == null) { points = new Vector4[0]; }
		int mw = Mathf.NextPowerOfTwo((int)map.size.x);
		int mh = Mathf.NextPowerOfTwo((int)map.size.y);
		Texture2D boxTex = (inTex != null && inTex.width == mw && inTex.height == mh) ? 
			inTex : 
			new Texture2D(
				mw, mh,
				TextureFormat.ARGB32, false
			);
		boxTex.filterMode = FilterMode.Point;
		boxTex.wrapMode = TextureWrapMode.Repeat;
		boxTex.anisoLevel = 1;

		boxTex.SetPixels32(blank);

		//pack overlay points into texture
		for(int i = 0; i < points.Length; i++) {
			Vector4 p = points[i];
			float minZ = p.z/64.0f;
			float maxZ = (p.z+p.w)/64.0f;
			Color col = boxTex.GetPixel((int)p.x, (int)p.y);
			if(col.a == 0 && col.r == 0) {
				col.a = minZ;
				col.r = maxZ;
/*				Debug.Log("set ar pt "+p+":"+minZ+"--"+maxZ);*/
			} else if(col.a <= minZ && col.r >= maxZ) {
				//skip
/*				Debug.Log("A skip pt "+p);*/
			} else {
				if(col.g == 0 && col.b == 0) {
					col.g = minZ;
					col.b = maxZ;
/*					Debug.Log("set gb pt "+p+":"+minZ+"--"+maxZ);*/
				} else if(col.g <= minZ && col.b >= maxZ) {
					//skip
/*					Debug.Log("B skip pt "+p);*/
				} else {
					Debug.Log("min:"+minZ+", max:"+maxZ+" not in "+col.a+".."+col.r+" nor "+col.g+".."+col.b+".");
					Debug.Log("Only two discontinuous ranges are supported for overlays at present.");
				}
			}
			boxTex.SetPixel((int)p.x, (int)p.y, col);
		}
		boxTex.Apply(false, false);
		return boxTex;
	}
	override protected void CreateShadeMaterial () {
		if(map == null) { return; }
		int mw = Mathf.NextPowerOfTwo((int)map.size.x);
		int mh = Mathf.NextPowerOfTwo((int)map.size.y);
		if(blank == null || blank.Length != mw * mh) {
			blank = new Color32[(int)mw*(int)mh];
		}
		if(color != Color.clear) {
			Shader shader = Shader.Find("Custom/GridOverlayClip");
			shadeMaterial = new Material(shader);
			shadeMaterial.SetVector("_MapWorldOrigin", new Vector4(map.transform.position.x, map.transform.position.y, map.transform.position.z, 1) + new Vector4(-map.sideLength/2.0f,0.01f,-map.sideLength/2.0f,0));
			shadeMaterial.SetVector("_MapTileSize", new Vector4(map.sideLength, map.tileHeight, map.sideLength, 1));
			shadeMaterial.SetVector("_MapSizeInTiles", new Vector4(mw, 64, mh, 1));
			shadeMaterial.SetColor("_Color", color);
			UpdateDestinations(destinations);
		}
		if(selectedColor != Color.clear) {
			Shader highlightShader = Shader.Find("Custom/GridOverlayClip");
			selectedHighlightMaterial = new Material(highlightShader);
			selectedHighlightMaterial.SetVector("_MapWorldOrigin", new Vector4(map.transform.position.x, map.transform.position.y, map.transform.position.z, 1) + new Vector4(-map.sideLength/2.0f,0.01f,-map.sideLength/2.0f,0));
			selectedHighlightMaterial.SetVector("_MapTileSize", new Vector4(map.sideLength, map.tileHeight, map.sideLength, 1));
			selectedHighlightMaterial.SetVector("_MapSizeInTiles", new Vector4(mw, 64, mh, 1));
			selectedHighlightMaterial.SetColor("_Color", selectedColor);
			indicatorTex = BoundsTextureFor(indicatorTex, selectedPoints);
			selectedHighlightMaterial.SetTexture("_Boxes", indicatorTex);
		}
	}
	
	override protected void AddShadeMaterial() {
		MeshRenderer mr = this.gameObject.AddComponent<MeshRenderer>();
		mr.materials = new Material[]{ shadeMaterial, selectedHighlightMaterial };
	}
	
	override public PathNode PositionAt(Vector3 hitSpot) {
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
}
