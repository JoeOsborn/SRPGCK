using UnityEngine;

[System.Serializable]
public class Target {
	public PathNode path, tile;
	public Character character;
	public int subregion=-1;
	public Quaternion? facing;
	public Target() {

	}
	public Target Clone() {
		Target t = new Target();
		t.path = path;
		t.tile = tile;
		t.character = character;
		t.subregion = subregion;
		t.facing = facing;
		return t;
	}
	public Vector3 Position { get {
		if(path != null) { return path.pos; }
		if(tile != null) { return tile.pos; }
		if(character != null) { return character.TilePosition; }
		return new Vector3(-1,-1,-1);
	} }
	public Target Path(PathNode pn) {
		path = pn;
		return this;
	}
	public Target Tile(PathNode pn) {
		tile = pn;
		return this;
	}
	public Target Tile(Vector3 v) {
		return Tile(new PathNode(v, null, 0));
	}
	public Target Character(Character c) {
		character = c;
		return this;
	}
	public Target Facing(Quaternion q) {
		facing = q;
		return this;
	}
	public Target Facing(float f) {
		return Facing(Quaternion.Euler(0,f,0));
	}
	public Target Subregion(int idx) {
		subregion = idx;
		return this;
	}
}