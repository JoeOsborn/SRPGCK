using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Skill : MonoBehaviour {
	public bool isPassive=false;
	[HideInInspector]
	public bool isActive=false;
	public string skillName;
	public string skillGroup="";
	public int skillSorting = 0;
	
	public StatEffect[] passiveEffects;
	
	public List<string> parameterNames;
	public List<Formula> parameterFormulae;

	Dictionary<string, Formula> runtimeParameters;
	
	public List<StatEffect> targetEffects;
	
	//only relevant to targeted skills, sadly
	[HideInInspector]
	[SerializeField]
	protected List<Character> targets;
	[HideInInspector]
	[SerializeField]
	protected Character currentTarget;

	public virtual void Start() {
		
	}
	public virtual void ActivateSkill() {
		if(isPassive) { return; }
		isActive = true;
	}
	public virtual void DeactivateSkill() {
		if(isPassive) { return; }
		isActive = false;
		targets = null;
		currentTarget = null;
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
				runtimeParameters.Add(parameterNames[i].NormalizeName(), parameterFormulae[i]);
			}
		}
	}
	
	public bool HasParam(string pname) {
		MakeParametersIfNecessary();
		return runtimeParameters.ContainsKey(pname);
	}
	
	public float GetParam(string pname) {
		MakeParametersIfNecessary();
		return runtimeParameters[pname].GetValue(this, currentTarget);
	}
	
	public Vector3 transformOffset { get { 
		return character.transformOffset; 
	} }
	public Character character { get { return GetComponent<Character>(); } }
	public Map map { get { return character.transform.parent.GetComponent<Map>(); } }
	public Scheduler scheduler { get { return this.map.scheduler; } }
	public Arbiter arbiter { get { return this.map.arbiter; } }
}