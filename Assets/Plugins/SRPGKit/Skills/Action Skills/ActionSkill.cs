using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("SRPGCK/Character/Skills/Action")]
public class ActionSkill : Skill {
	//def properties, remove after upgrading
	public SkillIO _io;
	public StatEffectGroup applicationEffects;
	public StatEffectGroup[] targetEffects;
	public Formula delay;
	public MultiTargetMode multiTargetMode = MultiTargetMode.Single;
	public bool waypointsAreIncremental=false;
	public bool canCancelWaypoints=true;
	public TargetSettings[] _targetSettings;
	public Formula maxWaypointDistanceF;
	//end def properties


}
