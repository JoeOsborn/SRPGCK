using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Skill : MonoBehaviour {
	public bool isPassive=false;
	[HideInInspector]
	public bool isActive=false;
	public string skillName;
	
	public StatEffect[] passiveEffects;
	
	public List<string> parameterNames;
	public List<Formula> parameterFormulae;

	Dictionary<string, Formula> runtimeParameters;

	public virtual void Start() {
		
	}
	public virtual void ActivateSkill() {
		if(isPassive) { return; }
		isActive = true;
	}
	public virtual void DeactivateSkill() {
		if(isPassive) { return; }
		isActive = false;
	}
	public virtual void Update() {
		
	}
	public virtual void Cancel() {
		if(isPassive) { return; }
		DeactivateSkill();
	}
	
	public virtual void Reset() {
		
	}
	public virtual void ApplySkill() {
		map.BroadcastMessage("SkillApplied", this, SendMessageOptions.DontRequireReceiver);
		DeactivateSkill();	
	}
	
	void MakeParametersIfNecessary() {
		if(runtimeParameters == null) {
			runtimeParameters = new Dictionary<string, Formula>();
			for(int i = 0; i < parameterNames.Count; i++) {
				runtimeParameters.Add(parameterNames[i], parameterFormulae[i]);
			}
		}
	}
	
	public float GetParam(string pname) {
		MakeParametersIfNecessary();
		return runtimeParameters[pname].GetValue(this);
	}
	
	public Vector3 transformOffset { get { 
		return character.transformOffset; 
	} }
	public Character character { get { return GetComponent<Character>(); } }
	public Map map { get { return character.transform.parent.GetComponent<Map>(); } }
	public Scheduler scheduler { get { return this.map.scheduler; } }
	public Arbiter arbiter { get { return this.map.arbiter; } }
}