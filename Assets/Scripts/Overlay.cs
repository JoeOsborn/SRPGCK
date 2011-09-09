using UnityEngine;
using System.Collections;

public class Overlay : MonoBehaviour {
	public Color color = Color.clear;
	public Vector4[] positions;
	public string category;
	public int identifier;
	
	Material shadeMaterial;
	
	Map map;
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		this.transform.localPosition = new Vector3(0,0.01f,0);
		if(map == null) {
			if(this.transform.parent != null) {
				map = this.transform.parent.GetComponent<Map>();
			}
			if(map == null) { 
				Debug.Log("Overlay must be child of a map");
				return; 
			}
		}
		if(shadeMaterial == null) {
			if(color == Color.clear) {
				return;
			}
			Shader shader = Shader.Find("Custom/OverlayClip");
			shadeMaterial = new Material(shader);
			//set shadeMaterial properties
			shadeMaterial.SetVector("_MapWorldOrigin", new Vector4(map.transform.position.x, map.transform.position.y, map.transform.position.z, 1));
			shadeMaterial.SetVector("_MapTileSize", new Vector4(map.sideLength, map.sideLength, map.tileHeight, 1));
			int mw = Mathf.NextPowerOfTwo((int)map.size.x);
			int mh = Mathf.NextPowerOfTwo((int)map.size.y);
			shadeMaterial.SetVector("_MapSizeInTiles", new Vector4(mw, 64, mh, 1));
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
		}
		if(this.renderer == null && shadeMaterial != null) {
			//add a mesh renderer
			MeshRenderer mr = this.gameObject.AddComponent<MeshRenderer>();
			MeshFilter mf = this.gameObject.AddComponent<MeshFilter>();
			mf.mesh = map.OverlayMesh;
			mr.material = shadeMaterial;
		}
	}	
}
