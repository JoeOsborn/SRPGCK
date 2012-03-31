using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("SRPGCK/Character/Skillset")]
public class Skillset : MonoBehaviour {
	public SkillDef[] skills;

	public void Awake() {
		//make sure we don't mess with the real asset
		for(int i = 0; i < skills.Length; i++) {
			SkillDef def = skills[i];
			if(def != null && def.reallyDefined) {
				def = Instantiate(def) as SkillDef;
				def.Owner = this;
				skills[i] = def;
			}
		}
	}

	public void Start() {
		foreach(SkillDef def in skills) {
			def.Start();
		}
	}
	public void Update() {
		foreach(SkillDef def in skills) {
			def.Update();
		}
	}
}
