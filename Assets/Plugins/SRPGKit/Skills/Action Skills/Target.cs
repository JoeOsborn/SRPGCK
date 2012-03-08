using UnityEngine;

[System.Serializable]
public class Target {
	public PathNode path;
	public Character character;
	public int subregion=-1;
	//nullables aren't serializable, so we have to get fancy
	public Quaternion? facing {
		get {
			if(_hasQuaternion) { return _facing; }
			return null;
		}
		set {
			if(value != null) {
				_facing = value.Value;
				_hasQuaternion = true;
			}
			else { _hasQuaternion = false; }
		}
	}
	[SerializeField]
	protected Quaternion _facing;
	[SerializeField]
	protected bool _hasQuaternion=false;
	public Target() {

	}
	public override string ToString() {
		string outp = "Target:";
		if(path != null) {
			outp += " path:"+path;
		}
		if(character != null) {
			outp += " char:"+character;
		}
		if(subregion != -1) {
			outp += " subr:"+subregion;
		}
		if(facing != null) {
			outp += " face:"+facing;
		}
		return outp;
	}
	public Target Clone() {
		Target t = new Target();
		t.path = path;
		t.character = character;
		t.subregion = subregion;
		t.facing = facing;
		return t;
	}
	public Vector3 Position { get {
		if(path != null) { return path.pos; }
		if(character != null) { return character.TilePosition; }
		return new Vector3(-1,-1,-1);
	} }
	public Target Path(PathNode pn) {
		path = pn;
		return this;
	}
	public Target Path(Vector3 v) {
		return Path(new PathNode(v, null, 0));
	}
	public Target Character(Character c) {
		character = c;
		return this;
	}
	public Target Facing(float f) {
		return Facing(Quaternion.Euler(0,f,0));
	}
	public Target Facing(Quaternion? q) {
		facing = q;
		return this;
	}
	public Target Subregion(int idx) {
		subregion = idx;
		return this;
	}
}