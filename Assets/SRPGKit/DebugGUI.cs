using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class DebugGUI : MonoBehaviour {
	Texture2D areaBGTexture;
	[SerializeField]
	List<string> selectedGroup;

	bool activeCharacterHasMoved;
	bool activeCharacterHasActed;
	public bool permitMultipleMoves = false;
	public bool permitMultipleActions = false;
	GUIStyle bgStyle;

	public void Start() {
		areaBGTexture = new Texture2D(1,1);
		areaBGTexture.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.8f, 0.5f));
		areaBGTexture.Apply();
		selectedGroup = new List<string>();
	}

	public void Update() {
		Scheduler s = GetComponent<Scheduler>();
		if(s.activeCharacter != null) {
			Camera cam = Camera.main;
			MovableCamera mc = cam.transform.parent.GetComponent<MovableCamera>();
			mc.targetPivot = s.activeCharacter.transform.position;
		}
	}

	protected void OnGUIConfirmation(string msg, out bool yesButton, out bool noButton) {
		if(bgStyle == null) {
			bgStyle = new GUIStyle();
			bgStyle.normal.background = areaBGTexture;
		}
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

	void SkillApplied(Skill s) {
		selectedGroup = null;
		Scheduler sch = GetComponent<Scheduler>();
		Character ac = sch.activeCharacter;
		if(s.character == ac) {
			if(s is MoveSkill) {
				activeCharacterHasMoved = true;
			}
			if(s is ActionSkill) {
				activeCharacterHasActed = true;
			}
		}
	}
	void DeactivatedCharacter(Character c) {
		selectedGroup = null;
		activeCharacterHasMoved = false;
		activeCharacterHasActed = false;
	}

	bool IsSkillEnabled(Skill s) {
		return (!(s is MoveSkill) || (permitMultipleMoves || !activeCharacterHasMoved)) &&
	  (((s is MoveSkill) || (s is WaitSkill)) || (permitMultipleActions || !activeCharacterHasActed));
	}

	Character lastShownCharacter = null;
	IEnumerable<object> sorted=null;
	Regex delimiter;
	string lastSegment=null;
	int lastSegmentCount=0;

	IEnumerable<string> OnGUISkillGroup(Character ac, IEnumerable<Skill> skills, IEnumerable<string> selectedGroup) {
		if(delimiter == null) { delimiter = new Regex("//"); }
		IEnumerable<string> nextSelectedGroup = selectedGroup;
		int segmentCount = selectedGroup == null ? 0 : selectedGroup.Count();
		string segment = (selectedGroup == null || selectedGroup.Count() == 0) ? null :  selectedGroup.Last();
		if(ac != lastShownCharacter || segmentCount != lastSegmentCount || segment != lastSegment || sorted == null) {
			//group by skillGroup
			//anything with selectedGroup as its group is the current level
			//anything that is one away from selectedGroup is the next level
			string groupPath = selectedGroup == null ? "" : string.Join("//", selectedGroup.ToArray());
			var groups = skills.Where(x => !x.isPassive).OrderBy(x => x.skillName).GroupBy(x => x.skillGroup);
			List<object> usedEntities = new List<object>();
			//top level skills

			//TODO: can defer sort score calculation until later --
			//find each subgroup prefix and do the deepGroupSkills calculation later
			//since each prefix ought to be unique across a number of groups, we can use
			//set union semantics.
			foreach(var group in groups.Where(x => x.Key == groupPath)) {
				foreach(Skill s in group) {
					usedEntities.Add(s as object);
				}
			}
			foreach(var group in groups.Where(x =>
				x.Key != groupPath &&
				x.Key != null &&
				(groupPath == null || (x.Key.StartsWith(groupPath))))
			) {
				string[] groupSegments = delimiter.Split(group.Key);
				//it's a next group
				string[] groupKeySegments = new string[segmentCount+1];
				Array.Copy(groupSegments, groupKeySegments, segmentCount+1);
				string groupKey = string.Join("//", groupKeySegments);
				if(!usedEntities.Contains(groupKey)) {
					usedEntities.Add(groupKey);
				}
			}
			//get it all ordered and sorted and interleaved and displayed nicely
			sorted = usedEntities.OrderBy(delegate(object x) {
				if(x is Skill) {
					return (x as Skill).skillSorting;
				} else {
					string key = x as string;
					var deepGroupSkills = skills.Where(y => y.skillGroup != null && y.skillGroup.StartsWith(key));
					return (int)Mathf.Round((float)deepGroupSkills.Average(y => y.skillSorting));
				}
			}).ToArray().AsEnumerable();
		}
		foreach(object o in sorted) {
			if(o is Skill) {
				Skill skill = o as Skill;
				GUI.enabled = IsSkillEnabled(skill);
				if(GUILayout.Button(skill.skillName)) {
					skill.ActivateSkill();
				}
				GUI.enabled = true;
			} else {
				string groupKey = o as string;
				string[] groupSegments = delimiter.Split(groupKey);
				string groupName = groupSegments[segmentCount];
				var deepGroupSkills = skills.Where(x => x.skillGroup != null && x.skillGroup.StartsWith(groupKey));
				GUI.enabled = deepGroupSkills.Any(x => IsSkillEnabled(x));
				if(GUILayout.Button(groupName)) {
					if(selectedGroup == null) {
						nextSelectedGroup = new[] { groupName };
					} else {
						nextSelectedGroup = selectedGroup.Concat(new[] { groupName });
					}
				}
				GUI.enabled = true;
			}
		}
		if(selectedGroup != null && selectedGroup.Count() > 0) {
			if(GUILayout.Button("Back") || Input.GetButtonDown("Cancel")) {
				List<string> nextSel = selectedGroup.ToList();
				nextSel.RemoveAt(nextSel.Count-1);
				nextSelectedGroup = nextSel;
			}
		}
		lastShownCharacter = ac;
		lastSegment = segment;
		lastSegmentCount = segmentCount;
		return nextSelectedGroup == null ? null : nextSelectedGroup.ToList().AsEnumerable();
	}
	Map map;
	public void OnGUI() {
		Scheduler s = GetComponent<Scheduler>();
		Arbiter a = GetComponent<Arbiter>();
		bool showAnySchedulerButtons = true;
		bool showCancelButton = true;
		Character ac = s.activeCharacter;
		var skills = ac == null ? new Skill[0] : ac.Skills;
		if(ac != null) {
			if(map == null) { map = transform.parent.GetComponent<Map>(); }
			MoveSkill ms = ac.moveSkill;
			if(ms.isActive && ms != null) {
				if(a.IsLocalTeam(ac.EffectiveTeamID)) {
					MoveExecutor me = ms.Executor;
					if(!me.IsMoving) {
						if(ms.RequireConfirmation &&
							 ms.AwaitingConfirmation) {
							bool yesButton=false, noButton=false;
							OnGUIConfirmation("Move here?", out yesButton, out noButton);
							if(yesButton) {
								PathNode pn = ms.overlay.PositionAt(ms.selectedTile);
					      ms.PerformMoveToPathNode(pn);
					      ms.AwaitingConfirmation = false;
							}
							if(noButton) {
					      ms.AwaitingConfirmation = false;
					      ms.TemporaryMove(map.InverseTransformPointWorld(me.position));
							}
							showCancelButton = false;
						} else {
							showCancelButton = false;
						}
						showAnySchedulerButtons = false;
					} else {
						showAnySchedulerButtons = false;
						showCancelButton = false;
					}
				}/* else if(io is ContinuousWithinTilesMoveIO) {
					ContinuousWithinTilesMoveIO mio = io as ContinuousWithinTilesMoveIO;
					if(mio.character != null &&
						mio.character.isActive &&
						a.IsLocalTeam(mio.character.EffectiveTeamID) &&
						mio.supportMouse) {
						GUILayout.BeginArea(new Rect(
							Screen.width/2-48, Screen.height-32,
							96, 24
						));
						if(GUILayout.Button("End Move")) {
							ms.PerformMove(mio.moveDest);
						}
						GUILayout.EndArea();
						showAnySchedulerButtons = false;
					}
				}*/
			}

			foreach(Skill skill in skills) {
				if(!skill.isPassive && skill.isActive && skill is ActionSkill && !(skill is MoveSkill || skill is WaitSkill)) {
					ActionSkill ask = skill as ActionSkill;
					if(a.IsLocalTeam(ac.EffectiveTeamID)) {
						if(ask.RequireConfirmation &&
							 ask.AwaitingConfirmation) {
							bool yesButton=false, noButton=false;
							OnGUIConfirmation("Confirm?", out yesButton, out noButton);
							if(yesButton) {
					  		ask.AwaitingConfirmation = false;
								ask.ApplySkill();
							}
							if(noButton) {
					  		ask.AwaitingConfirmation = false;
							}
						}
						showAnySchedulerButtons = false;
					}
				}
			}
			WaitSkill ws = ac.waitSkill as WaitSkill;
			if(ws != null && ws.isActive) {
				if(a.IsLocalTeam(ac.EffectiveTeamID)) {
					if(ws.RequireConfirmation &&
						 ws.AwaitingConfirmation) {
						bool yesButton=false, noButton=false;
						OnGUIConfirmation("Wait here?", out yesButton, out noButton);
						if(yesButton) {
			  	  	ws.AwaitingConfirmation = false;
							ws.ApplySkill();
						}
						if(noButton) {
			      	ws.AwaitingConfirmation = false;
						}
					} else {
				  	showCancelButton = permitMultipleMoves || permitMultipleActions || !(activeCharacterHasMoved && activeCharacterHasActed);
					}
					showAnySchedulerButtons = false;
				}
			}
		}
		if(s is CTScheduler) {
			if(ac != null && a.IsLocalTeam(ac.EffectiveTeamID)) {
				GUILayout.BeginArea(new Rect(
					8, 8,
					128, 180
				));
				GUILayout.Label("Character:"+ac.gameObject.name);
				GUILayout.Label("Health: "+Mathf.Ceil(ac.GetStat("health")));
				CTCharacter ctc = ac.GetComponent<CTCharacter>();
				GUILayout.Label("CT: "+Mathf.Floor(ctc.CT));

				//TODO:0: support skills
				//show list of skills
				Skill activeSkill = null;
				foreach(Skill sk in skills) {
					if(sk.isActive) {
						activeSkill = sk;
						break;
					}
				}
				if(activeSkill == null) {
					//root: move, act group, wait
					var nextSel = OnGUISkillGroup(ac, skills, selectedGroup);
					selectedGroup = nextSel == null ? null : nextSel.ToList();
				} else if(showCancelButton) {
					if(GUILayout.Button("Cancel "+activeSkill.skillName)) {
						activeSkill.Cancel();
					}
				}
				GUILayout.EndArea();
			}
		} else if(s is TeamRoundsPickAnyOnceScheduler) {
			TeamRoundsPickAnyOnceScheduler tps = s as TeamRoundsPickAnyOnceScheduler;
			GUILayout.BeginArea(new Rect(
				8, 8,
				110, 180
			));
			GUILayout.Label("Current Team:"+tps.currentTeam);
			if(ac != null) {
				GUILayout.Label("Character:"+ac.gameObject.name);
				GUILayout.Label("Health: "+Mathf.Ceil(ac.GetStat("health")));
				//show list of skills
				Skill activeSkill = null;
				foreach(Skill sk in skills) {
					if(sk.isActive) {
						activeSkill = sk;
						break;
					}
				}
				if(activeSkill == null) {
					//root: move, act group, wait
					var nextSel = OnGUISkillGroup(ac, skills, selectedGroup);
					selectedGroup = nextSel == null ? null : nextSel.ToList();
				} else if(showCancelButton) {
					if(GUILayout.Button("Cancel "+activeSkill.skillName)) {
						activeSkill.Cancel();
					}
				}
			} else {
				GUILayout.Label("Click any team member");
			}
			if(a.IsLocalTeam(tps.currentTeam)) {
				if(showAnySchedulerButtons &&
					!(ac != null && ac.moveSkill.Executor.IsMoving) &&
				  GUILayout.Button("End Round")) {
					tps.EndRound();
				}
			}
			GUILayout.EndArea();
		} else if(s is TeamRoundsPointsScheduler) {
			TeamRoundsPointsScheduler tps = s as TeamRoundsPointsScheduler;
			if(a.IsLocalTeam(tps.currentTeam)) {
				GUILayout.BeginArea(new Rect(
					8, 8,
					110, 180
				));
				GUILayout.Label("Current Team: "+tps.currentTeam);
				GUILayout.Label("Points Left: "+tps.pointsRemaining);
				if(ac != null) {
					GUILayout.Label("Character:"+ac.gameObject.name);
					GUILayout.Label("Health: "+Mathf.Ceil(ac.GetStat("health")));
					RoundPointsCharacter rpc = ac.GetComponent<RoundPointsCharacter>();
					GUILayout.Label("AP: "+Mathf.Floor(rpc.Limiter));

					//show list of skills
					Skill activeSkill = null;
					foreach(Skill sk in skills) {
						if(sk.isActive) {
							activeSkill = sk;
							break;
						}
					}
					if(activeSkill == null) {
						//root: move, act group, wait
						var nextSel = OnGUISkillGroup(ac, skills, selectedGroup);
						selectedGroup = nextSel == null ? null : nextSel.ToList();
					} else if(showCancelButton) {
						if(GUILayout.Button("Cancel "+activeSkill.skillName)) {
							activeSkill.Cancel();
						}
						if(showAnySchedulerButtons &&
							 activeSkill is MoveSkill &&
							 !(activeSkill as MoveSkill).Executor.IsMoving) {
							if(GUILayout.Button("End Move")) {
								activeSkill.ApplySkill();
							}
						}
					}
				} else {
					if(showAnySchedulerButtons &&
						!(ac != null && ac.moveSkill.Executor.IsMoving) &&
					  GUILayout.Button("End Round")) {
						tps.EndRound();
					}
				}
				GUILayout.EndArea();
			}
		} else if(s is TeamRoundsInitiativeScheduler || s is RoundsInitiativeScheduler) {
			GUILayout.BeginArea(new Rect(
				8, 8,
				110, 180
			));
			TeamRoundsInitiativeScheduler tis = s as TeamRoundsInitiativeScheduler;
			if(tis != null) {
				GUILayout.Label("Current Team:"+tis.currentTeam);
			}
			if(ac != null) {
				GUILayout.Label("Character:"+ac.gameObject.name);
				GUILayout.Label("Health: "+Mathf.Ceil(ac.GetStat("health")));
				//show list of skills
				Skill activeSkill = null;
				foreach(Skill sk in skills) {
					if(sk.isActive) {
						activeSkill = sk;
						break;
					}
				}
				if(activeSkill == null) {
					//root: move, act group, wait
					var nextSel = OnGUISkillGroup(ac, skills, selectedGroup);
					selectedGroup = nextSel == null ? null : nextSel.ToList();
				} else if(showCancelButton) {
					if(GUILayout.Button("Cancel "+activeSkill.skillName)) {
						activeSkill.Cancel();
					}
				}
			}
			GUILayout.EndArea();
		}
	}
}