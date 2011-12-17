using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PickTileMoveIO : MoveIO, ITilePickerOwner {
	public bool RequireConfirmation { get { return requireConfirmation; } }
	
	public bool AwaitingConfirmation {
		get { return tilePicker.AwaitingConfirmation; }
		set { tilePicker.AwaitingConfirmation = value; }
	}
	
	public GridOverlay overlay;
	
	[SerializeField]
	TilePicker tilePicker;
	
	override public void Activate() {
		base.Activate();
		tilePicker = new TilePicker();
		tilePicker.owner = this;
	}
	
	override public void Update () {
		base.Update();
		tilePicker.owner = this;
		if(owner.character == null || !owner.character.isActive) { return; }
		if(!isActive) { return; }
		if(!owner.arbiter.IsLocalPlayer(owner.character.EffectiveTeamID)) {
			return;
		}
		if(GUIUtility.hotControl != 0) { return; }
		MoveExecutor me = owner.Executor;
		if(me.IsMoving) { return; }
		tilePicker.supportKeyboard      = supportKeyboard;
		tilePicker.supportMouse         = supportMouse;
		tilePicker.requireConfirmation  = requireConfirmation;
		tilePicker.indicatorCycleLength = indicatorCycleLength;
		tilePicker.moveExecutor         = me;
		tilePicker.Update();
	}
	
	public Map Map { get { return owner.map; } }
	
	public void CancelPick(TilePicker tp) {
		owner.Cancel();
	}
	
	public Vector3 IndicatorPosition {
		get { return tilePicker.IndicatorPosition; }
	}	
	
	override public void PresentMoves() {
		base.PresentMoves();
		tilePicker.requireConfirmation = requireConfirmation;
		tilePicker.map = owner.map;
		PathNode[] destinations = owner.Strategy.GetValidMoves();
		MoveExecutor me = owner.Executor;
		Vector3 charPos = owner.map.InverseTransformPointWorld(me.position);
		overlay = owner.map.PresentGridOverlay(
			owner.skillName, owner.character.gameObject.GetInstanceID(), 
			new Color(0.2f, 0.3f, 0.9f, 0.7f),
			new Color(0.4f, 0.6f, 0.9f, 0.85f),
			destinations
		);
		tilePicker.PresentOverlay(overlay, charPos);
	}
	
	override public void Deactivate() {
		overlay = null;
		tilePicker.Clear();
		if(owner.map.IsShowingOverlay(owner.skillName, owner.character.gameObject.GetInstanceID())) {
			owner.map.RemoveOverlay(owner.skillName, owner.character.gameObject.GetInstanceID());
		}	
	}	
	public void FocusOnPoint(Vector3 pos) {
		tilePicker.FocusOnPoint(pos);
	}

	public void TentativePick(TilePicker tp, Vector3 p) {
		owner.TemporaryMove(p);
	}	
	
	public void TentativePick(TilePicker tp, PathNode pn) {
		owner.TemporaryMoveToPathNode(pn);
	}
	
	public void Pick(TilePicker tp, Vector3 p) {
		owner.PerformMove(p);	
	}
	
	public void Pick(TilePicker tp, PathNode pn) {
		owner.PerformMoveToPathNode(pn);	
	}	
}
