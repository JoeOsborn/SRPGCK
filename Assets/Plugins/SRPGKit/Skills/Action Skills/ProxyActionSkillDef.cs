using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum MergeModeList {
	UseOriginal,
	UseProxy,
	Combine
}

public enum MergeMode {
	UseOriginal,
	UseProxy
}

public class ProxyActionSkillDef : ActionSkillDef {
	public string referredSkillGroup=null;
	public string referredSkillName="Attack";
	public ActionSkillDef referredSkill=null;

	ActionSkillDef ReferredSkill { get {
		return referredSkill ?? 
			(character.GetSkill(referredSkillGroup, referredSkillName) as ActionSkillDef);
	} }

	public MergeModeList mergePassiveEffects=MergeModeList.Combine;
	public override StatEffect[] PassiveEffects {
		get {
			switch(mergePassiveEffects) {
				case MergeModeList.UseOriginal:
					return ReferredSkill.PassiveEffects;
				case MergeModeList.UseProxy:
					return base.PassiveEffects;
				case MergeModeList.Combine:
				default:
					if(passiveEffects == null) { return ReferredSkill.PassiveEffects; }
					return passiveEffects.Concat(ReferredSkill.PassiveEffects).ToArray();
			}
		}
	}

	public MergeModeList mergeParameters=MergeModeList.Combine;
	public override bool HasParam(string pname) {
		MakeParametersIfNecessary();
		switch(mergeParameters) {
			case MergeModeList.UseOriginal:
				return ReferredSkill.HasParam(pname);
			case MergeModeList.UseProxy:
				return base.HasParam(pname);
			case MergeModeList.Combine:
			default:
				return base.HasParam(pname) || ReferredSkill.HasParam(pname);
		}
	}

	public override float GetParam(string pname, float fallback=float.NaN, SkillDef parentCtx=null) {
		MakeParametersIfNecessary();
		switch(mergeParameters) {
			case MergeModeList.UseOriginal:
				return ReferredSkill.GetParam(pname, fallback, parentCtx ?? this);
			case MergeModeList.UseProxy:
				return base.GetParam(pname, fallback, parentCtx);
			case MergeModeList.Combine:
			default:
				//use proxy if available, else referred
				return runtimeParameters.ContainsKey(pname) ?
					base.GetParam(pname, fallback, parentCtx) :
					ReferredSkill.GetParam(pname, fallback, parentCtx ?? this);
		}
	}

	public MergeMode mergeIsEnabledF=MergeMode.UseOriginal;
	public override Formula IsEnabledF { get {
		switch(mergeIsEnabledF) {
			case MergeMode.UseOriginal:
				return ReferredSkill.IsEnabledF;
			case MergeMode.UseProxy:
			default:
				return base.IsEnabledF;
		}
	} }

	public MergeMode mergeInvolvedItem=MergeMode.UseOriginal;
	public override Item InvolvedItem { get { 
		switch(mergeInvolvedItem) {
			case MergeMode.UseOriginal:
				return ReferredSkill.InvolvedItem;
			case MergeMode.UseProxy:
			default:
				return base.InvolvedItem;
		}
	} }

	public MergeMode mergeIO=MergeMode.UseOriginal;

	public override SkillIO io {
		get {
			switch(mergeIO) {
				case MergeMode.UseOriginal:
					return ReferredSkill.io;
				case MergeMode.UseProxy:
				default:
					return base.io;
			}
		}
		set { base.io = value; }
	}

	public MergeMode mergeDelay=MergeMode.UseOriginal;
	public override Formula Delay { get {
		switch(mergeDelay) {
			case MergeMode.UseOriginal:
				return ReferredSkill.Delay;
			case MergeMode.UseProxy:
			default:
				return base.Delay;
		}
	} }

	public MergeMode mergeDelayedApplicationUsesOriginalPosition=MergeMode.UseOriginal;
	public override bool DelayedApplicationUsesOriginalPosition { get {
		switch(mergeDelayedApplicationUsesOriginalPosition) {
			case MergeMode.UseOriginal:
				return ReferredSkill.DelayedApplicationUsesOriginalPosition;
			case MergeMode.UseProxy:
			default:
				return base.DelayedApplicationUsesOriginalPosition;
		}
	} }

	public MergeMode mergeMultiTargetMode=MergeMode.UseOriginal;
	public override MultiTargetMode MultiTargetMode { get {
		switch(mergeMultiTargetMode) {
			case MergeMode.UseOriginal:
				return ReferredSkill.MultiTargetMode;
			case MergeMode.UseProxy:
			default:
				return base.MultiTargetMode;
		}
	} }

	public MergeMode mergeWaypointsAreIncremental=MergeMode.UseOriginal;
	public override bool WaypointsAreIncremental { get {
		switch(mergeWaypointsAreIncremental) {
			case MergeMode.UseOriginal:
				return ReferredSkill.WaypointsAreIncremental;
			case MergeMode.UseProxy:
			default:
				return base.WaypointsAreIncremental;
		}
	} }

	public MergeMode mergeCanCancelWaypoints=MergeMode.UseOriginal;
	public override bool CanCancelWaypoints { get {
		switch(mergeCanCancelWaypoints) {
			case MergeMode.UseOriginal:
				return ReferredSkill.CanCancelWaypoints;
			case MergeMode.UseProxy:
			default:
				return base.CanCancelWaypoints;
		}
	} }

	public MergeMode mergeTurnToFaceTarget=MergeMode.UseOriginal;
	public override bool TurnToFaceTarget { get {
		switch(mergeTurnToFaceTarget) {
			case MergeMode.UseOriginal:
				return ReferredSkill.TurnToFaceTarget;
			case MergeMode.UseProxy:
			default:
				return base.TurnToFaceTarget;
		}
	} }

	public MergeMode mergeTargetSettings=MergeMode.UseOriginal;
	public override TargetSettings[] TargetSettings {
		get {
			switch(mergeTargetSettings) {
				case MergeMode.UseOriginal:
					return ReferredSkill.TargetSettings;
				case MergeMode.UseProxy:
				default:
					return base.TargetSettings;
			}
		}
	}

	public MergeMode mergeMaxWaypointDistanceF=MergeMode.UseOriginal;
	public override float maxWaypointDistance { get {
		switch(mergeMaxWaypointDistanceF) {
			case MergeMode.UseOriginal:
				return ReferredSkill.maxWaypointDistance;
			case MergeMode.UseProxy:
			default:
				return base.maxWaypointDistance;
		}
	} }


	public MergeModeList mergeScheduledEffects=MergeModeList.UseOriginal;
	public override StatEffectGroup ScheduledEffects { get {
		switch(mergeScheduledEffects) {
			case MergeModeList.UseOriginal:
				return ReferredSkill.ScheduledEffects;
			case MergeModeList.UseProxy:
			default:
				return base.ScheduledEffects;
			case MergeModeList.Combine:
				return ReferredSkill.ScheduledEffects.Concat(base.ScheduledEffects);
		}
	} }

	public MergeModeList mergeApplicationEffects=MergeModeList.UseOriginal;
	public override StatEffectGroup ApplicationEffects { get {
		switch(mergeApplicationEffects) {
			case MergeModeList.UseOriginal:
				return ReferredSkill.ApplicationEffects;
			case MergeModeList.UseProxy:
			default:
				return base.ApplicationEffects;
			case MergeModeList.Combine:
				return ReferredSkill.ApplicationEffects.Concat(base.ApplicationEffects);
		}
	} }

	public MergeModeList mergeTargetEffects=MergeModeList.UseOriginal;
	public override StatEffectGroup[] TargetEffects { get {
		switch(mergeTargetEffects) {
			case MergeModeList.UseOriginal:
				return ReferredSkill.TargetEffects;
			case MergeModeList.UseProxy:
			default:
				return base.TargetEffects;
			case MergeModeList.Combine: {
				var refFX = ReferredSkill.TargetEffects;
				var baseFX = base.TargetEffects;
				int maxLen = System.Math.Max(refFX.Length, baseFX.Length);
				StatEffectGroup[] ret = new StatEffectGroup[maxLen];
				for(int i = 0; i < maxLen; i++) {
					if(i < refFX.Length && i < baseFX.Length) {
						ret[i] = refFX[i].Concat(baseFX[i]);
					} else if(i < refFX.Length) {
						ret[i] = refFX[i];
					} else if(i < baseFX.Length) {
						ret[i] = baseFX[i];
					} else {
						Debug.LogError("Somehow ran out of both stat effect groups");
					}
				}
				return ret;
			}
		}
	} }
}