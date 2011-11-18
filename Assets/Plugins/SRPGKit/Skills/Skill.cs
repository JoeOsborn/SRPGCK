using UnityEngine;

[System.Serializable]
public class Skill : System.Object {
	public Character character;
	
	public bool isActive=false;
	public string name;
	
	public virtual void Start() {
		
	}
	public virtual void Activate() {
		isActive = true;
	}
	public virtual void Deactivate() {
		isActive = false;
	}
	public virtual void Update() {
		
	}
	public virtual void Cancel() {
		Deactivate();
	}
	
	public enum ApplicationMode {
		Replace,
		Augment
	}
	
	public virtual ApplicationMode Mode {
		get { return ApplicationMode.Replace; }
	}
	
	public Map map { get { return character.transform.parent.GetComponent<Map>(); } }
	public Scheduler scheduler { get { return this.map.scheduler; } }
	public Arbiter arbiter { get { return this.map.arbiter; } }
}