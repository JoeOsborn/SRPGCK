using UnityEngine;

[System.Serializable]
public class StandardPickTileMoveSkill : MoveSkill {	
	[HideInInspector]
	public PickTileMoveIO moveIO;
		
	public override MoveIO IO { get {
		return moveIO;
	} }
	
	protected override void MakeIO() {
		moveIO = new PickTileMoveIO();	
	}
}