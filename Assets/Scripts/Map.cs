using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;

[System.Serializable]
public class TileSpec {
	public Texture2D texture;
}

[System.Serializable]
public class MapTile {
	public MapTile next=null;
	public int z=0;
	//indexed in same order as Map.Corners: l, f, r, b
	public int[] heights = {1,1,1,1};
	public int[] baselines = {0,0,0,0};
	//these should range from 0 to 0.5. They can be used for things like corners of walls.
	public float[] cornerInsets = {0,0,0,0};
	//indexed in same order as Map.Neighbors: fl, fr, br, bl, b, t
	public int[] tileSpecs = {-1,-1,-1,-1,-1,-1};
	//these should range from 0 to 0.5. They can be used for walls or thin bridges.
	public float[] sideInsets = {0,0,0,0,0,0};
	
	public bool invisible=false;
	
	/*
	More about insets:
	 Side insets and corner insets are combined at mesh generation time.
	 Side insets shift a pair of corners inwards.
	 Corner insets shift an individual corner inwards, generating more triangles.
	*/
	
	//unity creates empty instances of these guys in place of nulls, so we need this hack
	public bool serializeHackUsable=false;
	public MapTile(int z) {
		this.z = z;
		this.invisible = false;
		this.tileSpecs = new int[]{-1, -1, -1, -1, -1, -1};
		this.heights = new int[]{1,1,1,1};
		this.baselines = new int[]{0,0,0,0};
		this.cornerInsets = new float[]{0,0,0,0};
		this.sideInsets = new float[]{0,0,0,0,0,0};
	}
	public void AdjustTileSpecsAfterRemoving(int i) {
		for(int ti = 0; ti < tileSpecs.Length; ti++) {
			if(tileSpecs[ti] > i) {
				tileSpecs[ti]--;
			}
		}
	}
	public void SetTileSpecOnSides(int spec, Map.Neighbors mask) {
		if(tileSpecs == null || tileSpecs.Length == 0) {
			tileSpecs = new int[]{-1, -1, -1, -1, -1, -1};
		}
		if((mask & Map.Neighbors.FrontLeft) != 0) {
			tileSpecs[0] = spec;
		}
		if((mask & Map.Neighbors.FrontRight) != 0) {
			tileSpecs[1] = spec;
		}
		if((mask & Map.Neighbors.BackRight) != 0) {
			tileSpecs[2] = spec;
		}
		if((mask & Map.Neighbors.BackLeft) != 0) {
			tileSpecs[3] = spec;
		}
		if((mask & Map.Neighbors.Bottom) != 0) {
			tileSpecs[4] = spec;
		}
		if((mask & Map.Neighbors.Top) != 0) {
			tileSpecs[5] = spec;
		}
	}
	public void AdjustHeightOnSides(int ht, Map.Neighbors mask, bool top) {
		if(top && (heights == null || heights.Length == 0)) {
			heights = new int[]{1, 1, 1, 1};
		}
		if(!top && (baselines == null || baselines.Length == 0)) {
			baselines = new int[]{0, 0, 0, 0};
		}
		int hts = ht > 0 ? 1 : -1;
		while(ht != 0) {
			int hcl = heights[(int)Map.Corners.Left];
			int hcf = heights[(int)Map.Corners.Front];
			int hcr = heights[(int)Map.Corners.Right];
			int hcb = heights[(int)Map.Corners.Back];
			int bcl = baselines[(int)Map.Corners.Left];
			int bcf = baselines[(int)Map.Corners.Front];
			int bcr = baselines[(int)Map.Corners.Right];
			int bcb = baselines[(int)Map.Corners.Back];
			int aboveTileZIfAny = next != null ? next.z : int.MaxValue;
			if(top) {
				//raise rules: if corners are different heights, only raise the lower one; 
				//             don't raise a corner if it will cause a collision with a tile above
				//lower rules: if corners are different heights, only lower the higher one; 
				//             don't lower a height to or below its corresponding baseline
				if((mask & Map.Neighbors.FrontLeft) != 0) {
					if(hts > 0 ? (hcl <= hcf && z+hcl < aboveTileZIfAny) : (hcl >= hcf && hcl > bcl+1)) {
						heights[(int)Map.Corners.Left]  += hts;
					}
					if(hts > 0 ? (hcf <= hcl && z+hcf < aboveTileZIfAny) : (hcf >= hcl && hcf > bcf+1)) {
						heights[(int)Map.Corners.Front] += hts;
					}
				}
				if((mask & Map.Neighbors.FrontRight) != 0) {
					if(hts > 0 ? (hcr <= hcf && z+hcr < aboveTileZIfAny) : (hcr >= hcf && hcr > bcr+1)) {
						heights[(int)Map.Corners.Right] += hts;
					}
					if(hts > 0 ? (hcf <= hcr && z+hcf < aboveTileZIfAny) : (hcf >= hcr && hcf > bcf+1)) {
						heights[(int)Map.Corners.Front] += hts;
					}
				}
				if((mask & Map.Neighbors.BackRight) != 0) {
					if(hts > 0 ? (hcr <= hcb && z+hcr < aboveTileZIfAny) : (hcr >= hcb && hcr > bcr+1)) {
						heights[(int)Map.Corners.Right] += hts;
					}
					if(hts > 0 ? (hcb <= hcr && z+hcb < aboveTileZIfAny) : (hcb >= hcr && hcb > bcb+1)) {
						heights[(int)Map.Corners.Back] += hts;
					}
				}
				if((mask & Map.Neighbors.BackLeft) != 0) {
					if(hts > 0 ? (hcl <= hcb && z+hcl < aboveTileZIfAny) : (hcl >= hcb && hcl > bcl+1)) {
						heights[(int)Map.Corners.Left] += hts;
					}
					if(hts > 0 ? (hcb <= hcl && z+hcb < aboveTileZIfAny) : (hcb >= hcl && hcb > bcb+1)) {
						heights[(int)Map.Corners.Back] += hts;
					}
				}
			} else {				
				//raise rules: if corners are different heights, only raise the lower one;
				//             don't raise a baseline to or above its corresponding height;
				//             while all corners are raised above 0, decrement all corners by 1 and increase z by 1.
				//lower rules: if corners are different heights, only lower the higher one; 
				//             don't lower a corner below 0
				//             (later: don't lower a corner below 0 if there is a tile below;)
				//             (later: while any corner is below 0, increment all corners by 1 and decrease z by 1)
				if((mask & Map.Neighbors.FrontLeft) != 0) {
					if(hts > 0 ? (bcl <= bcf && bcl+1<hcl) : (bcl >= bcf && bcl > 0)) {
						baselines[(int)Map.Corners.Left]  += hts;
					}
					if(hts > 0 ? (bcf <= bcl && bcf+1<hcf) : (bcf >= bcl && bcf > 0)) {
						baselines[(int)Map.Corners.Front] += hts;
					}
				}
				if((mask & Map.Neighbors.FrontRight) != 0) {
					if(hts > 0 ? (bcr <= bcf && bcr+1<hcr): (bcr >= bcf && bcr > 0)) {
						baselines[(int)Map.Corners.Right] += hts;
					}
					if(hts > 0 ? (bcf <= bcr && bcf+1<hcf) : (bcf >= bcr && bcf > 0)) {
						baselines[(int)Map.Corners.Front] += hts;
					}
				}
				if((mask & Map.Neighbors.BackRight) != 0) {
					if(hts > 0 ? (bcr <= bcb && bcr+1<hcr) : (bcr >= bcb && bcr > 0)) {
						baselines[(int)Map.Corners.Right] += hts;
					}
					if(hts > 0 ? (bcb <= bcr && bcb+1<hcb) : (bcb >= bcr && bcb > 0)) {
						baselines[(int)Map.Corners.Back] += hts;
					}
				}
				if((mask & Map.Neighbors.BackLeft) != 0) {
					if(hts > 0 ? (bcl <= bcb && bcl+1<hcl) : (bcl >= bcb && bcl > 0)) {
						baselines[(int)Map.Corners.Left] += hts;
					}
					if(hts > 0 ? (bcb <= bcl && bcb+1<hcb) : (bcb >= bcl && bcb > 0)) {
						baselines[(int)Map.Corners.Back] += hts;
					}
				}
				while(baselines[(int)Map.Corners.Left] > 0 &&
							baselines[(int)Map.Corners.Front] > 0 &&
							baselines[(int)Map.Corners.Right] > 0 &&
							baselines[(int)Map.Corners.Back] > 0) {
					baselines[(int)Map.Corners.Left]--;
					baselines[(int)Map.Corners.Front]--;
					baselines[(int)Map.Corners.Right]--;
					baselines[(int)Map.Corners.Back]--;
					z++;
				}
			}

			ht -= hts;
		}
	}
	public bool noInsets {
		get { 
			for(int i = 0; i < cornerInsets.Length; i++) {
				if(cornerInsets[i] != 0) { return false; }
			}
			for(int i = 0; i < sideInsets.Length; i++) {
				if(sideInsets[i] != 0) { return false; }
			}
			return true;
		}
	}
	
	public void InsetCorner(float inset, Map.Corners corner) {
		if(cornerInsets == null || cornerInsets.Length == 0) {
			cornerInsets = new float[]{0,0,0,0};
		}
		if(corner == Map.Corners.Left ) {
			cornerInsets[(int)Map.Corners.Left ] = inset;
		}
		if(corner == Map.Corners.Front) {
			cornerInsets[(int)Map.Corners.Front] = inset;
		}
		if(corner == Map.Corners.Right) {
			cornerInsets[(int)Map.Corners.Right] = inset;
		}
		if(corner == Map.Corners.Back ) {
			cornerInsets[(int)Map.Corners.Back ] = inset;
		}
	}
	
	public void InsetSides(float inset, Map.Neighbors mask) {
		if(sideInsets == null || sideInsets.Length == 0) {
			sideInsets = new float[]{0,0,0,0,0,0};
		}
		if((mask & Map.Neighbors.FrontLeft) != 0) {
			sideInsets[0] = inset;
		}
		if((mask & Map.Neighbors.FrontRight) != 0) {
			sideInsets[1] = inset;
		}
		if((mask & Map.Neighbors.BackRight) != 0) {
			sideInsets[2] = inset;
		}
		if((mask & Map.Neighbors.BackLeft) != 0) {
			sideInsets[3] = inset;
		}
		if((mask & Map.Neighbors.Bottom) != 0) {
			sideInsets[4] = inset;
		}
		if((mask & Map.Neighbors.Top) != 0) {
			sideInsets[5] = inset;
		}
	}
	public bool IsAboveZ(int zed) {
		return this.z > zed;
	}
	public int maxHeight {
		get { return Math.Max(heights[(int)Map.Corners.Left], 
												  Math.Max(heights[(int)Map.Corners.Front], 
															     Math.Max(heights[(int)Map.Corners.Right], 
																            heights[(int)Map.Corners.Left]))); }
	}
	public int maxZ {
		get { return z+maxHeight; }
	}
	public bool ContainsZ(int zed) {
		//note <, not <=!
		return zed >= this.z && zed < (this.z+this.maxHeight);
	}
}

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
	
	public bool HasTileAt(int x, int y, int z) {
		return !MapTileIsNull(TileAt(x,y,z));
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
		return new Vector3(tileCoord.x*sideLength-sideLength/2, tileCoord.z*tileHeight-tileHeight/2, tileCoord.y*sideLength-sideLength/2);
	}
	public Vector3 InverseTransformPointLocal(Vector3 localCoord) {
		return new Vector3((localCoord.x+sideLength/2)/sideLength, (localCoord.z+sideLength/2)/sideLength, localCoord.y);
	}
	public Vector3 TransformPointWorld(Vector3 tileCoord) {
		return this.transform.TransformPoint(this.TransformPointLocal(tileCoord));
	}
	public Vector3 InverseTransformPointWorld(Vector3 localCoord) {
		return this.InverseTransformPointLocal(this.transform.InverseTransformPoint(localCoord));
	}
}
