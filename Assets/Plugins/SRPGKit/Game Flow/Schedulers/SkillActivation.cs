using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class SkillActivation {
	public bool applied=false;
	public float delay=-1, delayRemaining=-1;
	public SkillDef skill;
	public List<Target> targets;
	public SkillActivation(SkillDef s, List<Target> targs, float d) {
		skill = s;
		targets = targs.Select(t => t.Clone()).ToList();
		delay = d;
		delayRemaining = delay;
	}
	public void Apply() {
		(skill as ActionSkillDef).DelayedApply(targets);
		applied = true;
	}
}