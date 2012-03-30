using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class SkillActivation {
	public bool applied=false;
	public float delay=-1, delayRemaining=-1;
	public SkillDef skill;
	public List<Target> targets;
	public Vector3? start;
	public SkillActivation(SkillDef s, Vector3? st, List<Target> targs, float d) {
		skill = s;
		start = st;
		targets = targs.Select(t => t.Clone()).ToList();
		delay = d;
		delayRemaining = delay;
	}
	public void Apply() {
		(skill as ActionSkillDef).DelayedApply(start, targets);
		applied = true;
	}
}