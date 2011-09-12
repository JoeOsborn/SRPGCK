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
		get { 
			if(heights == null || heights.Length == 0) { return 1; }
			return Math.Max(heights[(int)Map.Corners.Left], 
											Math.Max(heights[(int)Map.Corners.Front], 
											     	   Math.Max(heights[(int)Map.Corners.Right], 
														            heights[(int)Map.Corners.Back]))); 
    }
	}
	public int maxZ {
		get { return z+maxHeight; }
	}
	public bool ContainsZ(int zed) {
		//note <, not <=!
		return zed >= this.z && zed < (this.z+this.maxHeight);
	}
}
