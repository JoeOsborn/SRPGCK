using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum RegionType {
	Cylinder,
	Sphere,
	Line,
	Cone,
	Self,
	Predicate, //actually "CylinderPredicate"--applies predicate to tiles within standard cylindrical region
	Compound //merges subregions based on their types, ignoring their intervening space types.
};

public enum InterveningSpaceType {
	Pick, //pick anywhere in 3d space
	Path, //walkable path
	Line, //straight line from source, possibly blocked by walls or enemies
	Arc   //arced line from source, possibly blocked by walls or enemies
};

[System.Serializable]
public class Region {
	protected Skill owner;
	public Skill Owner {
		get { return owner; }
		set {
			owner = value;
			foreach(Region r in regions) {
				r.Owner = owner;
			}
		}
	}
	public Formulae fdb { get {
		if(owner != null) { return owner.fdb; }
		return Formulae.DefaultFormulae;
	} }

	public RegionType type=RegionType.Cylinder;

	public InterveningSpaceType interveningSpaceType=InterveningSpaceType.Pick;

	public bool useArcRangeBonus=false;

	//WARNING: region generators will avoid using DZ checks if this flag is not set.
	//now, these will get filtered out by the final range filter at the end of getvalidnodes,
	//but just keep it in mind for certain fancy intervening-space generators
	//this flag will have no effect on the Pick space type or the Line or Cone region types.
	public bool useAbsoluteDZ=true;

	//these mean the same for pathable and non-pathable regions
	//they don't apply to self or predicate.
	public bool canCrossWalls=true;
	public bool canCrossEnemies=true;
	//turn this off for move skills!
	//it only has meaning if canCrossEnemies is false.
	//basically, it's the difference between:
	//ending ON an enemy (as an attack would); and
	//ending BEFORE an enemy (as a move would)
	public bool canHaltAtEnemies=true;

	//these apply to cylinder/predicate, sphere, cone, and line
	public Formula radiusMinF, radiusMaxF;
	//these apply to cylinder/predicate (define), sphere (clip), line (define up/down displacements from line)
	public Formula zUpMinF, zUpMaxF, zDownMinF, zDownMaxF;
	//these apply to cone and line
	public Formula xyDirectionF, zDirectionF;
	//these apply to cone
	public Formula xyArcMinF, zArcMinF;
	public Formula xyArcMaxF, zArcMaxF;
	public Formula rFwdClipMaxF;
	//these apply to line
	public Formula lineWidthMinF, lineWidthMaxF;
	//this applies to predicate, and gets these variable bindings as skill params:
	//arg.region.{...}
	//x, y, z, angle.xy, angle.z,
	//target.x, target.y, target.z, angle.between.xy, angle.between.z
	//dx, dy, dz, distance, distance.xy, mdistance, mdistance.xy
	//as a bonus, in the scope of a region lookup the skill's "current target" is the character on a given tile
	//it should return 0 (false) or non-0 (true)
	public Formula predicateF;

	//only used for compound regions. subregions of a compound region may only
	//generate tiles, and may not apply their intervening space modes.
	//more complex uses of compound spaces should subclass Skill or Region.
	public Region[] regions;

	public float radiusMin { get { return radiusMinF.GetValue(fdb, owner, null, null); } }
	public float radiusMax { get { return radiusMaxF.GetValue(fdb, owner, null, null); } }
	public float zUpMin { get { return zUpMinF.GetValue(fdb, owner, null, null); } }
	public float zUpMax { get { return zUpMaxF.GetValue(fdb, owner, null, null); } }
	public float zDownMin { get { return zDownMinF.GetValue(fdb, owner, null, null); } }
	public float zDownMax { get { return zDownMaxF.GetValue(fdb, owner, null, null); } }
	public float lineWidthMin { get { return lineWidthMinF.GetValue(fdb, owner, null, null); } }
	public float lineWidthMax { get { return lineWidthMaxF.GetValue(fdb, owner, null, null); } }

	public float xyDirection { get { return xyDirectionF.GetValue(fdb, owner, null, null); } }
	public float zDirection { get { return zDirectionF.GetValue(fdb, owner, null, null); } }
	public float xyArcMin { get { return xyArcMinF.GetValue(fdb, owner, null, null); } }
	public float zArcMin { get { return zArcMinF.GetValue(fdb, owner, null, null); } }
	public float xyArcMax { get { return xyArcMaxF.GetValue(fdb, owner, null, null); } }
	public float zArcMax { get { return zArcMaxF.GetValue(fdb, owner, null, null); } }

	public float rFwdClipMax { get { return rFwdClipMaxF.GetValue(fdb, owner, null, null); } }

	protected Map map { get { return owner.map; } }

	public delegate PathDecision PathNodeIsValid(Vector3 start, PathNode pn, Character c);

	readonly Vector2[] XYNeighbors = {
		new Vector2(-1, 0),
		new Vector2( 1, 0),
		new Vector2( 0,-1),
		new Vector2( 0, 1)
	};

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
		zDirectionF = Formula.Constant(0);
		xyArcMinF = Formula.Constant(0);
		xyArcMaxF = Formula.Constant(0);
		zArcMinF = Formula.Constant(0);
		zArcMaxF = Formula.Constant(0);
		lineWidthMinF = Formula.Constant(0);
		lineWidthMaxF = Formula.Constant(0);
		predicateF = Formula.Constant(0);
	}

	protected bool isEffectRegion=false;
	public bool IsEffectRegion {
		get { return isEffectRegion; }
		set {
			isEffectRegion = value;
			if(regions == null) { return; }
			foreach(Region r in regions) {
				r.IsEffectRegion = value;
			}
		}
	}

	// //FIXME: wrong because of reliance on z{Up|Down}{Max|Min}
	// public virtual PathDecision PathNodeIsValidHack(Vector3 start, PathNode pn, Character c) {
	// 	float dz = useAbsoluteDZ ? map.SignedDZForMove(pn.position, start) : pn.signedDZ;
	// 	float absDZ = Mathf.Abs(dz);
	// 	if(c != null && c != owner.character) {
	// 		if(c.EffectiveTeamID != owner.character.EffectiveTeamID) {
	// 			return (IsEffectRegion ? PathDecision.Normal : PathDecision.PassOnly);
	// 		} else {
	// 			if(!IsEffectRegion) {
	// 				return PathDecision.PassOnly;
	// 			}
	// 		}
	// 	}
	// 	if(IsEffectRegion) {
	// 		if(dz < 0 ? (absDZ > zUpMax) :
	// 				(dz > 0 ? absDZ > zDownMax : false)) {
	// 			return PathDecision.PassOnly;
	// 		}
	// 	} else {
	// 		if((dz == 0 ?
	// 			(zDownMin != 0 && zUpMin != 0) :
	// 				(dz < 0 ? (dz > -zDownMin || dz <= -zDownMax) :
	// 				(dz < zUpMin || dz >= zUpMax)))) {
	// 			return PathDecision.PassOnly;
	// 		}
	// 	}
	// 	return PathDecision.Normal;
	// }
	public virtual PathDecision PathNodeIsValidPredicate(
		Vector3 start,
		PathNode pn,
		Character c
	) {
		Vector3 pos = pn.pos;
		float distance = Vector3.Distance(pos, start);
		float xyDistance = (new Vector2(pos.x-start.x, pos.y-start.y)).magnitude;
		Character oldTarget = owner.currentTarget;
		owner.currentTarget = c;
		owner.SetParam("arg.region.distance", distance);
		owner.SetParam("arg.region.distance.xy", xyDistance);
		owner.SetParam("arg.region.mdistance", Mathf.Abs(pos.x-start.x)+Mathf.Abs(pos.y-start.y)+Mathf.Abs(pos.z-start.z));
		owner.SetParam("arg.region.mdistance.xy", Mathf.Abs(pos.x-start.x)+Mathf.Abs(pos.y-start.y));
		owner.SetParam("arg.region.dx", Mathf.Abs(pos.x-start.x));
		owner.SetParam("arg.region.dy", Mathf.Abs(pos.y-start.y));
		owner.SetParam("arg.region.dz", Mathf.Abs(pos.z-start.z));
		owner.SetParam("arg.region.target.x", pos.x);
		owner.SetParam("arg.region.target.y", pos.y);
		owner.SetParam("arg.region.target.z", pos.z);
		owner.SetParam("arg.region.angle.between.absolute.xy", Mathf.Atan2(pos.y-start.y, pos.x-start.x)*Mathf.Rad2Deg);
		owner.SetParam("arg.region.angle.between.absolute.z", Mathf.Atan2(pos.z-start.z, xyDistance)*Mathf.Rad2Deg);
		owner.SetParam("arg.region.angle.between.xy", Mathf.Atan2(pos.y-start.y, pos.x-start.x)*Mathf.Rad2Deg - owner.GetParam("arg.region.angle.xy"));
		owner.SetParam("arg.region.angle.between.z", Mathf.Atan2(pos.z-start.z, xyDistance)*Mathf.Rad2Deg - owner.GetParam("arg.region.angle.z"));
		float ret = predicateF.GetValue(fdb, owner, null, null);
		owner.currentTarget = oldTarget;
		return (ret != 0) ? PathDecision.Normal : PathDecision.Invalid;
	}

	//FIXME: wrong because of reliance on z{Up|Down}{Max|Min}
	public virtual PathDecision PathNodeIsValidRange(Vector3 start, PathNode pn, Character c) {
		float dz = useAbsoluteDZ ? map.SignedDZForMove(pn.position, start) : pn.signedDZ;
		float absDZ = Mathf.Abs(dz);
		//TODO: replace with some type of collision check?
		if(c != null && c != owner.character) {
			if(c.EffectiveTeamID != owner.character.EffectiveTeamID) {
				if(canCrossEnemies) {
					return (IsEffectRegion||canHaltAtEnemies) ? PathDecision.Normal : PathDecision.PassOnly;
				} else {
					return canHaltAtEnemies ? PathDecision.Normal : PathDecision.Invalid;
				}
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
			lineWidthMin, lineWidthMax,
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
			lineWidthMin, lineWidthMax,
			interveningSpaceType,
			true
		);
	}

	public virtual PathNode[] GetValidTiles(
		Vector3 tc, Quaternion q,
		float xyrmn, float xyrmx,
		float zrdmn, float zrdmx,
		float zrumn, float zrumx,
		float lwmn, float lwmx,
		InterveningSpaceType spaceType,
		bool returnAllTiles=false
  ) {
		//intervening space selection filters what goes into `nodes` and what
		//nodes get picked next time around—i.e. how prevs get set up.

		//TODO: all should operate with continuous generators as well as grid-based generators
		Vector3 here = SRPGUtil.Trunc(tc);
		Dictionary<Vector3, PathNode> pickables = null;
		switch(type) {
			case RegionType.Cylinder:
				pickables = CylinderTilesAround(here, xyrmx, zrdmx, zrumx, PathNodeIsValidRange);
				break;
			case RegionType.Sphere:
				pickables = SphereTilesAround(here, xyrmx, zrdmx, zrumx, PathNodeIsValidRange);
				break;
			case RegionType.Line:
				pickables = LineTilesAround(here, q, xyrmx, zrdmx, zrumx, xyDirection, zDirection, lwmn, lwmx, PathNodeIsValidRange);
				break;
			case RegionType.Cone:
				pickables = ConeTilesAround(here, q, xyrmx, zrdmx, zrumx, xyDirection, zDirection, xyArcMin, xyArcMax, zArcMin, zArcMax, rFwdClipMax, PathNodeIsValidRange);
				break;
			case RegionType.Self:
				pickables =	new Dictionary<Vector3, PathNode>(){
					{here, new PathNode(here, null, 0)}
				};
				break;
			case RegionType.Predicate:
				pickables =	PredicateSatisfyingTilesAround(here, q, xyrmx, zrdmx, zrumx);
				break;
			case RegionType.Compound:
				pickables =	new Dictionary<Vector3, PathNode>();
				for(int i = 0; i < regions.Length; i++) {
					Region r = regions[i];
					PathNode[] thesePickables = r.GetValidTiles(
						here, q,
						//pass the subregion's formulae for these so
						//that our own, ignored formulae don't clobber them
						r.radiusMin, r.radiusMax,
						r.zDownMin, r.zDownMax,
						r.zUpMin, r.zUpMax,
						r.lineWidthMin, r.lineWidthMax,
						InterveningSpaceType.Pick
					);
					foreach(PathNode p in thesePickables) {
						p.subregion = i;
						pickables[p.pos] = p;
					}
				}
				break;
			default:
				Debug.LogError("Unknown region type not supported");
				pickables = null;
				break;
		}
		IEnumerable<PathNode> picked=null;
		switch(spaceType) {
			case InterveningSpaceType.Arc:
				picked = ArcReachableTilesAround(
					here,
					pickables,
					xyrmx,
					returnAllTiles
				);
				break;
			case InterveningSpaceType.Line:
				picked = LineReachableTilesAround(
					here,
					pickables,
					returnAllTiles
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
					zrumx,
					returnAllTiles
				);
				break;
		}
		switch(type) {
			case RegionType.Predicate:
			case RegionType.Cylinder:
				return picked.Where(delegate(PathNode n) {
					float xyd = n.XYDistanceFrom(here);
					int signedDZ = useAbsoluteDZ ? (int)map.SignedDZForMove(n.position, here) : n.signedDZ;
					return xyd >= xyrmn && xyd <= xyrmx+n.bonusRange &&
				  	(signedDZ <= -zrdmn || signedDZ >= zrumn) &&
						signedDZ >= -zrdmx && signedDZ <= zrumx;
				}).ToArray();
			case RegionType.Sphere:
				return picked.Where(delegate(PathNode n) {
					float xyzd = n.XYZDistanceFrom(here);
					int signedDZ = useAbsoluteDZ ? (int)map.SignedDZForMove(n.position, here) : n.signedDZ;
					return xyzd >= xyrmn && xyzd <= xyrmx+n.bonusRange &&
					  (signedDZ <= -zrdmn || signedDZ >= zrumn) &&
						signedDZ >= -zrdmx && signedDZ <= zrumx;
				}).ToArray();
			case RegionType.Line:
				return picked.Where(delegate(PathNode n) {
					float xyd = n.radius;
					int xyOff = (int)Mathf.Abs(n.centerOffset.x) + (int)Mathf.Abs(n.centerOffset.y);
					int signedDZ = (int)n.centerOffset.z;
					return xyd >= xyrmn && xyd <= xyrmx+n.bonusRange &&
						xyOff >= lwmn && xyOff <= lwmx &&
					  (signedDZ <= -zrdmn || signedDZ >= zrumn) &&
					  signedDZ >= -zrdmx && signedDZ <= zrumx;
				}).ToArray();
			case RegionType.Cone:
			//we've already filtered out stuff at bad angles and beyond maxima.
				return picked.Where(delegate(PathNode n) {
					float xyd = n.radius;
					float fwd = n.radius*Mathf.Cos(n.angle);
					int signedDZ = (int)n.centerOffset.z;
					return xyd >= xyrmn && (signedDZ <= -zrdmn || signedDZ >= zrumn) && (rFwdClipMax <= 0 || fwd <= (rFwdClipMax+1));
				}).ToArray();
			default:
				return picked.ToArray();
		}
	}

	public virtual PathNode[] GetValidTiles(PathNode[] allTiles) {
		Dictionary<Vector3, PathNode> union = new Dictionary<Vector3, PathNode>();
		foreach(PathNode start in allTiles) {
			//take union of all valid tiles
			//TODO: in many cases, this will just be the passed-in tiles. optimize!
			PathNode[] theseValid = GetValidTiles(start.pos);
			foreach(PathNode v in theseValid) {
				union[v.pos] = v;
			}
		}
		return union.Values.ToArray();
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
		int tries = 0;
		Color c = Color.red;
		while(cur != null) {
			// Debug.Log("cur:"+cur);
			if(cur.prev != null) {
				Debug.DrawLine(map.TransformPointWorld(cur.pos), map.TransformPointWorld(cur.prev.pos), c, 1.0f, false);
			}
			if(cur.isWall && !canCrossWalls) {
				lastEnd = cur.prev;
				// if(cur.prev != null) { Debug.Log("block just before wall "+cur.prev.pos); }
			}
			if(cur.isEnemy && !canCrossEnemies) {
				if(canHaltAtEnemies) {
					lastEnd = cur;
					// Debug.Log("block on top of enemy "+cur.pos);
				} else {
					lastEnd = cur.prev;
					// if(cur.prev != null) { Debug.Log("block just before enemy "+cur.prev.pos); }
				}
			}
			cur = cur.prev;
			tries++;
			if(tries > 50) {
				Debug.LogError("Infinite loop in lastPassableTile for "+p);
				return null;
			}
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
		Vector3 start, Vector3 dest,
		bool provideAllTiles
	) {
		//FIXME: do something smart with arcs in the future
  	for(int j = 0; j < jumpDistance; j++) {
  		//don't go further than our move would allow
  		if(pn.distance+2+j > maxRadius) { break; }
  		Vector2 jumpAdj = new Vector2(pn.pos.x+n2x*(j+2), pn.pos.y+n2y*(j+2));
  		bool canJumpNoFurther = false;
  		foreach(int jumpAdjZ in map.ZLevelsWithin((int)jumpAdj.x, (int)jumpAdj.y, (int)pn.pos.z, -1)) {
  			Vector3 jumpPos = new Vector3(jumpAdj.x, jumpAdj.y, jumpAdjZ);
  			float jumpDZ = useAbsoluteDZ ? map.AbsDZForMove(start, pn.pos) : map.AbsDZForMove(jumpPos, pn.pos);
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
					//FIXME: these ".z == .z || .z==.z+1" checks may be buggy wrt tall tiles
  				if(!provideAllTiles && jumpPn.isWall && !canCrossWalls) {
  					if(jumpPos.z == pn.pos.z || jumpPos.z == pn.pos.z+1) {
  						canJumpNoFurther = true;
  						break;
  					}
  					continue;
  				}
  				if(!provideAllTiles && jumpPn.isEnemy && !canCrossEnemies) {
  					if(jumpPos.z == pn.pos.z || jumpPos.z == pn.pos.z+1) {
  						canJumpNoFurther = true;
  						break;
  					}
						if(!canHaltAtEnemies) {
	  					continue;
						}
  				}
					// Debug.Log("enqueue leap to "+jumpPn.pos);
					queue.Enqueue(jumpPn.distance+Mathf.Abs(jumpPos.x-dest.x)+Mathf.Abs(jumpPos.y-dest.y)+Mathf.Abs(jumpPos.z-dest.z), jumpPn);
  			} else if(jumpAdjZ > pn.pos.z) { //don't jump upwards or through a wall
  				MapTile jt = map.TileAt(jumpPos);
  				if(!provideAllTiles && jt != null && jt.z <= pn.pos.z+2 && !canCrossWalls) { canJumpNoFurther = true; }
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
//√ arc  -- anywhere within arc, prev nodes lead in a parabola
	bool AddPathTo(
		PathNode destPn, Vector3 start,
		Dictionary<Vector3, PathNode> pickables,
		List<PathNode> ret,
		float maxRadius, //max cost for path
		float zDownMax,  //apply to each step
		float zUpMax,    //apply to each step
		bool provideAllTiles
	) {
		if(ret.Contains(destPn)) {
			// Debug.Log("dest "+destPn.pos+" is already in ret");
			return true;
		}
		Vector3 dest = destPn.pos;
		if(dest == start) {
			// Debug.Log("ret gets "+dest+" which == "+start);
			ret.Add(destPn);
			return true;
		}
		// Debug.Log("seek path to "+dest);
		int jumpDistance = (int)(zDownMax/2);
		int headroom = 1;
		HashSet<PathNode> closed = new HashSet<PathNode>();
		var queue = new PriorityQueue<float, PathNode>();
		if(!pickables.ContainsKey(start)) { return false; }
		PathNode startNode = pickables[start];
		queue.Enqueue(startNode.distance, startNode);
		int tries = 0;
		while(!queue.IsEmpty && tries < 100) {
			tries++;
			PathNode pn = queue.Dequeue();
			// 	Debug.Log("dequeue "+pn);
			//skip stuff we've seen already
			if(closed.Contains(pn)) {
//				Debug.Log("closed");
				continue;
			}
			//if we have a path, or can reach a node that is in ret, add the involved nodes to ret if they're not yet present
			if(pn.pos == dest) {
				//add all prevs to ret
				PathNode cur = pn;
					// Debug.Log("found path from "+start+" to "+dest+":"+pn);
				while(cur.prev != null) {
					// Debug.Log(""+cur.pos);
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
			if(pn.isEnemy && !canCrossEnemies && !provideAllTiles) { continue; }
      foreach(Vector2 n2 in XYNeighbors)
      {
				if(pn.XYDistanceFrom(start)+n2.x+n2.y > maxRadius && !provideAllTiles) {
					continue;
				}
				float px = pn.pos.x+n2.x;
				float py = pn.pos.y+n2.y;
					// Debug.Log("search at "+px+", "+py + " (d "+n2.x+","+n2.y+")");

				foreach(int adjZ in map.ZLevelsWithin((int)px, (int)py, (int)pn.pos.z, -1)) {
					Vector3 pos = SRPGUtil.Trunc(new Vector3(px, py, adjZ));
					float dz = useAbsoluteDZ ? map.SignedDZForMove(pos, start) : map.SignedDZForMove(pos, pn.pos);
					if(dz > 0 && dz > zUpMax) { continue; }
					if(dz < 0 && Mathf.Abs(dz) > zDownMax) { continue; }
					if(!provideAllTiles && dz > 0 && !canCrossWalls && map.ZLevelsWithinLimits((int)pn.pos.x, (int)pn.pos.y, (int)pn.pos.z, adjZ+headroom).Length != 0) {
						continue;
					}
					if(!provideAllTiles && dz < 0 && !canCrossWalls && map.ZLevelsWithinLimits((int)pos.x, (int)pos.y, adjZ, (int)pn.pos.z+headroom).Length != 0) {
						continue;
					}
					if(map.TileAt(pos) == null) {
						//can't path through empty space
						continue;
					}
					PathNode next=null;
        	if(!pickables.TryGetValue(pos, out next)) {
						continue;
					}
					if(closed.Contains(next)) {
						//skip stuff we've already examined
						continue;
					}
					if(next.distance > 0 && next.distance <= pn.distance) {
						//skip anything that's got a better path to it than we can offer
							// Debug.Log("Don't bother looking via "+next);
						continue;
					}
					if(adjZ < pn.pos.z) {
						//try to jump across me
						TryAddingJumpPaths(queue, closed, pickables, ret, pn, (int)n2.x, (int)n2.y, maxRadius, zUpMax, zDownMax, jumpDistance, start, dest, provideAllTiles);
					}
					float addedCost = Mathf.Abs(n2.x)+Mathf.Abs(n2.y)-0.01f*(Mathf.Max(zUpMax, zDownMax)-Mathf.Abs(pn.pos.z-adjZ)); //-0.3f because we are not a leap
					next.isWall = map.TileAt(pos+new Vector3(0,0,1)) != null;
					next.isEnemy = map.CharacterAt(pos) != null && map.CharacterAt(pos).EffectiveTeamID != owner.character.EffectiveTeamID;
					next.distance = pn.distance+addedCost;
					next.prev = pn;
					if(!provideAllTiles && next.isWall && !canCrossWalls) {
						continue;
					}
					if(!provideAllTiles && next.isEnemy && !canCrossEnemies && !canHaltAtEnemies) {
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
		float zrumx,
		bool provideAllTiles
	) {
		var ret = new List<PathNode>();
		//we bump start up by 1 in z so that the line can come from the head rather than the feet
		Vector3 truncStart = SRPGUtil.Trunc(here);
		var sortedPickables = pickables.Values.
			OrderBy(p => p.XYDistanceFrom(here)).
			ThenBy(p => Mathf.Abs(p.SignedDZFrom(here)));
		//TODO: cache and reuse partial search results
		foreach(PathNode pn in sortedPickables) {
			//find the path
//			if(pn.prev != null) { Debug.Log("pos "+pn.pos+" has prev "+pn.prev.pos); continue; }
			AddPathTo(pn, truncStart, pickables, ret, xyrmx, zrdmx, zrumx, provideAllTiles);
		}
		return ret;
	}
	public Dictionary<Vector3, PathNode> PredicateSatisfyingTilesAround(
		Vector3 start,
		Quaternion q,
		float maxRadius,
		float zDownMax,
		float zUpMax
	) {
		owner.SetParam("arg.region.x", start.x);
		owner.SetParam("arg.region.y", start.y);
		owner.SetParam("arg.region.z", start.z);
		owner.SetParam("arg.region.angle.xy", q.eulerAngles.y);
		owner.SetParam("arg.region.angle.z", q.eulerAngles.z);
		return CylinderTilesAround(start, maxRadius, zDownMax, zUpMax, PathNodeIsValidPredicate);
	}
	public Dictionary<Vector3, PathNode> CylinderTilesAround(
		Vector3 start,
		float maxRadiusF,
		float zDownMax,
		float zUpMax,
		PathNodeIsValid isValid
	) {
		var ret = new Dictionary<Vector3, PathNode>();
		//for all tiles at all z levels with xy manhattan distance < max radius and z manhattan distance between -zDownMax and +zUpMax, make a node if that tile passes the isValid check
		float maxBonus = useArcRangeBonus ? Mathf.Max(zDownMax, zUpMax)/2.0f : 0;
		float maxRadius = Mathf.Floor(maxRadiusF);
		float minR = -maxRadius-maxBonus;
		float maxR = maxRadius+maxBonus;
		for(float i = minR; i <= maxR; i++) {
			for(float j = minR; j <= maxR; j++) {
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
					if(useAbsoluteDZ && (signedDZ < -zDownMax || signedDZ > zUpMax)) {
						continue;
					}
					float bonus = useArcRangeBonus ? -signedDZ/2.0f : 0;
//					Debug.Log("bonus at z="+adjZ+"="+bonus);
					if(Mathf.Abs(i) + Mathf.Abs(j) > maxRadius+bonus) {
						continue;
					}
//					float adz = Mathf.Abs(signedDZ);
					PathNode newPn = new PathNode(pos, null, 0/*i+j+0.01f*adz*/);
					newPn.bonusRange = bonus;
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

	public Dictionary<Vector3, PathNode> SphereTilesAround(
		Vector3 start,
		float maxRadiusF,
		float zDownMax,
		float zUpMax,
		PathNodeIsValid isValid
	) {
		var ret = new Dictionary<Vector3, PathNode>();
		//for all tiles at all z levels with xyz manhattan distance < max radius and z manhattan distance between -zDownMax and +zUpMax, make a node if that tile passes the isValid check
		float maxBonus = useArcRangeBonus ? Mathf.Max(zDownMax, zUpMax)/2.0f : 0;
		float maxRadius = Mathf.Floor(maxRadiusF);
		float minR = -maxRadius-maxBonus;
		float maxR = maxRadius+maxBonus;
		for(float i = minR; i <= maxR; i++) {
			for(float j = minR; j <= maxR; j++) {
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
					float adz = Mathf.Abs(signedDZ);
//					Debug.Log("signed dz:"+signedDZ+" at "+pos.z+" from "+start.z);
					if(useAbsoluteDZ && (signedDZ < -zDownMax || signedDZ > zUpMax)) {
						continue;
					}
					float bonus = useArcRangeBonus ? -signedDZ/2.0f : 0;
//					Debug.Log("bonus at z="+adjZ+"="+bonus);
					float radius = Mathf.Abs(i) + Mathf.Abs(j) + adz;
					if(radius > maxRadius+bonus) {
						continue;
					}
					PathNode newPn = new PathNode(pos, null, 0/*i+j+0.01f*adz*/);
					newPn.radius = radius;
					newPn.bonusRange = bonus;
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

	public Dictionary<Vector3, PathNode> LineTilesAround(
		Vector3 here, Quaternion q,
		float xyrmx,
		float zrdmx, float zrumx,
		float xyDirection,
		float zDirection,
		float lwmn, float lwmx,
		PathNodeIsValid isValid
	) {
		var ret = new Dictionary<Vector3, PathNode>();
		float xyTheta = (xyDirection+q.eulerAngles.y)*Mathf.Deg2Rad;
		float zPhi = (zDirection+q.eulerAngles.z)*Mathf.Deg2Rad;
		for(int r = 0; r <= (int)xyrmx; r++) {
			//FIXME: if xyTheta != 0, oxy and oz calculations may be wrong
			//FIXME: use of zPhi (cos, cos, sin) is not correct -- consider pointing vertically. it shouldn't collapse to a single column!
			Vector3 linePos = new Vector3(Mathf.Cos(xyTheta)*Mathf.Cos(zPhi)*r, Mathf.Sin(xyTheta)*Mathf.Cos(zPhi)*r, Mathf.Sin(zPhi)*r);
			for(int lwo = -(int)lwmx; lwo <= (int)lwmx; lwo++) {
				//FIXME: use of zPhi (cos, cos, 0) is not correct -- consider pointing vertically. it shouldn't collapse to a single column!
				Vector3 oxy = new Vector3(Mathf.Sin(xyTheta)*Mathf.Cos(zPhi)*lwo, Mathf.Cos(xyTheta)*Mathf.Cos(zPhi)*lwo, 0);
				for(int zRad = -(int)zrdmx; zRad <= (int)zrumx; zRad++) {
					//FIXME: (sin,sin,cos) here may be way wrong-- it really depends on xyTheta as well!!
					Vector3 oz = new Vector3(Mathf.Sin(zPhi)*zRad, Mathf.Sin(zPhi)*zRad, Mathf.Cos(zPhi)*zRad);
					Vector3 pos = SRPGUtil.Round(new Vector3(
						here.x+linePos.x+oxy.x+oz.x,
						here.y+linePos.y+oxy.y+oz.y,
						here.z+linePos.z+oxy.z+oz.z
					));
					if(map.TileAt(pos) == null) { continue; }
					PathNode pn = new PathNode(pos, null, 0);
					pn.radius = r;
					pn.angle = xyDirection;
					pn.altitude = zDirection;
					pn.centerOffset = new Vector3(lwo, 0, zRad);
					//FIXME: duplicated across other generators
					Character c = map.CharacterAt(pos);
					if(c != null &&
						 c.EffectiveTeamID != owner.character.EffectiveTeamID) {
						pn.isEnemy = true;
					}
					//FIXME: ramps. also, this might not make any sense wrt immediately stacked tiles.
					MapTile aboveT = map.TileAt((int)pos.x, (int)pos.y, (int)pos.z+1);
					if(aboveT != null) {
						pn.isWall = true;
					}
					PathDecision decision = isValid(here, pn, map.CharacterAt(pos));
					if(decision == PathDecision.PassOnly) {
						pn.canStop = false;
					}
					if(decision != PathDecision.Invalid) {
						ret[pos] = pn;
					}
				}
			}
		}
		return ret;
	}

	public Dictionary<Vector3, PathNode> ConeTilesAround(
		Vector3 here, Quaternion q,
		float xyrmx,
		float zrdmx, float zrumx,
		float xyDirection,
		float zDirection,
		float xyArcMin, float xyArcMax,
		float zArcMin, float zArcMax,
		float rFwdClipMax,
		PathNodeIsValid isValid
	) {
		float centerXYAng = (xyDirection+q.eulerAngles.y);
		float cosCenterXYAng = Mathf.Cos(Mathf.Deg2Rad*centerXYAng);
		float sinCenterXYAng = Mathf.Sin(Mathf.Deg2Rad*centerXYAng);
		float centerZAng = Mathf.Deg2Rad*(zDirection+q.eulerAngles.z);
		float cosCenterZAng = Mathf.Cos(Mathf.Deg2Rad*centerZAng);
		float sinCenterZAng = Mathf.Sin(Mathf.Deg2Rad*centerZAng);
		//just clip a sphere for now
		return SphereTilesAround(here, xyrmx, float.MaxValue, float.MaxValue, isValid).Where(delegate(KeyValuePair<Vector3, PathNode> pair) {
			PathNode n = pair.Value;
			float xyd = Vector3.Distance(n.pos, here);
			float xyArcTolerance = xyd == 0 ? 0 : (0.5f/xyd)*Mathf.Rad2Deg;
			float zArcTolerance = xyd == 0 ? 0 : (0.5f/xyd)*Mathf.Rad2Deg;

			float xyAng = Mathf.Rad2Deg*Mathf.Atan2(n.pos.y-here.y, n.pos.x-here.x)-q.eulerAngles.y;
			float zAng = Mathf.Rad2Deg*Mathf.Atan2(n.pos.z-here.z, xyd)-q.eulerAngles.z;
			//FIXME: (cos,cos,sin) is wrong, consider vertical pointing
			Vector3 centerPoint = new Vector3(cosCenterXYAng*sinCenterZAng*xyd, sinCenterXYAng*sinCenterZAng*xyd, cosCenterZAng*xyd);
			//FIXME: these four lines certainly do not belong here! side effects in a filter, bleh!
			n.radius = xyd;
			n.angle = xyAng;
			n.altitude = zAng;
			n.centerOffset = n.pos - centerPoint;
			float fwd = n.radius*Mathf.Cos(n.angle);
			float signedDZ = n.centerOffset.z;
			return xyd <= xyrmx+n.bonusRange &&
			  (SRPGUtil.AngleBetween(xyAng, xyDirection+xyArcMin-xyArcTolerance, xyDirection+xyArcMax+xyArcTolerance) ||
			   SRPGUtil.AngleBetween(xyAng, xyDirection-xyArcMax-xyArcTolerance, xyDirection-xyArcMin+xyArcTolerance)) &&
			  (SRPGUtil.AngleBetween(zAng, zDirection+zArcMin-zArcTolerance, zDirection+zArcMax+zArcTolerance) ||
			   SRPGUtil.AngleBetween(zAng, zDirection-zArcMax-zArcTolerance, zDirection-zArcMin+zArcTolerance)) &&
 				(rFwdClipMax <= 0 || fwd <= (rFwdClipMax+1)) &&
			  signedDZ >= -zrdmx && signedDZ <= zrumx;
		}).ToDictionary(pair => pair.Key, pair => pair.Value);
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
		Dictionary<Vector3, PathNode> pickables,
		bool provideAllTiles
	) {
		var ret = new List<PathNode>();
		//we bump start up by 1 in z so that the line can come from the head rather than the feet
		Vector3 truncStart = SRPGUtil.Trunc(start+new Vector3(0,0,1));
		var sortedPickables = pickables.Values.
			OrderBy(p => p.XYDistanceFrom(start)).
			ThenBy(p => Mathf.Abs(p.SignedDZFrom(start)));
		//improve efficiency by storing intermediate calculations -- i.e. the tiles on the line from end to start
		foreach(PathNode pn in sortedPickables) {
			if(pn.prev != null) { continue; }
			Vector3 here = truncStart;
			Vector3 truncEnd = SRPGUtil.Trunc(pn.pos);
			Vector3 truncHere = truncStart;
			if(truncStart == truncEnd) {
				ret.Add(pn);
				continue;
			}
			Vector3 d = truncEnd-truncHere;
			//HACK: moves too fast and produces infinite loops
			//when normalized d is too big relative to the actual distance
			d = d.normalized;
			PathNode cur=null;
			pickables.TryGetValue(truncHere, out cur);
			if(cur == null) { cur = new PathNode(truncHere, null, 0); }
			Vector3 prevTrunc = here;
			int tries = 0;
			while(truncHere != truncEnd) {
				here += d;
				truncHere = SRPGUtil.Round(here);
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
				herePn.prev = cur;
				cur = herePn;
				if(herePn.isWall && !canCrossWalls && !provideAllTiles) {
					//don't add this node or parents and break now
					break;
				}
				if(herePn.isEnemy && !canCrossEnemies && !canHaltAtEnemies && !provideAllTiles) {
					//don't add this node and break now
					break;
				}
				if(truncHere == truncEnd || tries > 50) {
					ret.Add(pn);
					break;
				}
				if(cur.isEnemy && !canCrossEnemies && !provideAllTiles) {
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
		Dictionary<Vector3, PathNode> pickables,
		bool provideAllTiles
	) {
		//Color c = new Color(Random.value, Random.value, Random.value, 0.6f);

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
			Vector3 testPos = SRPGUtil.Round(new Vector3(startPos.x, startPos.y, startPos.z+y) + xr*dir);
			if(testPos == prevPos && t != 0) { t += dt; continue; }
			// if(pos.x==1&&pos.y==0&&pos.z==0) { Debug.DrawLine(map.TransformPointWorld(prevPos), map.TransformPointWorld(testPos), c, 1.0f); }
			PathNode pn = null;
			if(pickables.ContainsKey(testPos)) {
				pn = pickables[testPos];
				if(testPos != prevPos) {
					pn.prev = pickables[prevPos];
				}
				pn.distance = xDist;
			} else {
				pickables[testPos] = pn = new PathNode(testPos, (testPos != prevPos && pickables.ContainsKey(prevPos) ? pickables[prevPos] : null), xDist);
				pn.canStop = false;
				pn.isWall = /*map.TileAt(testPos) != null && */map.TileAt(testPos+new Vector3(0,0,1)) != null;
				pn.isEnemy = map.CharacterAt(testPos) != null && map.CharacterAt(testPos).EffectiveTeamID != owner.character.EffectiveTeamID;
			}
			if(prevPos.z < testPos.z && map.TileAt(testPos) != null) {
				pn.isWall = true;
			}
			if(pn.isWall && !canCrossWalls && !provideAllTiles) {
				//no good!
//				if(pos.x==1&&pos.y==0&&pos.z==0) Debug.Log("wall, can't cross at testpos "+testPos);
				break;
			}
			if(pn.isEnemy && !canCrossEnemies && !canHaltAtEnemies && !provideAllTiles) {
				//no good!
//				if(pos.x==1&&pos.y==0&&pos.z==0) Debug.Log("enemy, can't cross, can't halt at testpos "+testPos);
				break;
			}
			if(pn.prev != null && pn.prev.isEnemy && !canCrossEnemies && !provideAllTiles) {
				//no good!
//				if(pos.x==1&&pos.y==0&&pos.z==0) Debug.Log("halted already at testpos "+testPos);
				break;
			}
//			if(pos.x==1&&pos.y==0&&pos.z==0) Debug.Log("go through testpos "+testPos);
			prevPos = testPos;
			t += dt;
		}
//		if(pos.x==1&&pos.y==0&&pos.z==0) { Debug.DrawLine(map.TransformPointWorld(prevPos), map.TransformPointWorld(pos), c, 1.0f); }
		if(prevPos == pos) {
			return true;
		} else if(t >= endT) {
//			if(pos.x==1&&pos.y==0&&pos.z==0) Debug.Log("T passed "+endT+" without reaching "+pos+", got "+prevPos);
		}
		return false;
	}

	public IEnumerable<PathNode> ArcReachableTilesAround(
		Vector3 here,
		Dictionary<Vector3, PathNode> pickables,
		float maxRadius,
		bool provideAllTiles
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
			if(FindArcFromTo(startPos, pos, dir, d, theta1, v, g, dt, pickables, provideAllTiles)) {
				posPn.velocity = v;
				posPn.altitude = theta1;
				ret.Add(posPn);
			} else if(FindArcFromTo(startPos, pos, dir, d, theta2, v, g, dt, pickables, provideAllTiles)) {
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