using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEditor;

[System.Serializable]
public class Map : MonoBehaviour {
//  [FlagsAttribute] 
	public enum Neighbors {
		FrontLeftIdx =0,
		FrontRightIdx=1,
		BackRightIdx =2,
		BackLeftIdx  =3,
		BottomIdx    =4,
		TopIdx       =5,
		
		FrontLeft =1<<3, //-x
		FrontRight=1<<4, //-y
		BackRight =1<<5, //+x
		BackLeft  =1<<6, //+y
		Bottom    =1<<7, //-z
		Top       =1<<8, //+z
		
		Sides     =FrontLeft|FrontRight|BackRight|BackLeft,
		All       =Sides|Bottom|Top,
		Any       =All,
		None      =0
	};

	public enum Corners {
		Left  =0, //+ y
		Front =1, //+0
		Right =2, //+x
		Back  =3  //+xy
	};
	
	public bool usesBottomFace=true;
	
	Neighbors NeighborsOfTile(int x, int y, int z) {
		//look to four side edges
		//look above
		//look below
		//FIXME: for tiles of x and y dims > 1
		int zMin=z-1, zMax=z+1;
		MapTile t = TileAt(x,y,z);
		if(t != null) { 
			zMin = t.z-1;
			zMax = t.z+t.maxHeight;
		}
		Neighbors mask = Neighbors.None;
		for(int tz = zMin+1; tz < zMax; tz++) {
			if(x > 0 && x <= _size.x && HasTileAt(x-1, y, tz)) {
				mask = mask | Neighbors.FrontLeft;
			}
			if(y > 0 && y <= _size.y && HasTileAt(x, y-1, tz)) {
				mask = mask | Neighbors.FrontRight;
			}
			if(x >= -1 && x < _size.x-1 && HasTileAt(x+1, y, tz)) {
				mask = mask | Neighbors.BackRight;
			}
			if(y >= -1 && y < _size.y-1 && HasTileAt(x, y+1, tz)) {
				mask = mask | Neighbors.BackLeft;
			}
		}
		if(x >= 0 && x < _size.x && y >= 0 && y < _size.y) {
			if(zMax >= 0 && HasTileAt(x, y, zMax)) {
				mask = mask | Neighbors.Top;
			}
			if(zMin >= 0 && HasTileAt(x, y, zMin)) {
				mask = mask | Neighbors.Bottom;
			}
		}
		return mask;
	}
	
	[SerializeField]
	MapColumn[] stacks;

	[SerializeField]
	List<TileSpec> tileSpecs=new List<TileSpec>();
	
	[SerializeField]
	Texture2D mainAtlas;
	
	[SerializeField]
	Rect[] tileRects;
	
	//Note: dictionaries can't be serialized
	Dictionary<string, Dictionary<int, Overlay>> overlays;
	
	public int TileSpecCount {
		get { return tileSpecs.Count; }
	}
	public void AddTileSpec(Texture2D tex) {
		TileSpec spec = new TileSpec();
		spec.texture = tex;
		tileSpecs.Add(spec);
		RemakeTexture();
	}
	public void RemoveTileSpecAt(int i) {
		for(int mi = 0; mi < stacks.Length; mi++) {
			MapColumn tl = stacks[mi];
			if(tl == null) { continue; }
			for(int ti = 0; ti < tl.Count; i++) {
				MapTile t = tl.At(ti);
				if(!MapTileIsNull(t)) {
					t.AdjustTileSpecsAfterRemoving(i);
				}
			}
		}
		tileSpecs.RemoveAt(i);
		RemakeTexture();
	}
	public TileSpec TileSpecAt(int i) {
		return tileSpecs[i];
	}
	public void UpdateTileSpecAt(int i, Texture2D tex) {
		tileSpecs[i].texture = tex;
		RemakeTexture();
	}
	public void SetTileSpecOnTileAt(int spec, int x, int y, int z, Neighbors sides) {
		MapTile t = TileAt(x,y,z);
		if(!MapTileIsNull(t)) {
			t.SetTileSpecOnSides(spec, sides);
		}
		RemakeMesh();
	}
	
	void RemakeTexture() {
		Texture2D[] textures = new Texture2D[tileSpecs.Count];
		for(int i = 0; i < tileSpecs.Count; i++) {
			if(tileSpecs[i].texture == null) {
				textures[i] = new Texture2D(1, 1);
			} else {
				textures[i] = tileSpecs[i].texture;	
			}
		}
		if(mainAtlas == null) { mainAtlas=new Texture2D(1024, 1024); }
		tileRects = mainAtlas.PackTextures(textures, 0);
		if(!Application.isPlaying) {
			mainAtlas.Compress(true);
//			EditorUtility.CompressTexture(mainAtlas, TextureFormat.DXT5);
		} else {
			mainAtlas.Compress(true);
		}
		RemakeMesh();
	}
	
	[SerializeField]
	Vector2 _size = new Vector2(10,10);
	public Vector2 size {
		get {
			return _size;
		}
		set {
			if(value == _size) { return; }
			Vector2 oldSize = _size;
			_size = value;
			Debug.Log("size reset");
			ResetStacks(oldSize);
		}
	}
	
	[SerializeField]
	float _sideLength = 10;
	public float sideLength {
		get {
			return _sideLength;
		}
		set {
			if(value == _sideLength) { return; }
			_sideLength = value;
			RemakeMesh();
		}
	}
	[SerializeField]
	float _tileHeight = 10;
	public float tileHeight {
		get {
			return _tileHeight;
		}
		set {
			if(value == _tileHeight) { return; }
			_tileHeight = value;
			RemakeMesh();
		}
	}
	void SetTileStackAt(MapTile stack, int x, int y) {
		int idx = y*(int)_size.x+x;
		if(idx >= stacks.Length) {
			Array.Resize(ref stacks, idx+1);
		}
		if(stacks[idx] == null) {
			stacks[idx] = new MapColumn();
		}
		if(stacks[idx].Count > 0) {
			stacks[idx].Clear();
		}
		stacks[idx].Add(stack);
	}
	
	MapColumn TileColumnAt(int x, int y) {
		int idx = y*(int)_size.x+x;
		if(idx >= stacks.Length) {
			return null;
		}
		return stacks[idx];	
	}
	
	MapTile NextTile(MapTile t) {
		MapColumn stack = TileColumnAt(t.x, t.y);
		if(stack == null) { return null; }
		int tidx = stack.IndexOf(t);
		if(tidx == -1) { return null; }
		if(tidx+1 >= stack.Count) {
			return null;
		}
/*		Debug.Log("next after "+t+" at "+tidx+" is "+stack.At(tidx+1)+" where count is "+stack.Count);*/
		return stack.At(tidx+1);
	}
	
	void SetNextTile(MapTile prev, MapTile next) {
		int idx = prev.y*(int)_size.x+prev.x;
		if(idx >= stacks.Length) {
			return;
		}
		MapColumn stack = stacks[idx];
		if(stack == null) { return; }
		int tidx = stack.IndexOf(prev);
		if(tidx == -1) { return; }
		stack.Insert(tidx, next);
	}
	
	public void AddIsoTileAt(int x, int y, int z) {
		if(stacks == null) {
			Debug.Log("add reset");
			ResetStacks(Vector2.zero);
		}
		MapColumn stackC = TileColumnAt(x,y);
		MapTile stack=null;
		bool added = false;
		if(stackC == null || stackC.Count == 0) {
			stack = new MapTile(x, y, z);
			stack.serializeHackUsable = true;
			SetTileStackAt(stack, x, y);
			added = true;
		} else {
			MapTile newStack = new MapTile(x, y, z);
			newStack.serializeHackUsable = true;
			bool present = false;
			for(int i = 0; i < stackC.Count; i++) {
				stack = stackC.At(i);
				if(stack.IsAboveZ(z)) {
					stackC.Insert(i, newStack);
					present = true;
					added = true;
					break;
				} else if(stack.ContainsZ(z)) {
					present = true;
					break;
				}
			}
			if(!present) {
				if(stack.maxZ == z && stack.maxHeight != 1) {
					//don't propagate heights, just clobber the old ones
					int max = stack.maxHeight;
					//stack.next.heights = stack.heights;
					//stack.heights = new int[]{1,1,1,1};
					stack.heights = new int[]{max, max, max, max};
				}
				stackC.Add(newStack);
				added = true;
			}
		}
		if(added) {
			RemakeMesh();
		}
	}

	public void RemoveIsoTileAt(int x, int y, int z) {
		if(stacks == null) {
			//no tile to remove
			return;
		}
		MapColumn stackC = TileColumnAt(x,y);
		MapTile stack;
		if(stackC == null || stackC.Count == 0) {
			//no tile to remove
			return;
		}
		bool removed = false;
		for(int i = 0; i < stackC.Count; i++) { 
			stack = stackC.At(i);
			if(stack.IsAboveZ(z)) { return; }
			if(stack.ContainsZ(z)) {
				stackC.RemoveAt(i);
				removed = true;
				break;
			}
		}
		if(removed) {
			RemakeMesh();
		}
	}
	
	bool MapTileIsNull(MapTile t) {
		return t == null || !t.serializeHackUsable;
	}
	
	void Awake() {
		MeshRenderer mr = GetComponent<MeshRenderer>();
		if(mr != null && mr.materials.Length >= 2) {
			if(Application.isPlaying) {
				mr.materials[1].color = Color.clear;
			}
		}
	}
	
	void UVMap(MapTile t, Neighbors side, Vector2[] uvs, int idx) {
		if(uvs == null) { return; }
		int specIdx;
		if(t.tileSpecs == null || t.tileSpecs.Length == 0) { specIdx = -1; }
		else {
			switch(side) {
				case Neighbors.FrontLeft:  specIdx = t.tileSpecs[0]; break;
				case Neighbors.FrontRight: specIdx = t.tileSpecs[1]; break;
				case Neighbors.BackRight:  specIdx = t.tileSpecs[2]; break;
				case Neighbors.BackLeft:   specIdx = t.tileSpecs[3]; break;
				case Neighbors.Bottom:     specIdx = t.tileSpecs[4]; break;
				case Neighbors.Top:        specIdx = t.tileSpecs[5]; break;
				default:                   specIdx = -1;             break;
			}
		}
		if(specIdx == -1 || specIdx >= tileSpecs.Count || tileSpecs[specIdx] == null || tileSpecs[specIdx].texture == null) {
			uvs[idx+0] = new Vector2(0,0);
			uvs[idx+1] = new Vector2(0,0);
			uvs[idx+2] = new Vector2(0,0);
			uvs[idx+3] = new Vector2(0,0);
		}	else {
/*			TileSpec spec = tileSpecs[specIdx];*/
			Rect rect = tileRects[specIdx];
			Vector2 ul = new Vector2(rect.x, rect.y);
			Vector2 ll = new Vector2(rect.x, rect.yMax);
			Vector2 ur = new Vector2(rect.xMax, rect.y);
			Vector2 lr = new Vector2(rect.xMax, rect.yMax);
			if(side == Neighbors.FrontLeft) {
				uvs[idx+0] = ll;
				uvs[idx+1] = lr;
				uvs[idx+2] = ul;
				uvs[idx+3] = ur;
			} else if(side == Neighbors.BackLeft) {
				uvs[idx+0] = lr;
				uvs[idx+1] = ur;
				uvs[idx+2] = ll;
				uvs[idx+3] = ul;
			}	else if(side == Neighbors.BackRight) {
				uvs[idx+0] = ur;
				uvs[idx+1] = ul;
				uvs[idx+2] = lr;
				uvs[idx+3] = ll;
			} else if(side == Neighbors.Bottom) {
				uvs[idx+0] = lr;
				uvs[idx+1] = ur;
				uvs[idx+2] = ll;
				uvs[idx+3] = ul;
			} else { //works for front-right and top
				uvs[idx+0] = ul;
				uvs[idx+1] = ll;
				uvs[idx+2] = ur;
				uvs[idx+3] = lr;
			}
		}
	}
	
	bool NoInsetOrInvisibleNeighbors(int x, int y, MapTile t) {
		int zMin=t.z-1, zMax=t.z+t.maxHeight;
		MapTile neighbor=null;
		for(int tz = zMin+1; tz < zMax; tz++) {
			if(x > 0 && x <= _size.x && ((neighbor = TileAt(x-1, y, tz)) != null) && (!neighbor.noInsets || neighbor.invisible)) {
				return false;
			}
			if(y > 0 && y <= _size.y && ((neighbor = TileAt(x, y-1, tz)) != null) && (!neighbor.noInsets || neighbor.invisible)) {
				return false;
			}
			if(x >= -1 && x < _size.x-1 &&  ((neighbor = TileAt(x+1, y, tz)) != null) && (!neighbor.noInsets || neighbor.invisible)) {
				return false;
			}
			if(y >= -1 && y < _size.y-1 &&  ((neighbor = TileAt(x, y+1, tz)) != null) && (!neighbor.noInsets || neighbor.invisible)) {
				return false;
			}
		}
		if(x >= 0 && x < _size.x && y >= 0 && y < _size.y) {
			if(zMax >= 0 && ((neighbor = TileAt(x, y, zMax)) != null) && (!neighbor.noInsets || neighbor.invisible)) {
				return false;
			}
			if(zMin >= 0 && ((neighbor = TileAt(x, y, zMin)) != null) && (!neighbor.noInsets || neighbor.invisible)) {
				return false;
			}
		}
		return true;
	}
	
	void RemakeMesh() {
		if(stacks == null) { return; }
		
		MeshFilter mf = GetComponent<MeshFilter>();
		if(mf == null) {
			mf = gameObject.AddComponent<MeshFilter>();
		}
		MeshRenderer mr = GetComponent<MeshRenderer>();
		if(mr == null) {
			mr = gameObject.AddComponent<MeshRenderer>();
		}
		MeshCollider mc = GetComponent<MeshCollider>();
		if(mc == null) {
			mc = gameObject.AddComponent<MeshCollider>();
		}
		
		if(mr.sharedMaterials.Length < 2 || mr.sharedMaterials[0] == null || mr.sharedMaterials[1] == null) {
			mr.sharedMaterials = new Material[]{
				new Material(Shader.Find("Transparent/Cutout/Diffuse")),
				new Material(Shader.Find("Transparent/Diffuse"))
			};
			mr.sharedMaterials[1].color = Application.isPlaying ? Color.clear : new Color(0.7f, 0.7f, 1.0f, 0.5f);
		}
		if(mr.sharedMaterials[0].mainTexture != mainAtlas) {
			mr.sharedMaterials[0].mainTexture = mainAtlas;	
		}
		if(mr.sharedMaterials[1].mainTexture != mainAtlas) {
			mr.sharedMaterials[1].mainTexture = mainAtlas;	
		}
		Mesh mesh = mf.sharedMesh != null ? mf.sharedMesh : new Mesh();
		mesh.Clear();
		
		float height = _tileHeight;
		
		//FIXME: height assumption may be higher than necessary
		//24 vertices per so each gets a uv, we will say 20 units high
		Vector3[] vertices = new Vector3[(int)(_size.x*_size.y*20*24)]; 
		//10 tris, 3 indices per, we will say 20 units high
		int[] opaqueTriangles = new int[(int)(_size.x*_size.y*20*10*3)];
		Vector2[] uvs = new Vector2[vertices.Length];
		
		int vertIdx = 0;
		int opaqueTriIdx = 0;

		//10 tris, 3 indices per, we will say 20 units high
		int[] transparentTriangles = new int[(int)(_size.x*_size.y*20*10*3)];
		
		int transparentTriIdx = 0;
				
		for(int i = 0; i < stacks.Length; i++) {
			MapColumn tlist = stacks[i];
			if(tlist == null) { continue; }
			int y = i/(int)_size.x;
			int x = i-(y*(int)_size.x);
			for(int ti = 0; ti < tlist.Count; ti++) {
				MapTile t = tlist.At(ti);
				if(MapTileIsNull(t)) { Debug.Log("tile "+t+" at "+ti+" is null somehow"); }
				int z = t.z;
				int[] triangles = t.invisible ? transparentTriangles : opaqueTriangles;
				int triIdx = t.invisible ? transparentTriIdx : opaqueTriIdx;
				
				bool avoidNeighbors = t.maxHeight == 1 && t.noInsets && !t.invisible && NoInsetOrInvisibleNeighbors(x, y, t);
				float lx = (x+0+t.sideInsets[(int)Neighbors.FrontLeftIdx ])*_sideLength-_sideLength/2;
				float fx = (x+0+t.sideInsets[(int)Neighbors.FrontLeftIdx ])*_sideLength-_sideLength/2;
				float rx = (x+1-t.sideInsets[(int)Neighbors.BackRightIdx])*_sideLength-_sideLength/2;
				float bx = (x+1-t.sideInsets[(int)Neighbors.BackRightIdx])*_sideLength-_sideLength/2;
				float fy = (y+0+t.sideInsets[(int)Neighbors.FrontRightIdx])*_sideLength-_sideLength/2;
				float ry = (y+0+t.sideInsets[(int)Neighbors.FrontRightIdx])*_sideLength-_sideLength/2;
				float ly = (y+1-t.sideInsets[(int)Neighbors.BackLeftIdx ])*_sideLength-_sideLength/2;
				float by = (y+1-t.sideInsets[(int)Neighbors.BackLeftIdx ])*_sideLength-_sideLength/2;
				
				//TODO: include corner insets and their extra geometry
				

				//TODO: stairs and their extra geometry

				float zMinL = (z+0+t.sideInsets[(int)Neighbors.BottomIdx]+t.baselines[(int)Corners.Left]-1)*height;
				float zMaxL = (z-t.sideInsets[(int)Neighbors.TopIdx]+t.heights[(int)Corners.Left]-1)*height;
				float zMinF = (z+0+t.sideInsets[(int)Neighbors.BottomIdx]+t.baselines[(int)Corners.Front]-1)*height;
				float zMaxF = (z-t.sideInsets[(int)Neighbors.TopIdx]+t.heights[(int)Corners.Front]-1)*height;
				float zMinB = (z+0+t.sideInsets[(int)Neighbors.BottomIdx]+t.baselines[(int)Corners.Back]-1)*height;
				float zMaxB = (z-t.sideInsets[(int)Neighbors.TopIdx]+t.heights[(int)Corners.Back]-1)*height;
				float zMinR = (z+0+t.sideInsets[(int)Neighbors.BottomIdx]+t.baselines[(int)Corners.Right]-1)*height;
				float zMaxR = (z-t.sideInsets[(int)Neighbors.TopIdx]+t.heights[(int)Corners.Right]-1)*height;
				Vector3 bl = new Vector3(lx, zMinL, ly);
				Vector3 bf = new Vector3(fx, zMinF, fy);
				Vector3 bb = new Vector3(bx, zMinB, by);
				Vector3 br = new Vector3(rx, zMinR, ry);
				Vector3 tl = new Vector3(lx, zMaxL, ly);
				Vector3 tf = new Vector3(fx, zMaxF, fy);
				Vector3 tb = new Vector3(bx, zMaxB, by);
				Vector3 tr = new Vector3(rx, zMaxR, ry);
				Neighbors mask = NeighborsOfTile(x, y, z);
				if((mask & Neighbors.Top) == 0 || !avoidNeighbors) {
					vertices[vertIdx+0] = tf; //5
					vertices[vertIdx+1] = tl; //4
					vertices[vertIdx+2] = tr; //7
					vertices[vertIdx+3] = tb; //6
					UVMap(t, Neighbors.Top, uvs, vertIdx);
        
					triangles[triIdx+0*3+0] = vertIdx+0;
					triangles[triIdx+0*3+1] = vertIdx+1;
					triangles[triIdx+0*3+2] = vertIdx+2;
					triangles[triIdx+1*3+0] = vertIdx+2;
					triangles[triIdx+1*3+1] = vertIdx+1;
					triangles[triIdx+1*3+2] = vertIdx+3;
					triIdx += 2*3;
					vertIdx += 4;
				}
				if((mask & Neighbors.BackLeft) == 0 || !avoidNeighbors) {
					vertices[vertIdx+0] = tl; //4
					vertices[vertIdx+1] = bl; //0
					vertices[vertIdx+2] = tb; //6
					vertices[vertIdx+3] = bb; //2
					UVMap(t, Neighbors.BackLeft, uvs, vertIdx);
					
					triangles[triIdx+0*3+0] = vertIdx+0;
					triangles[triIdx+0*3+1] = vertIdx+1;
					triangles[triIdx+0*3+2] = vertIdx+2;
					triangles[triIdx+1*3+0] = vertIdx+2;
					triangles[triIdx+1*3+1] = vertIdx+1;
					triangles[triIdx+1*3+2] = vertIdx+3;
					triIdx += 2*3;
					vertIdx += 4;
				}
				if((mask & Neighbors.FrontRight) == 0 || !avoidNeighbors) {
					vertices[vertIdx+0] = bf; //1
					vertices[vertIdx+1] = tf; //5
					vertices[vertIdx+2] = br; //3
					vertices[vertIdx+3] = tr; //7
					UVMap(t, Neighbors.FrontRight, uvs, vertIdx);

					triangles[triIdx+0*3+0] = vertIdx+0;
					triangles[triIdx+0*3+1] = vertIdx+1;
					triangles[triIdx+0*3+2] = vertIdx+2;
					triangles[triIdx+1*3+0] = vertIdx+2;
					triangles[triIdx+1*3+1] = vertIdx+1;
					triangles[triIdx+1*3+2] = vertIdx+3;
					triIdx += 2*3;
					vertIdx += 4;
				}
				if((mask & Neighbors.FrontLeft) == 0 || !avoidNeighbors) {
					vertices[vertIdx+0] = tl; //4
					vertices[vertIdx+1] = tf; //5
					vertices[vertIdx+2] = bl; //0
					vertices[vertIdx+3] = bf; //1
					UVMap(t, Neighbors.FrontLeft, uvs, vertIdx);

					triangles[triIdx+0*3+0] = vertIdx+0;
					triangles[triIdx+0*3+1] = vertIdx+1;
					triangles[triIdx+0*3+2] = vertIdx+2;
					triangles[triIdx+1*3+0] = vertIdx+2;
					triangles[triIdx+1*3+1] = vertIdx+1;
					triangles[triIdx+1*3+2] = vertIdx+3;
					triIdx += 2*3;
					vertIdx += 4;
				}
				if((mask & Neighbors.BackRight) == 0 || !avoidNeighbors) {
					vertices[vertIdx+0] = bb; //2
					vertices[vertIdx+1] = br; //3
					vertices[vertIdx+2] = tb; //6
					vertices[vertIdx+3] = tr; //7
					UVMap(t, Neighbors.BackRight, uvs, vertIdx);

					triangles[triIdx+0*3+0] = vertIdx+0;
					triangles[triIdx+0*3+1] = vertIdx+1;
					triangles[triIdx+0*3+2] = vertIdx+2;
					triangles[triIdx+1*3+0] = vertIdx+2;
					triangles[triIdx+1*3+1] = vertIdx+1;
					triangles[triIdx+1*3+2] = vertIdx+3;
					triIdx += 2*3;
					vertIdx += 4;
				}
				if(usesBottomFace && (((mask & Neighbors.Bottom) == 0) || !avoidNeighbors)) {
					vertices[vertIdx+0] = bl; //0
					vertices[vertIdx+1] = bf; //1
					vertices[vertIdx+2] = bb; //2
					vertices[vertIdx+3] = br; //3
					UVMap(t, Neighbors.Bottom, uvs, vertIdx);

					triangles[triIdx+0*3+0] = vertIdx+0;
					triangles[triIdx+0*3+1] = vertIdx+1;
					triangles[triIdx+0*3+2] = vertIdx+2;
					triangles[triIdx+1*3+0] = vertIdx+2;
					triangles[triIdx+1*3+1] = vertIdx+1;
					triangles[triIdx+1*3+2] = vertIdx+3;
					triIdx += 2*3;
					vertIdx += 4;
				}
				if(t.invisible) { transparentTriIdx = triIdx; }
				else { opaqueTriIdx = triIdx; }
			}
		}
		Array.Resize<Vector3>(ref vertices, vertIdx);
		Array.Resize<Vector2>(ref uvs, vertIdx);
		Array.Resize<int>(ref opaqueTriangles, opaqueTriIdx);
		Array.Resize<int>(ref transparentTriangles, transparentTriIdx);

		mesh.vertices = vertices;
		mesh.uv = uvs;
		mesh.subMeshCount = 2;
		mesh.SetTriangles(opaqueTriangles, 0);
		mesh.SetTriangles(transparentTriangles, 1);
		mesh.RecalculateNormals();
		mesh.Optimize();
		mf.sharedMesh = mesh;
		mc.convex = false;

		InvalidateOverlayMesh();
	}
	
	void ResetStacks(Vector2 oldSize) {
		MapColumn[] newStacks = new MapColumn[(int)_size.x*(int)_size.y];
		Debug.Log("reset");
		for(int i = 0; i < newStacks.Length; i++) {
			if(oldSize != Vector2.zero) {
				if(oldSize == _size) {
					newStacks[i] = stacks[i];
				} else {
					int y = i/(int)_size.x;
					int x = i-y*(int)_size.x;
					int oldI = y*(int)oldSize.x+x;
					if(x >= 0 && x < oldSize.x && x < _size.x &&
					   y >= 0 && y < oldSize.y && y < _size.y &&
						 stacks != null) {
						newStacks[i] = stacks[oldI];
					} else {
						newStacks[i] = null;
					}
				}
			} else {
				newStacks[i] = null;
			}
		}
		this.stacks = newStacks;
		RemakeMesh();
	}
	
	// Use this for initialization
	void Start () {
		if(this.stacks == null) {
			Debug.Log("start reset"); 
			ResetStacks(Vector2.zero);
		}
	}
	
	// Update is called once per frame
	void Update () {
	}
	
	public bool HasTileAt(int x, int y) {
		MapColumn c = TileColumnAt(x,y);
		return x >= 0 && x < size.x && y >= 0 && y < size.y && c != null && c.Count > 0;
	}
	public bool HasTileAt(int x, int y, int z) {
		return !MapTileIsNull(TileAt(x,y,z));
	}

	public int NearestZLevel(int x, int y, int z) {
		MapColumn c = TileColumnAt(x,y);
		int dz = int.MaxValue;
		MapTile closest = null;
		if(c == null || c.Count == 0) { return 0; }
		for(int i = 0; i < c.Count; i++) {
			MapTile s = c.At(i);
			MapTile next = i+1 >= c.Count ? null : c.At(i+1);
			if(next == null || next.z > s.maxZ) {
				int thisDz = (int)Mathf.Abs((s.avgZ) - z);
				if(thisDz < dz) {
					dz = thisDz;
					closest = s;
				}
			}
		}
		return closest.avgZ;
	}
	
	public int NextZLevel(int x, int y, int z, bool wrap=false) {
		MapColumn mc = TileColumnAt(x,y);
		if(mc == null) { return 0; }
		int lowestValidZ = -1;
		int lastValidZ = -1;
		
		for(int i = 0; i < mc.Count; i++) {
			bool valid = false;
			MapTile t = mc.At(i);
			if(MapTileIsNull(t)) { 
				if(wrap) { return lowestValidZ; }
				return lastValidZ;
			} else {
				MapTile n = NextTile(t);
				if(MapTileIsNull(n)) {
					valid = true;
				} else {
					if(n.z > t.maxZ) { valid = true; }
				}
				if(valid) {
					if(lowestValidZ == -1) { lowestValidZ = t.avgZ; }
					lastValidZ = t.avgZ;
					if(lastValidZ > z) { return lastValidZ; }
				}
			}
		}
		return lowestValidZ;
	}
	
	//TODO: include a direction argument for ramps
	public int[] ZLevelsWithin(int x, int y, int z, int range) {
		if(x<0 || y<0 || x >= size.x || y >= size.y) { return new int[0]; }
		MapColumn c = TileColumnAt(x,y);
		if(c == null || c.Count == 0) { return new int[0]; }
		List<int> zLevels = new List<int>();
		for(int i = 0; i < c.Count; i++) {
			MapTile t = c.At(i);
			//skip anybody with a tile immediately above them
			if(i+1 < c.Count && c.At(i+1).z <= t.maxZ) { continue; }
			//skip tiles that are not within range
			if(range < 0 || Mathf.Abs(t.avgZ-z) <= range) { 
				zLevels.Add(t.avgZ); 
			}
		}
		return zLevels.ToArray();
	}
	
	public MapTile TileAt(int x, int y, int z) {
		if(x < 0 || 
		   y < 0 || 
		   x >= _size.x || 
		   y >= _size.y || 
		   z < 0) { 
			return null; 
		}
		if(stacks == null) { return null; }
		MapColumn c = TileColumnAt(x,y);
		if(c == null) { return null; }
		for(int i = 0; i < c.Count; i++) {
			MapTile t = c.At(i);
			if(t.ContainsZ(z)) { return t; }
			if(t.IsAboveZ(z)) { return null; }
		}
		return null;
	}
	
	public void AdjustHeightOnSidesOfTile(int x, int y, int z, int deltaH, Neighbors sides, bool top) {
		MapTile t = TileAt(x,y,z);
		if(t == null) { return; }
		t.AdjustHeightOnSides(deltaH, sides, top, NextTile(t));
		RemakeMesh();
	}
	
	public void InsetCornerOfTile(int x, int y, int z, float inset, Map.Corners corner) {
		MapTile t = TileAt(x,y,z);
		if(t != null) { 
			t.InsetCorner(inset, corner); 
			RemakeMesh();
		}
	}
	
	public void InsetSidesOfTile(int x, int y, int z, float inset, Map.Neighbors mask) {
		MapTile t = TileAt(x,y,z);
		if(t != null) { 
			t.InsetSides(inset, mask); 
			RemakeMesh();
		}
	}
	public void SetTileInvisible(int x, int y, int z, bool invis) {
		MapTile t = TileAt(x,y,z);
		if(t != null) {
			if(t.invisible != invis) {
				t.invisible = invis;
				RemakeMesh();
			}
		}
	}
	
	public Vector2 TransformKeyboardAxes(float h, float v, bool switchXY=true) {
		//use the camera and the map's own rotation
		Transform cam = Camera.main.transform;
		//h*right+v*forward
		Vector3 xp = cam.TransformDirection(new Vector3(1, 0, 0));
		xp.y = 0;
		xp = xp.normalized;
		Vector3 yp = new Vector3(-xp.z, 0, xp.x);
/*		Debug.Log("XP:"+xp+", YP:"+yp+", hxp:"+(h*xp)+", vyp:"+(v*yp)+", h:"+h+", v:"+v);*/
		Vector3 result = h*xp + v*yp;
		if(switchXY) {
			return new Vector2(-result.z, result.x);
		} else {
			return new Vector2(result.x, result.z);
		}
	}
	
	public Vector3 TransformPointLocal(Vector3 tileCoord) {
/*		Debug.Log("Tile: "+tileCoord+" is local "+new Vector3(tileCoord.x*sideLength, tileCoord.z*tileHeight, tileCoord.y*sideLength));*/
		return new Vector3(tileCoord.x*sideLength, tileCoord.z*tileHeight, tileCoord.y*sideLength);
	}
	public Vector3 InverseTransformPointLocal(Vector3 localCoord) {
/*		Debug.Log("Local: "+localCoord+" is "+new Vector3((localCoord.x)/sideLength, (localCoord.z)/sideLength, localCoord.y/tileHeight));*/
		return new Vector3((localCoord.x)/sideLength, (localCoord.z)/sideLength, localCoord.y/tileHeight);
	}
	public Vector3 TransformPointWorld(Vector3 tileCoord) {
		return this.transform.TransformPoint(this.TransformPointLocal(tileCoord));
	}
	public Vector3 InverseTransformPointWorld(Vector3 worldCoord) {
		return this.InverseTransformPointLocal(this.transform.InverseTransformPoint(worldCoord));
	}
	
	public GridOverlay PresentGridOverlay(string category, int id, Color color, Color selectedColor, PathNode[] destinations) {
		if(overlays == null) { overlays = new Dictionary<string, Dictionary<int, Overlay>>(); }
		if(!overlays.ContainsKey(category)) {
			overlays[category] = new Dictionary<int, Overlay>();
		}
		GameObject go = new GameObject();
		go.transform.parent = this.transform;
		GridOverlay ov = go.AddComponent<GridOverlay>();
		ov.destinations = destinations;
		ov.positions = CoalesceTiles(destinations);
		ov.color = color;
		ov.selectedColor = selectedColor;
		ov.category = category;
		ov.identifier = id;
		overlays[category][id] = ov;
		return ov;
	}
	public RadialOverlay PresentSphereOverlay(string category, int id, Color color, Vector3 origin, float radius, bool drawRim=false, bool drawOuterVolume=false, bool invert=false) {
		if(overlays == null) { overlays = new Dictionary<string, Dictionary<int, Overlay>>(); }
		if(!overlays.ContainsKey(category)) {
			overlays[category] = new Dictionary<int, Overlay>();
		}
		GameObject go = new GameObject();
		go.transform.parent = this.transform;
		RadialOverlay ov = go.AddComponent<RadialOverlay>();
		//Q: Is it proper to convert these into world coordinates here? should we expect tile coords instead? hrm hrm
		ov.type = RadialOverlayType.Sphere;
		ov.origin = TransformPointWorld(origin);
		ov.tileRadius = radius;
		ov.radius = radius*sideLength;
		ov.drawRim = drawRim;
		ov.drawOuterVolume = drawOuterVolume;
		ov.invert = invert;
		ov.color = color;
		ov.category = category;
		ov.identifier = id;
		overlays[category][id] = ov;
		return ov;
	}
	public RadialOverlay PresentCylinderOverlay(string category, int id, Color color, Vector3 origin, float radius, float height, bool drawRim=false, bool drawOuterVolume=false, bool invert=false) {
		if(overlays == null) { overlays = new Dictionary<string, Dictionary<int, Overlay>>(); }
		if(!overlays.ContainsKey(category)) {
			overlays[category] = new Dictionary<int, Overlay>();
		}
		GameObject go = new GameObject();
		go.transform.parent = this.transform;
		RadialOverlay ov = go.AddComponent<RadialOverlay>();
		//Q: Is it proper to convert these into world coordinates here? should we expect tile coords instead? hrm hrm
		ov.type = RadialOverlayType.Cylinder;
		ov.origin = TransformPointWorld(origin);
		ov.radius = radius*sideLength;
		ov.tileRadius = radius;
		ov.height = height*tileHeight;
		ov.drawRim = drawRim;
		ov.drawOuterVolume = drawOuterVolume;
		ov.invert = invert;
		ov.color = color;
		ov.category = category;
		ov.identifier = id;
		overlays[category][id] = ov;
		return ov;
	}
	public void RemoveOverlay(string category, int id) {
		if(overlays == null) { return; }
		if(!overlays.ContainsKey(category)) { return; }
		Overlay ov = overlays[category][id];
		if(ov != null) {
/*			Debug.Log("remove overlay "+ov);*/
			overlays[category].Remove(id);
			Destroy(ov.gameObject);
		}
	}
	public bool IsShowingOverlay(string category, int id) {
		if(overlays == null) { return false; }
		return overlays[category].ContainsKey(id);
	}
	bool IsProp(GameObject go) {
		if(go == this.gameObject) { return false; }
		if(go.GetComponent<Prop>() != null) { return true; }
		if(go.transform.parent == null) { return false; }
		return IsProp(go.transform.parent.gameObject);
	}
	Mesh overlayMesh;
	public Mesh OverlayMesh {
		get { 
			if(overlayMesh == null) {
				//caveat: this will only behave properly wrt static meshes, 
				//so be sure you can't stand on parts of props that animate.
				overlayMesh = new Mesh();
		    MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(false);
				int meshesUsed = 0;
				CombineInstance[] combine = new CombineInstance[meshFilters.Length+1];
				combine[meshesUsed].mesh = GetComponent<MeshFilter>().mesh;
				//translate all these matrices by the inverse of the local translation matrix
				combine[meshesUsed].transform = transform.localToWorldMatrix * Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one).inverse;
				meshesUsed++;
				foreach(MeshFilter mf in meshFilters) {
					if(IsProp(mf.gameObject)) {
						combine[meshesUsed].mesh = mf.sharedMesh;
						//translate all these matrices by the inverse of the local translation matrix
						combine[meshesUsed].transform = mf.transform.localToWorldMatrix;
						meshesUsed++;
					}
				}
				overlayMesh.CombineMeshes(combine);
			}
			return overlayMesh;
		}
	}

	public void InvalidateOverlayMesh() {
		overlayMesh = null;
		BroadcastMessage("OverlayMeshInvalidated", null, SendMessageOptions.DontRequireReceiver);
	}
	
	public Scheduler scheduler {
		get { 
			return GetComponentInChildren<Scheduler>();
		}
	}
	public Arbiter arbiter {
		get { 
			return GetComponentInChildren<Arbiter>();
		}
	}
	
	public Vector4[] CoalesceTiles(PathNode[] spots) {
		Vector4[] outputs = new Vector4[spots.Length];
		int count=0;
		foreach(PathNode sp in spots) {
			Vector3 s = sp.pos;
			MapTile t = TileAt((int)s.x, (int)s.y, (int)s.z);
			float w = t.maxHeight;
			bool merged = false;
			for(int i = 0; i < count; i++) {
				Vector4 v4 = outputs[i];
				if(v4.x == s.x && v4.y == s.y) {
					if((v4.z+v4.w >= t.z-1 && v4.z <= t.z-1) ||
					   (v4.z == t.z+w)) {
						outputs[i].z = Mathf.Min(v4.z, t.z);
						outputs[i].w = Mathf.Max(v4.z+v4.w, t.z+w)-outputs[i].z;
						merged = true;
						break;
					}
				}
			}
			if(!merged) {
				outputs[count] = new Vector4(s.x, s.y, t.z, w);
				count++;
			}
		}
		Array.Resize(ref outputs, count);
		return outputs;
	}
	public Character CharacterAt(Vector3 tc) {
		foreach(Character c in GetComponentsInChildren<Character>()) {
			Vector3 ctc = c.TilePosition;
//			Debug.Log("TC:"+tc+", CTC:"+ctc);
			if(Mathf.Floor(tc.x) == Mathf.Floor(ctc.x) &&
			   Mathf.Floor(tc.y) == Mathf.Floor(ctc.y) &&
			   Mathf.Floor(tc.z) == Mathf.Floor(ctc.z)) {
				return c;
			}
		}
		return null;
	}
	public delegate PathDecision PathNodeIsValid(Vector3 start, PathNode pn, Character c);

	readonly Vector2[] neighbors = {
		new Vector2(-1, 0),
		new Vector2( 1, 0),
		new Vector2( 0,-1),
		new Vector2( 0, 1)
	};
	
	//maxDistance is distinct from move in that it's xyz distance
	public PathNode[] PathsAround(
		Vector3 tc, 
		float minRadius, float maxRadius, 
		float zDownMin, float zDownMax, 
		float zUpMin, float zUpMax, 
		bool shouldJump,
		PathNodeIsValid isValid=null
	) {
/*		Color debugColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1);*/
		int x = (int)Mathf.Floor(tc.x), y = (int)Mathf.Floor(tc.y), z = (int)Mathf.Floor(tc.z);
		Stack<PathNode> open = new Stack<PathNode>();
		List<PathNode> closed = new List<PathNode>();
		List<PathNode> nodes = new List<PathNode>();
		PathNode startNode = new PathNode(new Vector3(x,y,z), null, 0);
		open.Push(startNode);
		MapTile t = TileAt(x,y,z);
		if(MapTileIsNull(t)) { return new PathNode[0]; }
		while(open.Count > 0) {
			PathNode pn = open.Pop();
			if(pn.distance > maxRadius) {
				//this shouldn't be here
				closed.Add(pn);
				continue;
			}
			if(!closed.Contains(pn)) {
				closed.Add(pn);
				nodes.Add(pn);
			}
			if(pn.distance == maxRadius) {
				//don't bother adding any more points, they'll be too far
				continue;
			}
			for(int i = 0; i < neighbors.Length; i++) {
				Vector2 n = neighbors[i];
				int jumpDistance = zDownMax > 0 ? (int)(zDownMax/2) : 0;
				Vector2 adj = new Vector2(pn.pos.x+n.x, pn.pos.y+n.y);
				//push to open (if not yet there) all tiles at adj.x, adj.y whose .maxZ is within zDownMax of adj.z.
				//TODO: fix people being able to walk through the floor!
				foreach(int adjZ in ZLevelsWithin((int)adj.x, (int)adj.y, (int)pn.pos.z, -1)) {
					Vector3 pos = new Vector3(adj.x, adj.y, adjZ);
					float signedDZ = SignedDZForMove(pos, pn.pos);
					float adz = Mathf.Abs(signedDZ);
					PathNode newPn = new PathNode(pos, pn, pn.distance+1+0.01f*adz);
					if(!closed.Contains(newPn) && !open.Contains(newPn)) {
						PathDecision decision = isValid == null ? PathDecision.Normal : isValid(tc, newPn, CharacterAt(pos));
						if(decision == PathDecision.PassOnly) {
							newPn.canStop = false;
						}
/*						Debug.DrawLine(TransformPointWorld(pn.position)+new Vector3(0,12,0), TransformPointWorld(pos)+new Vector3(0,12,0), debugColor, 10.0f);*/
						//can't jump over things, can only jump across things
						if(shouldJump && pos.z < pn.pos.z) {
							//FIXME: duplication
							//go out further in adj until we get past jump/2
							for(int j = 0; j < jumpDistance; j++) {
								//don't go further than our move would allow
								if(pn.distance+2+j > maxRadius) { continue; }
								Vector2 jumpAdj = new Vector2(pn.pos.x+n.x*(j+2), pn.pos.y+n.y*(j+2));
								bool jumped = false, cannotJump = false;
								foreach(int jumpAdjZ in ZLevelsWithin((int)jumpAdj.x, (int)jumpAdj.y, (int)pn.pos.z, -1)) {
									Vector3 jumpPos = new Vector3(jumpAdj.x, jumpAdj.y, jumpAdjZ);
									float jumpDZ = AbsDZForMove(jumpPos, pn.pos);
									//TODO: decide whether we can only cross like this downwards, or if up is also allowed
									if(jumpDZ <= zDownMax) {
										PathNode jumpPn = new PathNode(jumpPos, pn, pn.distance+2+j+0.01f*jumpDZ);
										jumpPn.isLeap = true;
										PathDecision jumpDecision = isValid == null ? PathDecision.Normal : isValid(tc, jumpPn, CharacterAt(jumpPos));
										if(jumpDecision == PathDecision.PassOnly) {
											jumpPn.canStop = false;
										}
										if((jumpDecision != PathDecision.Invalid)) {
											open.Push(jumpPn);
											jumped = true;
										}
									} else if(jumpAdjZ > pn.pos.z) { //don't jump through a wall
										cannotJump = true;
									}
								}
								if(jumped || cannotJump) { break; }
							}
						} 
						bool heightOK = (signedDZ < 0 ? 
							signedDZ > zDownMax : 
							(signedDZ > 0 ? 
								signedDZ < zUpMax : 
								decision == PathDecision.Normal));
/*						Debug.Log("Height OK? pos:"+newPn.pos+", dz:"+signedDZ+", down:"+zDownMin+".."+zDownMax+", up:"+zUpMin+".."+zUpMax+", decision:"+decision+": "+heightOK);*/
						if((decision != PathDecision.Invalid) && heightOK) {
/*							Debug.DrawLine(TransformPointWorld(pn.position)+new Vector3(2, 12, 0), TransformPointWorld(pos)+new Vector3(2, 12, 0), debugColor, 10.0f);*/
							open.Push(newPn);
						}
					}
				}
			}
		}
		return nodes.Where(n => 
			n.xyDistance >= minRadius && 
			(n.signedDZ < 0 ? n.signedDZ <= -zDownMin : (n.signedDZ > 0 ? n.signedDZ >= zUpMin : true))).ToArray();
	}
	public Neighbors EnteringSideFromXYDelta(float dx, float dy) {
		if(dx > 0) { return Neighbors.FrontLeftIdx; }
		if(dx < 0) { return Neighbors.BackRightIdx; }
		if(dy > 0) { return Neighbors.FrontRightIdx; }
		if(dy < 0) { return Neighbors.BackLeftIdx; }
/*		Debug.LogError("entering side uses weird deltas "+dx+","+dy);*/
		return Neighbors.None;
	}
	public Neighbors ExitingSideFromXYDelta(float dx, float dy) {
		if(dx > 0) { return Neighbors.BackRightIdx; }
		if(dx < 0) { return Neighbors.FrontLeftIdx; }
		if(dy > 0) { return Neighbors.BackLeftIdx; }
		if(dy < 0) { return Neighbors.FrontRightIdx; }
/*		Debug.LogError("exiting side uses weird deltas "+dx+","+dy);*/
		return Neighbors.None;
	}
	public float SignedDZForMove(Vector3 to, Vector3 from) {
		float dx = to.x-from.x;
		float dy = to.y-from.y;
		MapTile toTile = TileAt((int)to.x, (int)to.y, (int)to.z);
		Neighbors entering = EnteringSideFromXYDelta(dx, dy);
		MapTile fromTile = TileAt((int)from.x, (int)from.y, (int)from.z);
		Neighbors exiting = ExitingSideFromXYDelta(dx, dy);
		if(fromTile == null || toTile == null) { return float.MaxValue; }
				
		float xz = (fromTile.LowestHeightAt(exiting)+fromTile.HighestHeightAt(exiting))/2.0f;
		float ez = (toTile.LowestHeightAt(entering)+toTile.HighestHeightAt(entering))/2.0f;
		return ez-xz;
	}
	public float AbsDZForMove(Vector3 to, Vector3 from) {
		return Mathf.Abs(SignedDZForMove(to, from));
	}
}

public enum PathDecision {
	Invalid,
	PassOnly,
	Normal
};

[System.Serializable]
public class PathNode {
	public Vector3 pos;
	public PathNode prev=null;
	public float distance;
	public bool canStop=true;
	public bool isLeap=false;
	public PathNode(Vector3 ps, PathNode pr, float dist) {
		pos = ps; prev = pr; distance = dist;
	}
	public Vector3 position { get { return pos; } }
	public int dz {
		get { return (int)Mathf.Abs(signedDZ); }
	}
	public int signedDZ {
		get { return (int)(pos.z - (prev != null ? prev.pos.z : pos.z)); }
	}
	public float xyDistance {
		get { return (int)(Mathf.Abs(pos.x - (prev != null ? prev.pos.x : pos.x))+Mathf.Abs(pos.y - (prev != null ? prev.pos.y : pos.y))); }
	}
	public override bool Equals(object obj)
  {	
    if (obj is PathNode)
    {
      return this.Equals((PathNode)obj);
    }
    return false;
  }

  public bool Equals(PathNode p)
  {
    return p != null && pos == p.pos;
  }

  public override int GetHashCode()
  {
    return pos.GetHashCode();
  }
};
