using UnityEngine;

//for each waypoint
//target settings:
[System.Serializable]
public class TargetSettings : ScriptableObject {
	//editor only
	public bool showInEditor=true;

	public Skill owner;
	public Skill Owner {
		get { return owner; }
		set {
			owner = value;
			if(targetRegion == null) { targetRegion = ScriptableObject.CreateInstance<Region>(); }
			if(effectRegion == null) { effectRegion = ScriptableObject.CreateInstance<Region>(); }
			targetRegion.Owner = owner;
			effectRegion.Owner = owner;
			effectRegion.IsEffectRegion = true;
		}
	}
	public Formulae fdb { get {
		if(owner != null) { return owner.fdb; }
		return Formulae.DefaultFormulae;
	} }

	public TargetingMode targetingMode = TargetingMode.Pick;
	//tile generation region (line/range/cone/etc)
	public Region targetRegion, effectRegion;
	public bool displayUnimpededTargetRegion=false;

	public bool allowsCharacterTargeting=false;
	public float newNodeThreshold=0.05f;
	public bool immediatelyExecuteDrawnPath=false;
	public Formula rotationSpeedXYF;
	public float rotationSpeedXY { get {
		return rotationSpeedXYF.GetValue(fdb, owner, null, null);
	} }

	public bool IsPickOrPath { get {
		return
			targetingMode == TargetingMode.Pick ||
			targetingMode == TargetingMode.SelectRegion ||
			targetingMode == TargetingMode.Path;
	} }

	public bool ShouldDrawPath { get {
		return targetingMode == TargetingMode.Path;
	} }

	public bool DeferPathRegistration { get {
		return !(ShouldDrawPath || immediatelyExecuteDrawnPath);
	} }

	public TargetSettings() {

	}
}
