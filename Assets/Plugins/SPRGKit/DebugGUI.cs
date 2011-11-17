using UnityEngine;
using System.Collections;

public class DebugGUI : MonoBehaviour {
	Texture2D areaBGTexture;
	
	public void Start() {
		areaBGTexture = new Texture2D(1,1);
		areaBGTexture.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.8f, 0.5f));
		areaBGTexture.Apply();		
	}
	
	public void OnGUI() {
		Scheduler s = GetComponent<Scheduler>();
		Arbiter a = GetComponent<Arbiter>();
		bool showAnySchedulerButtons = true;
		if(s.activeCharacter != null) {
			Map map = transform.parent.GetComponent<Map>();
			MoveIO io = s.activeCharacter.GetComponent<MoveIO>();		
			if(io != null) {
				if(io as PickTileMoveIO) {
					PickTileMoveIO mio = io as PickTileMoveIO;
					if(mio.isActive && a.IsLocalPlayer(mio.character.EffectiveTeamID)) {
			  		MoveExecutor me = mio.GetComponent<MoveExecutor>();
						if(!me.IsMoving &&
							mio.requireConfirmation && 
							mio.awaitingConfirmation) {
							GUIStyle bgStyle = new GUIStyle();
							bgStyle.normal.background = areaBGTexture;
							GUILayout.BeginArea(new Rect(
								Screen.width/2-64, Screen.height/2-32, 
								128, 64
							), bgStyle); {
							  GUILayout.BeginVertical(); {
							    GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
							    centeredStyle.alignment = TextAnchor.MiddleCenter;
							    GUILayout.Label("Move here?", centeredStyle);
							    GUILayout.BeginHorizontal(); {
							      if(GUILayout.Button("No")) {
							      	mio.awaitingConfirmation = false;
							      	mio.TemporaryMove(map.InverseTransformPointWorld(me.position));
							      } else if(GUILayout.Button("Yes")) {
											PathNode pn = mio.overlay.PositionAt(mio.IndicatorPosition);
							      	mio.PerformMoveToPathNode(pn);
							      	mio.awaitingConfirmation = false;
							      }
							    } GUILayout.EndHorizontal();
							  } GUILayout.EndVertical();
							} GUILayout.EndArea();
							showAnySchedulerButtons = false;
						}
					}
				} else if(io as ContinuousWithinTilesMoveIO) {
					ContinuousWithinTilesMoveIO mio = io as ContinuousWithinTilesMoveIO;
					if(mio.character != null && 
						mio.character.isActive &&
						a.IsLocalPlayer(mio.character.EffectiveTeamID) && 
						mio.supportMouse) {
						GUILayout.BeginArea(new Rect(
							Screen.width/2-48, Screen.height-32, 
							96, 24
						));
						if(GUILayout.Button("End Move")) {
							mio.PerformMove(mio.moveDest);
						}
						GUILayout.EndArea();
						showAnySchedulerButtons = false;
					}
				}
			}
		}
		if(s as CTScheduler) {
			if(s.activeCharacter != null && a.IsLocalPlayer(s.activeCharacter.EffectiveTeamID)) {
				GUILayout.BeginArea(new Rect(
					8, 8, 
					128, 128
				));
				GUILayout.Label("Current Character:");
				GUILayout.Label(s.activeCharacter.gameObject.name);
				CTCharacter ctc = s.activeCharacter.GetComponent<CTCharacter>();
				GUILayout.Label("CT: "+Mathf.Floor(ctc.CT));
				if(showAnySchedulerButtons &&
					!s.activeCharacter.GetComponent<MoveExecutor>().IsMoving && 
				  GUILayout.Button("End Turn")) {
					s.Deactivate(s.activeCharacter);
				}
				GUILayout.EndArea();
			}
		} else if(s as TeamRoundsPickAnyOnceScheduler) {
			TeamRoundsPickAnyOnceScheduler tps = s as TeamRoundsPickAnyOnceScheduler;
			if(a.IsLocalPlayer(tps.currentTeam)) {
				GUILayout.BeginArea(new Rect(
					8, 8, 
					96, 128
				));
				GUILayout.Label("Current Team:"+tps.currentTeam);
				if(showAnySchedulerButtons &&
					!(tps.activeCharacter != null && tps.activeCharacter.GetComponent<MoveExecutor>().IsMoving) && 
				  GUILayout.Button("End Round")) {
					tps.EndRound();
				}
				GUILayout.EndArea();
			}
		} else if(s as TeamRoundsPointsScheduler) {
			TeamRoundsPointsScheduler tps = s as TeamRoundsPointsScheduler;
			if(a.IsLocalPlayer(tps.currentTeam)) { 
				GUILayout.BeginArea(new Rect(
					8, 8, 
					96, 128
				));
				GUILayout.Label("Current Team: "+tps.currentTeam);
				GUILayout.Label("Points Left: "+tps.pointsRemaining);
				if(showAnySchedulerButtons &&
					!(tps.activeCharacter != null && tps.activeCharacter.GetComponent<MoveExecutor>().IsMoving) && 
				  GUILayout.Button("End Round")) {
					tps.EndRound();
				}
				if(showAnySchedulerButtons &&
					tps.activeCharacter != null && tps.activeCharacter.GetComponent<MoveExecutor>().IsMoving) {
					if(GUILayout.Button("End Move")) {
						tps.EndMovePhase(tps.activeCharacter);
					}
				}
				GUILayout.EndArea();
			}
		}
	}
}