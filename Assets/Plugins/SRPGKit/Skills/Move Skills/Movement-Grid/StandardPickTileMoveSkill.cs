using UnityEngine;

[System.Serializable]
public class StandardPickTileMoveSkill : MoveSkill, ITilePickerOwner {
	public GridOverlay overlay;
	
	[SerializeField]
	TilePicker tilePicker;
	
	override public void ActivateSkill() {
		tilePicker = new TilePicker();
		tilePicker.owner = this;
		base.ActivateSkill();
	}
	
	public override void DeactivateSkill() {
		if(isActive) {
			overlay = null;
			if(map.IsShowingOverlay(skillName, character.gameObject.GetInstanceID())) {
				map.RemoveOverlay(skillName, character.gameObject.GetInstanceID());
			}	
		}
		base.DeactivateSkill();
	}
	
	override public void Update () {
		base.Update();
		tilePicker.owner = this;
		if(character == null || !character.isActive) { return; }
		if(!isActive) { return; }
		if(!arbiter.IsLocalPlayer(character.EffectiveTeamID)) {
			return;
		}
		if(GUIUtility.hotControl != 0) { return; }
		MoveExecutor me = Executor;
		if(me.IsMoving) { return; }
		tilePicker.Update();
	}
	
	public GridOverlay Overlay { get { return overlay; } }
	public Map Map { get { return map; } }
	public bool SupportKeyboard { get { return supportKeyboard; } }
	public bool SupportMouse { get { return supportMouse; } }
	public float IndicatorCycleLength { get { return indicatorCycleLength; } }

	public void FocusOnPoint(Vector3 pos) {
		tilePicker.FocusOnPoint(pos);
	}

	public void TentativePick(TilePicker tp, Vector3 p) {
		TemporaryMove(p);
	}	
	
	public void TentativePick(TilePicker tp, PathNode pn) {
		TemporaryMoveToPathNode(pn);
	}
	
	public void Pick(TilePicker tp, Vector3 p) {
		PerformMove(p);	
	}
	
	public void Pick(TilePicker tp, PathNode pn) {
		PerformMoveToPathNode(pn);	
	}	
	
	override protected void PresentMoves() {
		base.PresentMoves();
		PathNode[] destinations = Strategy.GetValidMoves();
		MoveExecutor me = Executor;
		Vector3 charPos = map.InverseTransformPointWorld(me.position);
		overlay = map.PresentGridOverlay(
			skillName, character.gameObject.GetInstanceID(), 
			new Color(0.2f, 0.3f, 0.9f, 0.7f),
			new Color(0.4f, 0.6f, 0.9f, 0.85f),
			destinations
		);
		awaitingConfirmation = false;
		tilePicker.FocusOnPoint(charPos);
	}
	
	public void CancelPick(TilePicker tp) {
		Cancel();
	}
	
	public Vector3 IndicatorPosition {
		get { return tilePicker.IndicatorPosition; }
	}	
}