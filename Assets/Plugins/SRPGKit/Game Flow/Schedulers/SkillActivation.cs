using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class SkillActivation {
	public bool applied=false;
	public float delay=-1, delayRemaining=-1;
	public Skill skill;
	public List<Target> targets;
	public SkillActivation(Skill s, List<Target> targs, float d) {
		skill = s;
		targets = targs.Select(t => t.Clone()).ToList();
		delay = d;
		delayRemaining = delay;
	}
	public void Apply() {
		(skill as ActionSkill).DelayedApply(targets);
		applied = true;
	}
}