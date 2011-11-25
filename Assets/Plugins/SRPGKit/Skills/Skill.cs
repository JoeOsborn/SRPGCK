using UnityEngine;

[System.Serializable]
public class Skill : MonoBehaviour {
	public bool isActive=false;
	public string skillName;

	[HideInInspector]
	public string adder;
	
	public virtual void Start() {
		
	}
	public virtual void ActivateSkill() {
		isActive = true;
	}
	public virtual void DeactivateSkill() {
		isActive = false;
	}
	public virtual void Update() {
		
	}
	public virtual void Cancel() {
		DeactivateSkill();
	}
	
	public Character character { get { return GetComponent<Character>(); } }
	public Map map { get { return character.transform.parent.GetComponent<Map>(); } }
	public Scheduler scheduler { get { return this.map.scheduler; } }
	public Arbiter arbiter { get { return this.map.arbiter; } }
}