using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("SRPGCK/Character/Skill")]
public class Skill : MonoBehaviour {
	public SkillDef def;

	public void Awake() {
		//make sure we don't mess with the real asset
		if(def != null && def.reallyDefined) {
			def = Instantiate(def) as SkillDef;
			def.Owner = this;
		}
	}

	public void Start() {
		def.Start();
	}
	public void Update() {
		def.Update();
	}
}