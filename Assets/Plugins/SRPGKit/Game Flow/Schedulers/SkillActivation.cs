using UnityEngine;

[System.Serializable]
public class SkillActivation {
	public bool applied=false;
	public float delay=-1, delayRemaining=-1;
	public Skill skill;
	public Target target;
	public SkillActivation(Skill s, Target t, float d) {
		skill = s;
		target = t;
		delay = d;
		delayRemaining = delay;
	}
	public void Apply() {
		(skill as ActionSkill).DelayedApply(target);
		applied = true;
	}
}