using UnityEngine;
using System.Collections.Generic;
using System;
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
	MapTile[] stacks;

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
			MapTile t = stacks[mi];
			if(!MapTileIsNull(t)) {
				t.AdjustTileSpecsAfterRemoving(i);
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
	MapTile TileStackAt(int x, int y) {
		int idx = y*(int)_size.x+x;
		if(idx >= stacks.Length) {
			return null;
		}
		return stacks[idx];
	}
	void SetTileStackAt(MapTile stack, int x, int y) {
		int idx = y*(int)_size.x+x;
		if(idx >= stacks.Length) {
			Array.Resize(ref stacks, idx+1);
		}
		stacks[idx] = stack;
	}
	
	public void AddIsoTileAt(int x, int y, int z) {
		if(stacks == null) {
			ResetStacks(Vector2.zero);
		}
		MapTile stack = TileStackAt(x,y);
		if(MapTileIsNull(stack)) {
			stack = new MapTile(z);
			stack.serializeHackUsable = true;
			SetTileStackAt(stack, x, y);
		}
		if(stack.IsAboveZ(z)) {
			MapTile newStack = new MapTile(z);
			newStack.serializeHackUsable = true;
			SetTileStackAt(newStack, x, y);
			newStack.next = stack;
		} else if(!stack.ContainsZ(z)) {
			if(MapTileIsNull(stack.next)) {
				stack.next = new MapTile(z);
				stack.next.serializeHackUsable = true;
				if(stack.maxZ == z && stack.maxHeight != 1) {
					//don't propagate heights, just clobber the old ones
					int max = stack.maxHeight;
					//stack.next.heights = stack.heights;
					//stack.heights = new int[]{1,1,1,1};
					stack.heights = new int[]{max, max, max, max};
				}
			} else {
				MapTile prev = stack, n;
				while(!MapTileIsNull(n = prev.next)) {
					if(n.IsAboveZ(z)) {
						prev.next = new MapTile(z);
						prev.next.serializeHackUsable = true;
						if(prev.maxZ == z && prev.maxHeight != 1) {
							//don't propagate heights, just clobber the old ones
							int max = prev.maxHeight;
							//prev.next.heights = stack.heights;
							//prev.heights = new int[]{1,1,1,1};
							prev.heights = new int[]{max,max,max,max};
						}
						prev.next.next = n;
						break;
					} else if(n.ContainsZ(z)) {
						break;
					}
					prev = n;
				}
				if(MapTileIsNull(n)) {
					prev.next = new MapTile(z);
					prev.next.serializeHackUsable = true;
					if(prev.maxZ == z && prev.maxHeight != 1) {
						//don't propagate heights, just clobber the old ones
						int max = prev.maxHeight;
						//prev.next.heights = prev.heights;
						//prev.heights = new int[]{1,1,1,1};
						prev.heights = new int[]{max,max,max,max};
					}
				}
			}
		} else {
			//cannot add on an existing tile
		}
		RemakeMesh();
	}

	public void RemoveIsoTileAt(int x, int y, int z) {
		if(stacks == null) {
			//no tile to remove
			return;
		}
		MapTile stack = TileStackAt(x,y);
		if(MapTileIsNull(stack)) {
			//no tile to remove
			return;
		}
		if(stack.IsAboveZ(z)) {
			//no tile to remove
			return;
		} else if(!stack.ContainsZ(z)) {
			if(MapTileIsNull(stack.next)) {
				//no tile to remove
				return;
			} else {
				MapTile prev = stack, n;
				while(!MapTileIsNull(n = prev.next)) {
					if(n.IsAboveZ(z)) {
						//no tile to remove
						return;
					} else if(n.ContainsZ(z)) {
						//cut this tile out of the chain
						prev.next = n.next;
						//don't propagate heights
						/*if(n.maxHeight > 1 && prev.maxZ == n.z) {
							prev.heights = n.heights;
						}*/
						break;
					}
					prev = n;
				}
			}
		} else {
			//remove this tile
			if(!MapTileIsNull(stack.next)) {
				SetTileStackAt(stack.next, x, y);
			} else {
				MapTile dummyStack = new MapTile(-1);
				dummyStack.serializeHackUsable = false;
				SetTileStackAt(dummyStack, x, y);
			}
		}
		RemakeMesh();
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
			MapTile t = stacks[i];
			if(MapTileIsNull(t)) { continue; }
			int y = i/(int)_size.x;
			int x = i-(y*(int)_size.x);
			while(!MapTileIsNull(t)) {
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
				t = t.next;
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
	}
	
	void ResetStacks(Vector2 oldSize) {
		MapTile[] newStacks = new MapTile[(int)_size.x*(int)_size.y];
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
			ResetStacks(Vector2.zero);
		}
	}
	
	// Update is called once per frame
	void Update () {
	}
	
	public bool HasTileAt(int x, int y) {
		return x >= 0 && x < size.x && y >= 0 && y < size.y && !MapTileIsNull(TileStackAt(x,y));
	}
	public bool HasTileAt(int x, int y, int z) {
		return !MapTileIsNull(TileAt(x,y,z));
	}

	public int NearestZLevel(int x, int y, int z) {
		MapTile t = TileAt(x,y,z);
		if(!MapTileIsNull(t)) { return t.maxZ-1; }
		t = TileStackAt(x,y);
		if(MapTileIsNull(t)) { return z; }
 		float closestDistance=Mathf.Infinity;
		MapTile closestTile = t;
		while(!MapTileIsNull(t) && !MapTileIsNull(t.next)) {
			float dist = Mathf.Abs(t.maxZ-z);
			if(dist < closestDistance) {
				closestTile = t;
			}
			t = t.next;
		}
		return closestTile.maxZ-1;
	}
	
	public int NextZLevel(int x, int y, int z, bool wrap=false) {
		MapTile t = TileAt(x,y,z);
		if(!MapTileIsNull(t)) { 
			if(MapTileIsNull(t.next) && wrap) { return TileStackAt(x,y).maxZ-1; } 
			if(!MapTileIsNull(t.next)) { return t.next.maxZ-1; }
			if(MapTileIsNull(t.next)) { return t.maxZ-1; }
		}
		t = TileStackAt(x,y);
		//return the first one whose maxZ exceeds z, or the bottom of the pile if wrapping.
		while(!MapTileIsNull(t)) {
			if(t.maxZ-1 >= z) { return t.maxZ-1; }
			t = t.next;
		}
		if(MapTileIsNull(t) && wrap) { 
			t = TileStackAt(x,y); 
			if(!MapTileIsNull(t)) { 
				return t.maxZ-1; 
			} 
		}
		return z;
	}
	
	public int[] ZLevelsWithin(int x, int y, int z, int range) {
		if(x<0 || y<0 || x >= size.x || y >= size.y) { return new int[0]; }
		MapTile t = TileStackAt(x,y);
		if(MapTileIsNull(t)) { return new int[0]; }
		List<int> zLevels = new List<int>();
		while(!MapTileIsNull(t)) {
			if(t.next != null && (t.next.z <= t.maxZ)) { t = t.next; continue; }
			if((range<0) || Mathf.Abs(t.maxZ-1-z) <= range) { zLevels.Add(t.maxZ-1); }
			t = t.next;
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
		MapTile stack = TileStackAt(x, y);
		if(MapTileIsNull(stack)) { return null; }
		if(stack.ContainsZ(z)) { return stack; }
		if(stack.IsAboveZ(z)) { return null; }
		MapTile n = stack;
		while(!MapTileIsNull(n = n.next)) {
			if(n.ContainsZ(z)) { return n; }
			if(n.IsAboveZ(z)) { return null; }
		}
		return null;
	}
	
	public void AdjustHeightOnSidesOfTile(int x, int y, int z, int deltaH, Neighbors sides, bool top) {
		MapTile t = TileAt(x,y,z);
		if(t == null) { return; }
		t.AdjustHeightOnSides(deltaH, sides, top);
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
	
	public Vector3 TransformPointLocal(Vector3 tileCoord) {
/*		Debug.Log("Tile: "+tileCoord+" is local "+new Vector3(tileCoord.x*sideLength-sideLength/2, tileCoord.z*tileHeight, tileCoord.y*sideLength-sideLength/2));*/
		return new Vector3(tileCoord.x*sideLength-sideLength/2, tileCoord.z*tileHeight, tileCoord.y*sideLength-sideLength/2);
	}
	public Vector3 InverseTransformPointLocal(Vector3 localCoord) {
/*		Debug.Log("Local: "+localCoord+" is "+new Vector3((localCoord.x+sideLength/2)/sideLength, (localCoord.z+sideLength/2)/sideLength, localCoord.y/tileHeight));*/
		return new Vector3((localCoord.x+sideLength/2)/sideLength, (localCoord.z+sideLength/2)/sideLength, localCoord.y/tileHeight);
	}
	public Vector3 TransformPointWorld(Vector3 tileCoord) {
		return this.transform.TransformPoint(this.TransformPointLocal(tileCoord));
	}
	public Vector3 InverseTransformPointWorld(Vector3 worldCoord) {
		return this.InverseTransformPointLocal(this.transform.InverseTransformPoint(worldCoord));
	}
	
	public GridOverlay PresentGridOverlay(string category, int id, Color color, PathNode[] destinations) {
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
				combine[meshesUsed].transform = transform.localToWorldMatrix;
				meshesUsed++;
				foreach(MeshFilter mf in meshFilters) {
					if(IsProp(mf.gameObject)) {
						combine[meshesUsed].mesh = mf.sharedMesh;
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
	}
	
	public Scheduler scheduler {
		get { 
			return GetComponentInChildren<Scheduler>();
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
			Vector3 ctc = InverseTransformPointWorld(c.transform.position-new Vector3(0,5,0));
//			Debug.Log("TC:"+tc+", CTC:"+ctc);
			if(Mathf.Floor(tc.x) == Mathf.Floor(ctc.x) &&
			   Mathf.Floor(tc.y) == Mathf.Floor(ctc.y) &&
			   Mathf.Floor(tc.z) == Mathf.Floor(ctc.z)) {
				return c;
			}
		}
		return null;
	}
	public delegate PathDecision PathNodeIsValid(PathNode pn, Character c);
	//maxDistance is distinct from move in that it's xyz distance
	public PathNode[] PathsAround(Vector3 tc, int move, int jump, PathNodeIsValid isValid=null) {
		int x = (int)Mathf.Floor(tc.x), y = (int)Mathf.Floor(tc.y), z = (int)Mathf.Floor(tc.z);
		Vector2[] neighbors = new Vector2[]{
			new Vector2(-1, 0),
			new Vector2( 1, 0),
			new Vector2( 0,-1),
			new Vector2( 0, 1)
		};
		Stack<PathNode> open = new Stack<PathNode>();
		List<PathNode> closed = new List<PathNode>();
		List<PathNode> nodes = new List<PathNode>();
		open.Push(new PathNode(new Vector3(x,y,z), new Vector3(x,y,z), 0));
		MapTile t = TileAt(x,y,z);
		if(MapTileIsNull(t)) { return new PathNode[0]; }
		while(open.Count > 0) {
			PathNode pn = open.Pop();
			if(pn.distance > move) {
				//this shouldn't be here
				closed.Add(pn);
				continue;
			}
			if(!closed.Contains(pn)) {
				closed.Add(pn);
				nodes.Add(pn);
//				Debug.Log("push pt "+pn.pos);
			}
			if(pn.distance == move) {
				//don't bother adding any more points, they'll be too far
				continue;
			}
			for(int i = 0; i < neighbors.Length; i++) {
				Vector2 n = neighbors[i];
				Vector2 adj = new Vector3(pn.pos.x+n.x, pn.pos.y+n.y);
				//push to open (if not yet there) all tiles at adj.x, adj.y whose .maxZ is within jump of adj.z.
//				Debug.Log("push adj "+adj);
				foreach(int adjZ in ZLevelsWithin((int)adj.x, (int)adj.y, (int)pn.pos.z, -1)) {
					Vector3 pos = new Vector3(adj.x, adj.y, adjZ);
					PathNode newPn = new PathNode(pos, pn.pos, pn.distance+1);
					if(!closed.Contains(newPn) && !open.Contains(newPn)) {
						PathDecision decision = isValid == null ? PathDecision.Normal : isValid(newPn, CharacterAt(pos));
						if(decision == PathDecision.PassOnly) {
							newPn.canStop = false;
						}
						bool heightOK = (decision != PathDecision.Normal || jump < 0) ? true : newPn.dz <= jump;
						if((decision != PathDecision.Invalid) && heightOK) {
							open.Push(newPn);
						}
					}
				}
			}
		}
		return nodes.ToArray();
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
	public Vector3 prev;
	public int distance;
	public bool canStop=true;
	public PathNode(Vector3 ps, Vector3 pr, int dist) {
		pos = ps; prev = pr; distance = dist;
	}
	public int dz {
		get { return (int)Mathf.Abs(pos.z - prev.z); }
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
