using UnityEngine;
using System.Collections;

public class DebugGUI : MonoBehaviour {
	Texture2D areaBGTexture;
	
	public void Start() {
		areaBGTexture = new Texture2D(1,1);
		areaBGTexture.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.8f, 0.5f));
		areaBGTexture.Apply();		
	}
	
	protected void OnGUIConfirmation(string msg, out bool yesButton, out bool noButton) {
		GUIStyle bgStyle = new GUIStyle();
		bgStyle.normal.background = areaBGTexture;
		GUILayout.BeginArea(new Rect(
			Screen.width/2-64, Screen.height/2-32, 
			128, 64
		), bgStyle); {
		  GUILayout.BeginVertical(); {
		    GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
		    centeredStyle.alignment = TextAnchor.MiddleCenter;
		    GUILayout.Label(msg, centeredStyle);
		    GUILayout.BeginHorizontal(); {
		      if(GUILayout.Button("No")) {
						noButton = true;
		      } else {
						noButton = false;
					}
					if(GUILayout.Button("Yes")) {
						yesButton = true;
		      } else {
						yesButton = false;
					}
		    } GUILayout.EndHorizontal();
		  } GUILayout.EndVertical();
		} GUILayout.EndArea();	
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
			//TODO:0:0: confirmation for action skills, e.g. AttackSkill
			if(ms.isActive && io != null) {
				if(io is PickTileMoveIO) {
					PickTileMoveIO mio = io as PickTileMoveIO;
					if(mio.isActive && a.IsLocalPlayer(s.activeCharacter.EffectiveTeamID)) {
			  		MoveExecutor me = ms.executor;
						if(!me.IsMoving) {
							if(mio.RequireConfirmation && 
								 mio.AwaitingConfirmation) {
								bool yesButton=false, noButton=false;
								OnGUIConfirmation("Move here?", out yesButton, out noButton);
								if(yesButton) {
									PathNode pn = mio.overlay.PositionAt(mio.IndicatorPosition);
					      	mio.PerformMoveToPathNode(pn);
					      	mio.AwaitingConfirmation = false;
								}
								if(noButton) {
					      	mio.AwaitingConfirmation = false;
					      	mio.TemporaryMove(map.InverseTransformPointWorld(me.position));
								}
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
						bool yesButton=false, noButton=false;
						OnGUIConfirmation("Wait here?", out yesButton, out noButton);
						if(yesButton) {
			  	  	wio.AwaitingConfirmation = false;
							ws.FinishWaitPick();
						}
						if(noButton) {
			      	wio.AwaitingConfirmation = false;
						}
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
			foreach(Skill skill in s.activeCharacter.GetComponents<Skill>()) {
				if(skill.isActive && skill is AttackSkill) {
					AttackSkill ask = skill as AttackSkill;
					ActionIO aio = ask.io;
					if(ask.isActive && aio != null) {
						if(a.IsLocalPlayer(s.activeCharacter.EffectiveTeamID)) {
							if(aio.RequireConfirmation && 
								 aio.AwaitingConfirmation) {
								bool yesButton=false, noButton=false;
								OnGUIConfirmation("Confirm?", out yesButton, out noButton);
								if(yesButton) {
					  	  	aio.AwaitingConfirmation = false;
									ask.ApplySkill();
								}
								if(noButton) {
					      	aio.AwaitingConfirmation = false;
								}
							} else {
						  	showCancelButton = false;
							}
							showAnySchedulerButtons = false;
						}
					}			
				}
			}
		}
		if(s is CTScheduler) {
			if(s.activeCharacter != null && a.IsLocalPlayer(s.activeCharacter.EffectiveTeamID)) {
				GUILayout.BeginArea(new Rect(
					8, 8, 
					128, 150
				));
				GUILayout.Label("Current Character:");
				GUILayout.Label(s.activeCharacter.gameObject.name);
				CTCharacter ctc = s.activeCharacter.GetComponent<CTCharacter>();
				GUILayout.Label("CT: "+Mathf.Floor(ctc.CT));
				
				//TODO:0: support skills
				//show list of skills
				Skill activeSkill = null;
				Skill[] skills = s.activeCharacter.GetComponents<Skill>();
				for(int i = 0; i < skills.Length; i++) {
					if(skills[i].isActive) {
						activeSkill = skills[i];
						break;
					}
				}
				if(activeSkill == null) {
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
				} else if(showCancelButton) {
					if(GUILayout.Button("Cancel "+activeSkill.skillName)) {
						activeSkill.Cancel();
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
						tps.activeCharacter.moveSkill.ApplySkill();
					}
				}
				GUILayout.EndArea();
			}
		}
	}
}