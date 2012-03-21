using UnityEngine;

public class SkillIO {
	public bool editorShow=false;
	
	//io
	public bool supportKeyboard = true;
	public bool supportMouse = true;
	public bool requireConfirmation = true;
	//if lockToGrid
	public float keyboardMoveSpeed=10.0f;
	public float indicatorCycleLength=1.0f;
	//lines
	public Material pathMaterial;
	//probe
	public CharacterController probePrefab;
	//TODO: support for inverting grid-locked overlay
	public bool invertOverlay = false;

	//FIXME: augment with facingLock
	public bool lockToGrid=true;
	public bool performTemporaryStepsImmediately=false;
	public bool performTemporaryStepsOnConfirmation=true;

	public Color overlayColor = new Color(0.6f, 0.3f, 0.2f, 0.7f);
	public Color highlightColor = new Color(0.9f, 0.6f, 0.4f, 0.85f);
	//TODO: infer from region
	public RadialOverlayType overlayType = RadialOverlayType.Sphere;
	public bool drawOverlayRim = false;
	public bool drawOverlayVolume = false;
}