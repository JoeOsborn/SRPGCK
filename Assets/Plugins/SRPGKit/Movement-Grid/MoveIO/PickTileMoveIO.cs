using UnityEngine;
using System.Collections.Generic;

public class PickTileMoveIO : MoveIO, ITilePickerOwner {
	public bool supportKeyboard = true;
	public bool supportMouse = true;
	
	public bool requireConfirmation = true;
	public bool RequireConfirmation { get { return requireConfirmation; } }
	
	public bool AwaitingConfirmation {
		get { return tilePicker.awaitingConfirmation; }
		set { tilePicker.awaitingConfirmation = value; }
	}
	
	public GridOverlay overlay;
	public float indicatorCycleLength=1.0f;
	
	TilePicker tilePicker;
	
	override public void Start() {
		base.Start();
		tilePicker = new TilePicker();
		tilePicker.owner = this;
	}
	
	override public void Update () {
		base.Update();
		if(character == null || !character.isActive) { return; }
		if(!isActive) { return; }
		if(!map.arbiter.IsLocalPlayer(character.EffectiveTeamID)) {
			return;
		}
		if(GUIUtility.hotControl != 0) { return; }
		MoveExecutor me = GetComponent<MoveExecutor>();
		if(me.IsMoving) { return; }
		tilePicker.supportKeyboard      = supportKeyboard;
		tilePicker.supportMouse         = supportMouse;
		tilePicker.requireConfirmation  = requireConfirmation;
		tilePicker.indicatorCycleLength = indicatorCycleLength;
		tilePicker.moveExecutor         = me;
		tilePicker.Update();
	}
	
	public Vector3 IndicatorPosition {
		get { return tilePicker.IndicatorPosition; }
	}	
	
	override protected void PresentMoves() {
		base.PresentMoves();
		tilePicker.requireConfirmation = requireConfirmation;
		tilePicker.map = map;
		GridMoveStrategy ms = GetComponent<GridMoveStrategy>();
		PathNode[] destinations = ms.GetValidMoves();
		MoveExecutor me = GetComponent<MoveExecutor>();
		Vector3 charPos = map.InverseTransformPointWorld(me.position);
		overlay = map.PresentGridOverlay(
			"move", this.gameObject.GetInstanceID(), 
			new Color(0.2f, 0.3f, 0.9f, 0.7f),
			new Color(0.4f, 0.6f, 0.9f, 0.85f),
			destinations
		);
		tilePicker.PresentOverlay(overlay, charPos);
	}
	
	override protected void FinishMove() {
		overlay = null;
		tilePicker.Clear();
		if(map.IsShowingOverlay("move", this.gameObject.GetInstanceID())) {
			map.RemoveOverlay("move", this.gameObject.GetInstanceID());
		}	
		base.FinishMove();
	}
	
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
	
	public override void TemporaryMoveToPathNode(PathNode pn) {
		FocusOnPoint(pn.pos);
		base.TemporaryMoveToPathNode(pn);
	}

	public override void IncrementalMoveToPathNode(PathNode pn) {
		FocusOnPoint(pn.pos);
		base.IncrementalMoveToPathNode(pn);
	}
	
	public override void PerformMoveToPathNode(PathNode pn) {
		FocusOnPoint(pn.pos);
		base.PerformMoveToPathNode(pn);
	}	
	
}
