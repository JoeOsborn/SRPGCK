using UnityEngine;
using System.Collections;

public class PathDrawingMoveSkill : MoveSkill {
	[HideInInspector]
	public DrawPathMoveIO moveIO;
	
	public CharacterController probePrefab;
	
	public override MoveIO IO { get {
		return moveIO;
	} }
	protected override void MakeIO() {
		moveIO = new DrawPathMoveIO();
		moveIO.probePrefab = probePrefab;
	}	
}
