using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum RegionType {
	Cylinder,
	Sphere,
	Cone,
	Line,
	Self,
	Predicate,
	Compound
};

public enum InterveningSpaceType {
	Pick, //pick anywhere in 3d space
	Path, //walkable path
	Line, //straight line from source, possibly blocked by walls or enemies
	Arc   //arced line from source, possibly blocked by walls or enemies
};

[System.Serializable]
public class Region {
	[System.NonSerialized]
	[HideInInspector]
	public Skill owner;

	public RegionType type=RegionType.Cylinder;

	//projectiles are not pathable, they go out in a direction;
	//movement is pathable, it can go forward, turn, etc.
	//TODO: put pick/path/line/arc stuff into map.pathsaround
	public InterveningSpaceType interveningSpaceType=InterveningSpaceType.Pick;
	
	public bool useArcRangeBonus=false;
	
	//these mean the same for pathable and non-pathable regions
	//they don't apply to self, predicate, or compound.
	public bool canCrossWalls=true;
	public bool canCrossEnemies=true;

	//these apply to cylinder, sphere, cone, and line
	public Formula radiusMinF, radiusMaxF;
	//these apply to cylinder (define), sphere (clip), and line (define up/down height)
	public Formula zUpMinF, zUpMaxF, zDownMinF, zDownMaxF;
	//these apply to cone
	public Formula xyDirectionF, xyArcF;
	//these apply to cone
	public Formula zDirectionF, zArcF;

	public float radiusMin { get { return radiusMinF.GetValue(owner, null, null); } }
	public float radiusMax { get { return radiusMaxF.GetValue(owner, null, null); } }
	public float zUpMin { get { return zUpMinF.GetValue(owner, null, null); } }
	public float zUpMax { get { return zUpMaxF.GetValue(owner, null, null); } }
	public float zDownMin { get { return zDownMinF.GetValue(owner, null, null); } }
	public float zDownMax { get { return zDownMaxF.GetValue(owner, null, null); } }

	public float xyDirection { get { return xyDirectionF.GetValue(owner, null, null); } }
	public float xyArc { get { return xyArcF.GetValue(owner, null, null); } }
	public float zDirection { get { return zDirectionF.GetValue(owner, null, null); } }
	public float zArc { get { return zArcF.GetValue(owner, null, null); } }

	protected Map map { get { return owner.map; } }

	public delegate PathDecision PathNodeIsValid(Vector3 start, PathNode pn, Character c);

	readonly Vector2[] XYNeighbors = {
		new Vector2(-1, 0),
		new Vector2( 1, 0),
		new Vector2( 0,-1),
		new Vector2( 0, 1)
	};

/*	readonly Vector3[] XYZNeighbors = {
		new Vector3(-1,-1, 0),
		new Vector3( 0,-1, 0),
		new Vector3( 1,-1, 0),
		new Vector3(-1, 0, 0),
		new Vector3( 1, 0, 0),
		new Vector3(-1, 1, 0),
		new Vector3( 0, 1, 0),
		new Vector3( 1, 1, 0),

		new Vector3(-1,-1, 1),
		new Vector3( 0,-1, 1),
		new Vector3( 1,-1, 1),
		new Vector3(-1, 0, 1),
		new Vector3( 1, 0, 1),
		new Vector3(-1, 1, 1),
		new Vector3( 0, 1, 1),
		new Vector3( 1, 1, 1),

		new Vector3(-1,-1,-1),
		new Vector3( 0,-1,-1),
		new Vector3( 1,-1,-1),
		new Vector3(-1, 0,-1),
		new Vector3( 1, 0,-1),
		new Vector3(-1, 1,-1),
		new Vector3( 0, 1,-1),
		new Vector3( 1, 1,-1)
	};*/

	public Region() {
		type = RegionType.Cylinder;
		interveningSpaceType = InterveningSpaceType.Pick;
		radiusMinF = Formula.Constant(0);
		radiusMaxF = Formula.Constant(0);
		zUpMinF = Formula.Constant(0);
		zUpMaxF = Formula.Constant(0);
		zDownMinF = Formula.Constant(0);
		zDownMaxF = Formula.Constant(0);
		xyDirectionF = Formula.Constant(0);
		xyArcF = Formula.Constant(0);
		zDirectionF = Formula.Constant(0);
		zArcF = Formula.Constant(0);
	}

	public bool isEffectRegion=false;
	public bool IsEffectRegion {
		get { return isEffectRegion; }
		set { isEffectRegion = value; }
	}
	
	//FIXME: wrong because of reliance on z{Up|Down}{Max|Min}
	public virtual PathDecision PathNodeIsValidHack(Vector3 start, PathNode pn, Character c) {
		float dz = pn.position.z - start.z;
		float absDZ = Mathf.Abs(dz);
		if(c != null && c != owner.character) {
			if(c.EffectiveTeamID != owner.character.EffectiveTeamID) {
				return (IsEffectRegion ? PathDecision.Normal : PathDecision.PassOnly);
			} else {
				if(!IsEffectRegion) {
					return PathDecision.PassOnly;
				}
			}
		}
		if(IsEffectRegion) {
			if(dz < 0 ? (absDZ > zUpMax) :
					(dz > 0 ? absDZ > zDownMax : false)) {
				return PathDecision.PassOnly;
			}
		} else {
			if((dz == 0 ?
				(zDownMin != 0 && zUpMin != 0) :
					(dz < 0 ? (dz > -zDownMin || dz <= -zDownMax) :
					(dz < zUpMin || dz >= zUpMax)))) {
				return PathDecision.PassOnly;
			}
		}
		return PathDecision.Normal;
	}
	
	//FIXME: wrong because of reliance on z{Up|Down}{Max|Min}
	public virtual PathDecision PathNodeIsValidRange(Vector3 start, PathNode pn, Character c) {
		float dz = pn.position.z - start.z;
		float absDZ = Mathf.Abs(dz);
		//TODO: replace with some type of collision check?
		if(c != null && c != owner.character) {
			if(c.EffectiveTeamID != owner.character.EffectiveTeamID) {
				return canCrossEnemies ?
					(IsEffectRegion ? PathDecision.Normal : PathDecision.PassOnly) :
					PathDecision.Invalid;
			} else {
				if(!IsEffectRegion) {
					return PathDecision.PassOnly;
				}
			}
		}
		//TODO: is this actually right? it seems like it should instead be
		//a check to see if there is a tile at pn.pos+(0,0,1)
		if(canCrossWalls) {
			if(IsEffectRegion) {
				if(dz < 0 ? (absDZ > zUpMax) :
						(dz > 0 ? absDZ > zDownMax : false)) {
					return PathDecision.PassOnly;
				}
			} else {
				if((dz == 0 ?
					(zDownMin != 0 && zUpMin != 0) :
						(dz < 0 ? (dz > -zDownMin || dz <= -zDownMax) :
						(dz < zUpMin || dz >= zUpMax)))) {
					return PathDecision.PassOnly;
				}
			}
		}
		return PathDecision.Normal;
	}

	public virtual PathNode[] GetValidTiles() {
	  return GetValidTiles(
			owner.character.TilePosition,
			Quaternion.Euler(0, owner.character.Facing, 0)
		);
	}
	
	public virtual PathNode[] GetValidTiles(Quaternion q) {
		return GetValidTiles(owner.character.TilePosition, q);
	}
	
	public virtual PathNode[] GetValidTiles(Vector3 tc) {
		return GetValidTiles(tc, Quaternion.Euler(0, owner.character.Facing, 0));
	}

	public virtual PathNode[] GetValidTiles(Vector3 tc, Quaternion q) {
		return GetValidTiles(
			tc, q,
			radiusMin, radiusMax,
			zDownMin, zDownMax,
			zUpMin, zUpMax,
			interveningSpaceType
		);
	}
	
	public virtual PathNode[] GetTilesInRegion() {
	  return GetValidTiles(
			owner.character.TilePosition,
			Quaternion.Euler(0, owner.character.Facing, 0),
			radiusMin, radiusMax,
			zDownMin, zDownMax,
			zUpMin, zUpMax,
			InterveningSpaceType.Pick
		);
	}

	public virtual PathNode[] GetValidTiles(
		Vector3 tc, Quaternion q,
		float xyrmn, float xyrmx,
		float zrdmn, float zrdmx,
		float zrumn, float zrumx,
		InterveningSpaceType spaceType
  ) {
		//intervening space selection filters what goes into `nodes` and what
		//nodes get picked next time around—i.e. how prevs get set up.
		
		//TODO: line vs pick vs path vs arc should instead be composed with the region type to generate the final region
		//use tc if we're anything but self or predicate or compound to get the candidate tiles
		//use q if we're a cone or line to get the candidate tiles
		//TODO: all should operate with quaternion-based node generators as well as grid-based node generators (and radius-based node generators?)
		Vector3 here = Trunc(tc);
		var pickables = spaceType == interveningSpaceType ? 
			CylinderTilesAround(here, xyrmx, zrdmx, zrumx, PathNodeIsValidRange) :
			CylinderTilesAround(here, xyrmx, zrdmx, zrumx, PathNodeIsValidHack);
		IEnumerable<PathNode> picked=null;
		switch(spaceType) {
			case InterveningSpaceType.Arc:
				picked = ArcReachableTilesAround(
					here,
					pickables,
					xyrmx
				);
				break;
			case InterveningSpaceType.Line:
				picked = LineReachableTilesAround(
					here,
					pickables
				);
				break;
			case InterveningSpaceType.Pick:
				picked = PickableTilesAround(
					here,
					pickables
				);
				break;
			case InterveningSpaceType.Path:
			  picked = PathableTilesAround(
					here,
					pickables,
					xyrmx,
					zrdmx,
					zrumx
				);
				break;
		}
		return picked.Where(delegate(PathNode n) {
			int signedDZ = n.SignedDZFrom(here);
			return n.XYDistanceFrom(here) >= xyrmn &&
			(signedDZ < 0 ? signedDZ <= -zrdmn : (signedDZ > 0 ? signedDZ >= zrumn : true));
		}).ToArray();
	}

	public virtual List<Character> CharactersForTargetedTiles(PathNode[] tiles) {
		List<Character> targets = new List<Character>();
		foreach(PathNode pn in tiles) {
			Character c = map.CharacterAt(pn.pos);
			if(c != null) {
				targets.Add(c);
			}
		}
		return targets;
	}
	public PathNode LastPassableTileBeforeTargetedTile(PathNode p) {
		//go back up prev until we hit the last blocking item before src
		PathNode cur = p;
		PathNode lastEnd = p;
		while(cur != null) {
			if(cur.isWall && !canCrossWalls) {
				lastEnd = cur;
			}
			if(cur.isEnemy && !canCrossEnemies) {
				lastEnd = cur;
			}
			cur = cur.prev;
		}
		return lastEnd;
	}

	public virtual PathNode[] ActualTilesForTargetedTiles(PathNode[] tiles) {
		//for arc and line, this may be different from the requested tile/tiles
		return tiles.
			Select(t => LastPassableTileBeforeTargetedTile(t)).
			Distinct().
			ToArray();
	}
	
	Vector3 Trunc(Vector3 v) {
		return new Vector3((int)v.x, (int)v.y, (int)v.z);
	}

	Vector3 Round(Vector3 v) {
		return new Vector3(Mathf.Round(v.x), Mathf.Round(v.y), Mathf.Round(v.z));
	}

#region Pathing and movement

  void TryAddingJumpPaths(
		PriorityQueue<float, PathNode> queue, 
		HashSet<PathNode> closed, 
		Dictionary<Vector3, PathNode> pickables, 
		List<PathNode> ret, 
		PathNode pn, 
		int n2x, int n2y,
		float maxRadius,
		float zUpMax, 
		float zDownMax, 
		float jumpDistance,
		Vector3 dest
	) {
		//FIXME: do something smart with arcs in the future
  	for(int j = 0; j < jumpDistance; j++) {
  		//don't go further than our move would allow
  		if(pn.distance+2+j > maxRadius) { break; }
  		Vector2 jumpAdj = new Vector2(pn.pos.x+n2x*(j+2), pn.pos.y+n2y*(j+2));
  		bool canJumpNoFurther = false;
  		foreach(int jumpAdjZ in map.ZLevelsWithin((int)jumpAdj.x, (int)jumpAdj.y, (int)pn.pos.z, -1)) {
  			Vector3 jumpPos = new Vector3(jumpAdj.x, jumpAdj.y, jumpAdjZ);
  			float jumpDZ = map.AbsDZForMove(jumpPos, pn.pos);
  			//TODO: decide whether we can only cross like this downwards, or if up is also allowed
  			if(jumpDZ <= zDownMax) {
  				float addedJumpCost = 2+j-0.01f*(Mathf.Max(zUpMax, zDownMax)-jumpDZ)+1;
  				PathNode jumpPn = new PathNode(jumpPos, pn, pn.distance+addedJumpCost);
  				jumpPn.isLeap = true;
  				jumpPn.isWall = map.TileAt(jumpPos+new Vector3(0,0,1)) != null;
  				jumpPn.isEnemy = map.CharacterAt(jumpPos) != null && map.CharacterAt(jumpPos).EffectiveTeamID != owner.character.EffectiveTeamID;
					jumpPn.prev = pn;
  				if(pickables.ContainsKey(jumpPos)) {
  					jumpPn.canStop = pickables[jumpPos].canStop;
					} else {
						//can't land here
						continue;
					}
					//FIXME: these ".z == .z" checks may be buggy wrt tall tiles
  				if(jumpPn.isWall && !canCrossWalls) {
  					if(jumpPos.z == pn.pos.z || jumpPos.z == pn.pos.z+1) {
  						canJumpNoFurther = true;
  						break;
  					}
  					continue;
  				}
  				if(jumpPn.isEnemy && !canCrossEnemies) {
  					if(jumpPos.z == pn.pos.z || jumpPos.z == pn.pos.z+1) {
  						canJumpNoFurther = true;
  						break;
  					}
  					continue;
  				}
					//Debug.Log("enqueue leap to "+jumpPn.pos);
					queue.Enqueue(jumpPn.distance+Mathf.Abs(jumpPos.x-dest.x)+Mathf.Abs(jumpPos.y-dest.y)+Mathf.Abs(jumpPos.z-dest.z), jumpPn);
  			} else if(jumpAdjZ > pn.pos.z) { //don't jump upwards or through a wall
  				MapTile jt = map.TileAt(jumpPos);
  				if(jt != null && jt.z <= pn.pos.z+2 && !canCrossWalls) { canJumpNoFurther = true; }
  				break;
  			}
  		}
  		if(canJumpNoFurther) {
  			break;
  		}
  	}	
  }
//rewrite as several smaller functions, one for each space type
//√ pick -- anywhere within region, prev nodes are all null
//√ path -- anywhere -reachable- within region, prev nodes lead back to start by walking path
//√ line -- anywhere within direct line from start, prev nodes lead back to start
//X arc  -- anywhere within arc, prev nodes lead in a parabola
	bool AddPathTo(
		PathNode destPn, Vector3 start, 
		Dictionary<Vector3, PathNode> pickables, 
		List<PathNode> ret,
		float maxRadius, //max cost for path
		float zDownMax, //apply to each step
		float zUpMax //apply to each step
	) {
		if(ret.Contains(destPn)) {
			//Debug.Log("dest "+destPn.pos+" is already in ret");
			return true;
		}
		Vector3 dest = destPn.pos;
		if(dest == start) { 
			//Debug.Log("ret gets "+dest+" which == "+start); 
			ret.Add(destPn); 
			return true;
		}
//		Debug.Log("seek path to "+dest);
		int jumpDistance = (int)(zDownMax/2);
		HashSet<PathNode> closed = new HashSet<PathNode>();
		var queue = new PriorityQueue<float, PathNode>();
		if(!pickables.ContainsKey(start)) { return false; }
		PathNode startNode = pickables[start];
		pickables[start] = startNode;
		queue.Enqueue(startNode.distance, startNode);
		int tries = 0;
		while(!queue.IsEmpty && tries < 100) {
			tries++;
			PathNode pn = queue.Dequeue();
//			Debug.Log("dequeue "+pn);
			//skip stuff we've seen already
			if(closed.Contains(pn)) {
//				Debug.Log("closed");
				continue;
			}	
			//if we have a path, or can reach a node that is in ret, add the involved nodes to ret if they're not yet present
			if(pn.pos == dest) {
				//add all prevs to ret
				PathNode cur = pn;
//				Debug.Log("cur:"+cur.pos+" dest:"+dest+" con? "+ret.Contains(pn));
//				Debug.Log("found path from "+start+" to "+dest+" through...");
				while(cur.prev != null) {
					//Debug.Log(""+cur.pos);
					if(!ret.Contains(cur)) {
						ret.Add(cur);
					}
					cur = cur.prev;
				}
				//and return true
				return true;
			}
			closed.Add(pn);
			//each time around, enqueue XYZ neighbors of cur that are in pickables and within zdownmax/zupmax. this won't enable pathing through walls, but that's kind of an esoteric usage anyway. file a bug. remember the jumping to cross gaps if a neighbor doesn't have a tile there (extend the neighbor search until we're past zDownMax/2)
			if(pn.XYDistanceFrom(start) == maxRadius) {
				//don't bother trying to add any more points, they'll be too far
				continue;
			}
      foreach(Vector2 n2 in XYNeighbors)
      {
				if(pn.XYDistanceFrom(start)+n2.x+n2.y > maxRadius) {
					continue;
				}
				float px = pn.pos.x+n2.x;
				float py = pn.pos.y+n2.y;
				//Debug.Log("search at "+px+", "+py + " (d "+n2.x+","+n2.y+")");
				
				//TODO: fix people being able to walk through the floor!
				foreach(int adjZ in map.ZLevelsWithin((int)px, (int)py, (int)pn.pos.z, -1)) {
					Vector3 pos = Trunc(new Vector3(px, py, adjZ));
					float dz = map.SignedDZForMove(pos, pn.pos);
					if(dz > 0 && dz > zUpMax) { continue; }
					if(dz < 0 && Mathf.Abs(dz) > zDownMax) { continue; }
					
					if(map.TileAt(pos) == null) {
						//can't path through empty space
						continue; 
					}
					PathNode next=null;
        	if(!pickables.TryGetValue(pos, out next))
        	{
						continue;
					}
					if(closed.Contains(next)) {
						//skip stuff we've already examined
						continue;
					}
					if(adjZ < pn.pos.z) {
						//try to jump across me
						TryAddingJumpPaths(queue, closed, pickables, ret, pn, (int)n2.x, (int)n2.y, maxRadius, zUpMax, zDownMax, jumpDistance, dest);
					}
					float addedCost = Mathf.Abs(n2.x)+Mathf.Abs(n2.y)-0.01f*(Mathf.Max(zUpMax, zDownMax)-Mathf.Abs(pn.pos.z-adjZ)); //-0.3f because we are not a leap
					next.isWall = map.TileAt(pos+new Vector3(0,0,1)) != null;
					next.isEnemy = map.CharacterAt(pos) != null && map.CharacterAt(pos).EffectiveTeamID != owner.character.EffectiveTeamID;
					next.distance = pn.distance+addedCost;
					next.prev = pn;
					if(next.isWall && !canCrossWalls) {
						continue;
					}
					if(next.isEnemy && !canCrossEnemies) {
						continue;
					}
					queue.Enqueue(next.distance+Mathf.Abs(pos.x-dest.x)+Mathf.Abs(pos.y-dest.y)+Mathf.Abs(pos.z-dest.z), next);
					//Debug.Log("enqueue "+next.pos+" with cost "+next.distance);
				}
      }
		}
		if(tries >= 100) {
			Debug.LogError("escape infinite loop in pathing from "+start+" to "+destPn.pos);
		}
		return false;
	}

	public IEnumerable<PathNode> PathableTilesAround(
		Vector3 here,
		Dictionary<Vector3, PathNode> pickables,
		float xyrmx,
		float zrdmx,
		float zrumx
	) {
		var ret = new List<PathNode>();
		//we bump start up by 1 in z so that the line can come from the head rather than the feet
		Vector3 truncStart = Trunc(here);
		var sortedPickables = pickables.Values.
			OrderBy(p => p.XYDistanceFrom(here)).
			ThenBy(p => Mathf.Abs(p.SignedDZFrom(here)));
			//TODO: cache and reuse partial search results
		foreach(PathNode pn in sortedPickables) {
			//find the path
//			if(pn.prev != null) { Debug.Log("pos "+pn.pos+" has prev "+pn.prev.pos); continue; }
			AddPathTo(pn, truncStart, pickables, ret, xyrmx, zrdmx, zrumx);
		}
		return ret;
	}
	public Dictionary<Vector3, PathNode> CylinderTilesAround(
		Vector3 start,
		float maxRadius,
		float zDownMax,
		float zUpMax,
		PathNodeIsValid isValid
	) {
		var ret = new Dictionary<Vector3, PathNode>();
		//for all tiles at all z levels with xy manhattan distance < max radius and z manhattan distance between -zDownMax and +zUpMax, make a node if that tile passes the isValid check
		float maxBonus = useArcRangeBonus ? Mathf.Max(zDownMax, zUpMax)/2.0f : 0;
		for(float i = -maxRadius-maxBonus; i <= maxRadius+maxBonus; i++) {
			for(float j = -maxRadius-maxBonus; j <= maxRadius+maxBonus; j++) {
				if(Mathf.Abs(i)+Mathf.Abs(j) > maxRadius+Mathf.Abs(maxBonus)) { 
					continue; 
				}
//				Debug.Log("gen "+i+","+j);
				
				Vector2 here = new Vector2(start.x+i, start.y+j);
				IEnumerable<int> levs = map.ZLevelsWithin((int)here.x, (int)here.y, (int)start.z, -1);
				foreach(int adjZ in levs) {
					Vector3 pos = new Vector3(here.x, here.y, adjZ);
					//CHECK: is this right? should it just be the signed delta? or is there some kind of "signed delta between lowest/highest points for z- and highest/lowest points for z+" nonsense?
					float signedDZ = map.SignedDZForMove(pos, start);
//					Debug.Log("signed dz:"+signedDZ+" at "+pos.z+" from "+start.z);
					if(signedDZ < -zDownMax || signedDZ > zUpMax) { 
						continue; 
					}
					float bonus = useArcRangeBonus ? -signedDZ/2.0f : 0;
//					Debug.Log("bonus at z="+adjZ+"="+bonus);
					if(Mathf.Abs(i) + Mathf.Abs(j) > maxRadius+bonus) {
						continue;
					}
					float adz = Mathf.Abs(signedDZ);
					PathNode newPn = new PathNode(pos, null, i+j+0.01f*adz);
					Character c = map.CharacterAt(pos);
					if(c != null && 
						 c.EffectiveTeamID != owner.character.EffectiveTeamID) {
						newPn.isEnemy = true;
					}
					MapTile aboveT = map.TileAt((int)pos.x, (int)pos.y, (int)pos.z+1);
					if(aboveT != null) {
						newPn.isWall = true;
					}
					PathDecision decision = isValid(start, newPn, map.CharacterAt(pos));
					if(decision == PathDecision.PassOnly) {
						newPn.canStop = false;
					}
					if(decision != PathDecision.Invalid) {
						ret.Add(pos, newPn);
					}
				}
			}
		}
		return ret;
	}
	
	public IEnumerable<PathNode> PickableTilesAround(
		Vector3 here,
		Dictionary<Vector3, PathNode> pickables
	) {
		return pickables.Values;
	}
	
	bool AnglesWithin(float a, float b, float eps) {
		return Mathf.Abs(Mathf.DeltaAngle(a, b)) < eps;
	}
	
	public IEnumerable<PathNode> LineReachableTilesAround(
		Vector3 start,
		Dictionary<Vector3, PathNode> pickables
	) {
		var ret = new List<PathNode>();
		//we bump start up by 1 in z so that the line can come from the head rather than the feet
		Vector3 truncStart = Trunc(start+new Vector3(0,0,1));
		var sortedPickables = pickables.Values.
			OrderBy(p => p.XYDistanceFrom(start)).
			ThenBy(p => Mathf.Abs(p.SignedDZFrom(start)));
		//TODO: improve efficiency by storing intermediate calculations -- i.e. the tiles on the line from end to start
		foreach(PathNode pn in sortedPickables) {
			if(pn.prev != null) { continue; }
			Vector3 here = pn.pos;
			Vector3 truncHere = Trunc(here);
			if(truncHere == truncStart) {
				ret.Add(pn);
				continue;
			}
			Vector3 d = truncStart-truncHere;
			//HACK: moves too fast and produces infinite loops 
			//when normalized d is too big relative to the actual distance
			d = d.normalized;
			PathNode cur = pn;
			Vector3 prevTrunc = here;
			int tries = 0;
			while(truncHere != truncStart) {
				here += d;
				truncHere = Round(here);
				if(prevTrunc == truncHere) { continue; }
				prevTrunc = truncHere;
				PathNode herePn = null;
				if(pickables.ContainsKey(truncHere)) {
					herePn = pickables[truncHere];
				} else { //must be empty air
					herePn = new PathNode(truncHere, null, 0);
					if(map.TileAt(truncHere+new Vector3(0,0,1)) != null) {
						herePn.isWall = true;
					}
					pickables.Add(truncHere, herePn);
				}
				cur.prev = herePn;
				cur = herePn;
				if(herePn.isWall && !canCrossWalls) {
					//don't add this node or parents and break now
					break;
				}
				if(herePn.isEnemy && !canCrossEnemies) {
					//don't add this node and break now
					break;
				}
				if(truncHere == truncStart || tries > 50) { 
					ret.Add(pn);
					break; 
				}
				tries++;
			}
			if(tries >= 50) {
				Debug.LogError("infinite loop while walking by "+d+" to "+truncStart+" from "+truncHere);
			}
		}
		return ret;
	}
	
	bool FindArcFromTo(
		Vector3 startPos, Vector3 pos, Vector3 dir,
		float d, float theta, float v, float g, float dt,
		Dictionary<Vector3, PathNode> pickables
	) {
/*		Color c = new Color(Random.value, Random.value, Random.value, 0.6f);*/
		
		Vector3 prevPos = startPos;
		float sTH = Mathf.Sin(theta);
		float cosTH = Mathf.Cos(theta);
		float endT = d / (v * cosTH);
		if(endT < 0) { Debug.LogError("Bad end T! "+endT); }
		float t = 0;
		while(t < endT && prevPos != pos) {
			//x(t) = v*cos(45)*t
			float xDist = v * cosTH * t;
			float xr = xDist / d;
			//y(t) = v*sin(45)*t - (g*t^2)/2
			float y = v * sTH * t - (g * t * t)/2.0f;
			Vector3 testPos = Round(new Vector3(startPos.x, startPos.y, startPos.z+y) + xr*dir);
			if(testPos == prevPos && t != 0) { t += dt; continue; }
/*			Debug.DrawLine(map.TransformPointWorld(prevPos), map.TransformPointWorld(testPos), c, 1.0f);*/
			PathNode pn = null;
			if(pickables.ContainsKey(testPos)) {
				pn = pickables[testPos];
				pn.prev = pickables[prevPos];
				pn.distance = xDist;
			} else {
				//through an invalid point? 
				//FIXME: will this work with big/tall tiles?
				if(map.TileAt(testPos) != null) {
					break;
				}
				//through empty space? that's ok!
				pickables[testPos] = pn = new PathNode(testPos, pickables[prevPos], xDist);
				pn.canStop = false;
				pn.isWall = map.TileAt(testPos+new Vector3(0,0,1)) != null;
				pn.isEnemy = map.CharacterAt(testPos) != null && map.CharacterAt(testPos).EffectiveTeamID != owner.character.EffectiveTeamID;
			}
			if(prevPos.z < testPos.z && map.TileAt(testPos) != null) {
				pn.isWall = true;
			}
			if(pn.isWall && !canCrossWalls) {
				//no good!
				break;
			}
			if(pn.isEnemy && !canCrossEnemies) {
				//no good!
				break;
			}
			prevPos = testPos;
			t += dt;
		}
//		Debug.DrawLine(map.TransformPointWorld(prevPos), map.TransformPointWorld(pos), c, 1.0f);
		if(prevPos == pos) {
			return true;
		} else if(t >= endT) {
/*			Debug.Log("T passed "+endT+" without reaching "+pos+", got "+prevPos);*/
		}
		return false;
	}
	
	//REMEMBER: should take into account extra xy range for downward z levels (arc)
	//there's a per-dz radius bonus/penalty for arcing (half the delta, signed)
	public IEnumerable<PathNode> ArcReachableTilesAround(
		Vector3 here,
		Dictionary<Vector3, PathNode> pickables,
		float maxRadius
	) {
		List<PathNode> ret = new List<PathNode>();
		Vector3 startPos = here+new Vector3(0,0,1);
		if(!pickables.ContainsKey(startPos) && map.TileAt(startPos) == null) {
			pickables[startPos] = new PathNode(startPos, null, 0);
		}
		float g = 9.8f;
		float dt = 0.05f;
		float v = Mathf.Sqrt(g*maxRadius);
		foreach(var pair in pickables.ToList()) {
			Vector3 pos = pair.Key;
			//Debug.Log("check "+pos);
			Vector3 dir = new Vector3(pos.x-startPos.x, pos.y-startPos.y, 0);
			PathNode posPn = pair.Value;
			float d = Mathf.Sqrt((pos.x-startPos.x)*(pos.x-startPos.x)+(pos.y-startPos.y)*(pos.y-startPos.y));
			if(d == 0) { continue; } //impossible to hit
			//theta = atan((v^2±sqrt(v^4-g(gx^2+2yv^2))/gx)) for x=distance, y=target y
			//either root, if it's not imaginary, will work! otherwise, bail because v is too small
			float thX = d;
			float thY = pos.z-startPos.z;
			float thSqrtTerm = Mathf.Pow(v,4)-g*(g*thX*thX+2*thY*v*v);
			if(thSqrtTerm < 0) { continue; } //impossible to hit with current v
			float theta1 = Mathf.Atan((v*v+Mathf.Sqrt(thSqrtTerm))/(g*thX));
			float theta2 = Mathf.Atan((v*v-Mathf.Sqrt(thSqrtTerm))/(g*thX));
			//try 1, then 2. we also accept aiming downward.
			if(FindArcFromTo(startPos, pos, dir, d, theta1, v, g, dt, pickables)) {
				posPn.velocity = v;
				posPn.altitude = theta1;
				ret.Add(posPn);
			} else if(FindArcFromTo(startPos, pos, dir, d, theta2, v, g, dt, pickables)) {
				posPn.velocity = v;
				posPn.altitude = theta2;
				ret.Add(posPn);
			} else {
//				Debug.Log("nope");
			}
		}
		return ret;
	}

#endregion
}