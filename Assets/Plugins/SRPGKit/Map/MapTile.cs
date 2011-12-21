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
	public int x=0,y=0,z=0;
	//indexed in same order as Corners: l, f, r, b
	public int[] heights = {1,1,1,1};
	public int[] baselines = {0,0,0,0};
	//these should range from 0 to 0.5. They can be used for things like corners of walls.
	public float[] cornerInsets = {0,0,0,0};
	//indexed in same order as Neighbors: fl, fr, br, bl, b, t
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
	public MapTile(int x, int y, int z) {
		this.x = x;
		this.y = y;
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
	public void SetTileSpecOnSides(int spec, Neighbors mask) {
		if(tileSpecs == null || tileSpecs.Length == 0) {
			tileSpecs = new int[]{-1, -1, -1, -1, -1, -1};
		}
		if((mask & Neighbors.FrontLeft) != 0) {
			tileSpecs[0] = spec;
		}
		if((mask & Neighbors.FrontRight) != 0) {
			tileSpecs[1] = spec;
		}
		if((mask & Neighbors.BackRight) != 0) {
			tileSpecs[2] = spec;
		}
		if((mask & Neighbors.BackLeft) != 0) {
			tileSpecs[3] = spec;
		}
		if((mask & Neighbors.Bottom) != 0) {
			tileSpecs[4] = spec;
		}
		if((mask & Neighbors.Top) != 0) {
			tileSpecs[5] = spec;
		}
	}
	public float LowestHeightAt(Neighbors side) {
		int hcl = heights[(int)Corners.Left];
		int hcf = heights[(int)Corners.Front];
		int hcr = heights[(int)Corners.Right];
		int hcb = heights[(int)Corners.Back];
		switch(side) {
			case Neighbors.FrontLeftIdx:
				return Mathf.Min(hcl, hcf);
			case Neighbors.FrontRightIdx:
				return Mathf.Min(hcr, hcf);
			case Neighbors.BackLeftIdx:
				return Mathf.Min(hcl, hcb);
			case Neighbors.BackRightIdx:
				return Mathf.Min(hcr, hcb);
			default: 
				Debug.LogError("given neighbor is not a side edge: "+side);
				return this.z;
		}
	}
	public float HighestHeightAt(Neighbors side) {
		int hcl = heights[(int)Corners.Left];
		int hcf = heights[(int)Corners.Front];
		int hcr = heights[(int)Corners.Right];
		int hcb = heights[(int)Corners.Back];
		switch(side) {
			case Neighbors.FrontLeftIdx:
				return Mathf.Max(hcl, hcf);
			case Neighbors.FrontRightIdx:
				return Mathf.Max(hcr, hcf);
			case Neighbors.BackLeftIdx:
				return Mathf.Max(hcl, hcb);
			case Neighbors.BackRightIdx:
				return Mathf.Max(hcr, hcb);
			default: 
				Debug.LogError("given neighbor is not a side edge: "+side);
				return this.maxZ;
		}
	}
	public void AdjustHeightOnSides(int ht, Neighbors mask, bool top, MapTile next) {
		if(top && (heights == null || heights.Length == 0)) {
			heights = new int[]{1, 1, 1, 1};
		}
		if(!top && (baselines == null || baselines.Length == 0)) {
			baselines = new int[]{0, 0, 0, 0};
		}
		int hts = ht > 0 ? 1 : -1;
		while(ht != 0) {
			int hcl = heights[(int)Corners.Left];
			int hcf = heights[(int)Corners.Front];
			int hcr = heights[(int)Corners.Right];
			int hcb = heights[(int)Corners.Back];
			int bcl = baselines[(int)Corners.Left];
			int bcf = baselines[(int)Corners.Front];
			int bcr = baselines[(int)Corners.Right];
			int bcb = baselines[(int)Corners.Back];
			int aboveTileZIfAny = next != null ? next.z : int.MaxValue;
			if(top) {
				//raise rules: if corners are different heights, only raise the lower one; 
				//             don't raise a corner if it will cause a collision with a tile above
				//lower rules: if corners are different heights, only lower the higher one; 
				//             don't lower a height to or below its corresponding baseline
				if((mask & Neighbors.FrontLeft) != 0) {
					if(hts > 0 ? (hcl <= hcf && z+hcl < aboveTileZIfAny) : (hcl >= hcf && hcl > bcl+1)) {
						heights[(int)Corners.Left]  += hts;
					}
					if(hts > 0 ? (hcf <= hcl && z+hcf < aboveTileZIfAny) : (hcf >= hcl && hcf > bcf+1)) {
						heights[(int)Corners.Front] += hts;
					}
				}
				if((mask & Neighbors.FrontRight) != 0) {
					if(hts > 0 ? (hcr <= hcf && z+hcr < aboveTileZIfAny) : (hcr >= hcf && hcr > bcr+1)) {
						heights[(int)Corners.Right] += hts;
					}
					if(hts > 0 ? (hcf <= hcr && z+hcf < aboveTileZIfAny) : (hcf >= hcr && hcf > bcf+1)) {
						heights[(int)Corners.Front] += hts;
					}
				}
				if((mask & Neighbors.BackRight) != 0) {
					if(hts > 0 ? (hcr <= hcb && z+hcr < aboveTileZIfAny) : (hcr >= hcb && hcr > bcr+1)) {
						heights[(int)Corners.Right] += hts;
					}
					if(hts > 0 ? (hcb <= hcr && z+hcb < aboveTileZIfAny) : (hcb >= hcr && hcb > bcb+1)) {
						heights[(int)Corners.Back] += hts;
					}
				}
				if((mask & Neighbors.BackLeft) != 0) {
					if(hts > 0 ? (hcl <= hcb && z+hcl < aboveTileZIfAny) : (hcl >= hcb && hcl > bcl+1)) {
						heights[(int)Corners.Left] += hts;
					}
					if(hts > 0 ? (hcb <= hcl && z+hcb < aboveTileZIfAny) : (hcb >= hcl && hcb > bcb+1)) {
						heights[(int)Corners.Back] += hts;
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
				if((mask & Neighbors.FrontLeft) != 0) {
					if(hts > 0 ? (bcl <= bcf && bcl+1<hcl) : (bcl >= bcf && bcl > 0)) {
						baselines[(int)Corners.Left]  += hts;
					}
					if(hts > 0 ? (bcf <= bcl && bcf+1<hcf) : (bcf >= bcl && bcf > 0)) {
						baselines[(int)Corners.Front] += hts;
					}
				}
				if((mask & Neighbors.FrontRight) != 0) {
					if(hts > 0 ? (bcr <= bcf && bcr+1<hcr): (bcr >= bcf && bcr > 0)) {
						baselines[(int)Corners.Right] += hts;
					}
					if(hts > 0 ? (bcf <= bcr && bcf+1<hcf) : (bcf >= bcr && bcf > 0)) {
						baselines[(int)Corners.Front] += hts;
					}
				}
				if((mask & Neighbors.BackRight) != 0) {
					if(hts > 0 ? (bcr <= bcb && bcr+1<hcr) : (bcr >= bcb && bcr > 0)) {
						baselines[(int)Corners.Right] += hts;
					}
					if(hts > 0 ? (bcb <= bcr && bcb+1<hcb) : (bcb >= bcr && bcb > 0)) {
						baselines[(int)Corners.Back] += hts;
					}
				}
				if((mask & Neighbors.BackLeft) != 0) {
					if(hts > 0 ? (bcl <= bcb && bcl+1<hcl) : (bcl >= bcb && bcl > 0)) {
						baselines[(int)Corners.Left] += hts;
					}
					if(hts > 0 ? (bcb <= bcl && bcb+1<hcb) : (bcb >= bcl && bcb > 0)) {
						baselines[(int)Corners.Back] += hts;
					}
				}
				while(baselines[(int)Corners.Left] > 0 &&
							baselines[(int)Corners.Front] > 0 &&
							baselines[(int)Corners.Right] > 0 &&
							baselines[(int)Corners.Back] > 0) {
					baselines[(int)Corners.Left]--;
					baselines[(int)Corners.Front]--;
					baselines[(int)Corners.Right]--;
					baselines[(int)Corners.Back]--;
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
	
	public void InsetCorner(float inset, Corners corner) {
		if(cornerInsets == null || cornerInsets.Length == 0) {
			cornerInsets = new float[]{0,0,0,0};
		}
		if(corner == Corners.Left ) {
			cornerInsets[(int)Corners.Left ] = inset;
		}
		if(corner == Corners.Front) {
			cornerInsets[(int)Corners.Front] = inset;
		}
		if(corner == Corners.Right) {
			cornerInsets[(int)Corners.Right] = inset;
		}
		if(corner == Corners.Back ) {
			cornerInsets[(int)Corners.Back ] = inset;
		}
	}
	
	public void InsetSides(float inset, Neighbors mask) {
		if(sideInsets == null || sideInsets.Length == 0) {
			sideInsets = new float[]{0,0,0,0,0,0};
		}
		if((mask & Neighbors.FrontLeft) != 0) {
			sideInsets[0] = inset;
		}
		if((mask & Neighbors.FrontRight) != 0) {
			sideInsets[1] = inset;
		}
		if((mask & Neighbors.BackRight) != 0) {
			sideInsets[2] = inset;
		}
		if((mask & Neighbors.BackLeft) != 0) {
			sideInsets[3] = inset;
		}
		if((mask & Neighbors.Bottom) != 0) {
			sideInsets[4] = inset;
		}
		if((mask & Neighbors.Top) != 0) {
			sideInsets[5] = inset;
		}
	}
	public bool IsAboveZ(int zed) {
		return this.z > zed;
	}
	public int maxHeight {
		get { 
			if(heights == null || heights.Length == 0) { return 1; }
			return Math.Max(heights[(int)Corners.Left], 
											Math.Max(heights[(int)Corners.Front], 
											     	   Math.Max(heights[(int)Corners.Right], 
														            heights[(int)Corners.Back]))); 
    }
	}
	public int maxZ {
		get { return z+maxHeight; }
	}
	public int avgZ { 
		get { return (int)(z+maxHeight/2.0f); }
	}
	public bool ContainsZ(int zed) {
		//note <, not <=!
		return zed >= this.z && zed < (this.z+this.maxHeight);
	}
}

[System.Serializable]
public class MapColumn {
	[SerializeField]
	List<MapTile> tiles;
	
	public MapColumn() {
		tiles = new List<MapTile>();
	}
	
	public MapTile At(int i) {
		return tiles[i];
	}
	public void RemoveAt(int i) {
		tiles.RemoveAt(i);
	}
	public int Count {
		get { return tiles.Count; }
	}
	public void Clear() {
		tiles.Clear();
	}
	public void Add(MapTile t) {
		tiles.Add(t);
	}
	public void Insert(int idx, MapTile t) {
		tiles.Insert(idx, t);
	}
	public int IndexOf(MapTile t) {
		return tiles.IndexOf(t);
	}
}