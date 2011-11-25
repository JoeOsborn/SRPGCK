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
		bool showCancelButton = true;
		if(s.activeCharacter != null) {
			Map map = transform.parent.GetComponent<Map>();
			StandardPickTileMoveSkill ms = s.activeCharacter.moveSkill as StandardPickTileMoveSkill;
			MoveIO io = ms.io;
			if(ms.isActive && io != null) {
				if(io is PickTileMoveIO) {
					PickTileMoveIO mio = io as PickTileMoveIO;
					if(mio.isActive && a.IsLocalPlayer(s.activeCharacter.EffectiveTeamID)) {
			  		MoveExecutor me = ms.executor;
						if(!me.IsMoving) {
							if(mio.RequireConfirmation && 
								 mio.AwaitingConfirmation) {
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
								      	mio.AwaitingConfirmation = false;
								      	mio.TemporaryMove(map.InverseTransformPointWorld(me.position));
								      } else if(GUILayout.Button("Yes")) {
												PathNode pn = mio.overlay.PositionAt(mio.IndicatorPosition);
								      	mio.PerformMoveToPathNode(pn);
								      	mio.AwaitingConfirmation = false;
								      }
								    } GUILayout.EndHorizontal();
								  } GUILayout.EndVertical();
								} GUILayout.EndArea();
							} 
						} else {	
							showCancelButton = false;
						}
						showAnySchedulerButtons = false;
					}
				}/* else if(io is ContinuousWithinTilesMoveIO) {
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
				}*/
			}
			WaitSkill ws = s.activeCharacter.waitSkill as WaitSkill;
			WaitIO wio = ws.io;
			if(ws.isActive && wio != null) {
				if(a.IsLocalPlayer(s.activeCharacter.EffectiveTeamID)) {
					if(wio.RequireConfirmation && 
						 wio.AwaitingConfirmation) {
						GUIStyle bgStyle = new GUIStyle();
						bgStyle.normal.background = areaBGTexture;
						GUILayout.BeginArea(new Rect(
							Screen.width/2-64, Screen.height/2-32, 
							128, 64
						), bgStyle); {
						  GUILayout.BeginVertical(); {
						    GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
						    centeredStyle.alignment = TextAnchor.MiddleCenter;
						    GUILayout.Label("Wait here?", centeredStyle);
						    GUILayout.BeginHorizontal(); {
						      if(GUILayout.Button("No")) {
						      	wio.AwaitingConfirmation = false;
						      } else if(GUILayout.Button("Yes")) {
						  	  	wio.AwaitingConfirmation = false;
										ws.FinishWaitPick();
						      }
						    } GUILayout.EndHorizontal();
						  } GUILayout.EndVertical();
						} GUILayout.EndArea();
					} else {
					  if(s is CTScheduler) {
					  	CTCharacter ctc = s.activeCharacter.GetComponent<CTCharacter>();
					  	showCancelButton = !(ctc.HasMoved && ctc.HasActed);
					  } else {
					  	showCancelButton = true;
					  }
					}
					showAnySchedulerButtons = false;
				}
			}
		}
		if(s is CTScheduler) {
			if(s.activeCharacter != null && a.IsLocalPlayer(s.activeCharacter.EffectiveTeamID)) {
				StandardPickTileMoveSkill ms = s.activeCharacter.moveSkill as StandardPickTileMoveSkill;
				WaitSkill ws = s.activeCharacter.waitSkill as WaitSkill;
				GUILayout.BeginArea(new Rect(
					8, 8, 
					128, 128
				));
				GUILayout.Label("Current Character:");
				GUILayout.Label(s.activeCharacter.gameObject.name);
				CTCharacter ctc = s.activeCharacter.GetComponent<CTCharacter>();
				GUILayout.Label("CT: "+Mathf.Floor(ctc.CT));
				
				//TODO:0: support skills that aren't ms
				//show list of skills
				if(!ms.isActive && !ws.isActive) {
					Skill[] skills = s.activeCharacter.GetComponents<Skill>();
					for(int i = 0; i < skills.Length; i++) {
						Skill skill = skills[i];
						if(!skill.isActive) {
							if((skill is MoveSkill && !ctc.HasMoved) ||
							   (!(skill is MoveSkill) && !ctc.HasActed) ||
								 skill is WaitSkill) {
								if(GUILayout.Button(skill.skillName)) {
									skill.ActivateSkill();
									break;
								}
							}
						}
					}
				}
				if((ms.isActive || ws.isActive) && showCancelButton) {
					if(GUILayout.Button("Cancel "+(ms.isActive ? ms.skillName : ws.skillName))) {
						if(ms.isActive) { ms.Cancel(); }
						if(ws.isActive) { ws.Cancel(); }
					}	
				}
				GUILayout.EndArea();
			}
		} else if(s is TeamRoundsPickAnyOnceScheduler) {
			TeamRoundsPickAnyOnceScheduler tps = s as TeamRoundsPickAnyOnceScheduler;
			if(a.IsLocalPlayer(tps.currentTeam)) {
				GUILayout.BeginArea(new Rect(
					8, 8, 
					96, 128
				));
				GUILayout.Label("Current Team:"+tps.currentTeam);
				if(showAnySchedulerButtons &&
					!(tps.activeCharacter != null && tps.activeCharacter.moveSkill.executor.IsMoving) && 
				  GUILayout.Button("End Round")) {
					tps.EndRound();
				}
				GUILayout.EndArea();
			}
		} else if(s is TeamRoundsPointsScheduler) {
			TeamRoundsPointsScheduler tps = s as TeamRoundsPointsScheduler;
			if(a.IsLocalPlayer(tps.currentTeam)) { 
				GUILayout.BeginArea(new Rect(
					8, 8, 
					96, 128
				));
				GUILayout.Label("Current Team: "+tps.currentTeam);
				GUILayout.Label("Points Left: "+tps.pointsRemaining);
				if(showAnySchedulerButtons &&
					!(tps.activeCharacter != null && tps.activeCharacter.moveSkill.executor.IsMoving) && 
				  GUILayout.Button("End Round")) {
					tps.EndRound();
				}
				if(showAnySchedulerButtons &&
					tps.activeCharacter != null && tps.activeCharacter.moveSkill.executor.IsMoving) {
					if(GUILayout.Button("End Move")) {
						tps.activeCharacter.moveSkill.DeactivateSkill();
					}
				}
				GUILayout.EndArea();
			}
		}
	}
}