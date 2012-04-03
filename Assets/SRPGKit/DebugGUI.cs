using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

[AddComponentMenu("SRPGCK/Arbiter/Debug GUI")]
public class DebugGUI : MonoBehaviour {
	Texture2D _areaBGTexture;
	Texture2D areaBGTexture { get {
		if(_areaBGTexture == null) {
			_areaBGTexture = new Texture2D(1,1);
			_areaBGTexture.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.8f, 0.5f));
			_areaBGTexture.Apply();
		}
		return _areaBGTexture;
	} }
	[HideInInspector]
	[SerializeField]
	List<string> selectedGroup;

	bool activeCharacterHasMoved;
	bool activeCharacterHasActed;
	public bool permitMultipleMoves = false;
	public bool permitMultipleActions = false;
	GUIStyle _bgStyle;
	GUIStyle bgStyle { get {
		if(_bgStyle == null) {
			_bgStyle = new GUIStyle();
			_bgStyle.normal.background = areaBGTexture;
		}
		return _bgStyle;
	} }

	[HideInInspector]
	public ActionSkillDef pendingTargetedSkill;

	public void Start() {
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

	protected void OnGUIConfirmation(string msg, out bool yesButton, out bool noButton, string yesMsg = "Yes", string noMsg = "No") {
		GUILayout.BeginArea(new Rect(
			Screen.width/2-64, Screen.height/2-32,
			128, 64
		), bgStyle); {
		  GUILayout.BeginVertical(); {
		    GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
		    centeredStyle.alignment = TextAnchor.MiddleCenter;
		    GUILayout.Label(msg, centeredStyle);
		    GUILayout.BeginHorizontal(); {
		      if(GUILayout.Button(noMsg)) {
						noButton = true;
		      } else {
						noButton = false;
					}
					if(GUILayout.Button(yesMsg)) {
						yesButton = true;
		      } else {
						yesButton = false;
					}
		    } GUILayout.EndHorizontal();
		  } GUILayout.EndVertical();
		} GUILayout.EndArea();
	}

	protected int OnGUIChoices(string msg, string[] msgs) {
		int chosen = -1;
		float width = 256;
		float height = 24+msgs.Length*36;
		GUILayout.BeginArea(new Rect(
			Screen.width/2-width/2, Screen.height/2-height/2,
			width, height
		), bgStyle); {
		  GUILayout.BeginVertical(); {
		    GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
		    centeredStyle.alignment = TextAnchor.MiddleCenter;
		    GUILayout.Label(msg, centeredStyle);
				for(int i = 0; i < msgs.Length; i++) {
					if(GUILayout.Button(msgs[i])) {
						chosen = i;
					}
				}
			} GUILayout.EndVertical();
		} GUILayout.EndArea();
		return chosen;
	}


	void SkillApplied(SkillDef s) {
		selectedGroup = null;
		Scheduler sch = GetComponent<Scheduler>();
		Character ac = sch.activeCharacter;
		if(s.character == ac) {
			if(s is MoveSkillDef) {
				activeCharacterHasMoved = true;
			} else if(s is ActionSkillDef) {
				activeCharacterHasActed = true;
			}
		}
	}
	void DeactivatedCharacter(Character c) {
		selectedGroup = null;
		activeCharacterHasMoved = false;
		activeCharacterHasActed = false;
	}

	bool IsSkillEnabled(SkillDef s) {
		return s.IsEnabled && (!(s is MoveSkillDef) || (permitMultipleMoves || !activeCharacterHasMoved)) &&
	  (((s is MoveSkillDef) || (s is WaitSkillDef)) || (permitMultipleActions || !activeCharacterHasActed));
	}

	Character lastShownCharacter = null;
	IEnumerable<object> sorted=null;
	Regex delimiter;
	string lastSegment=null;
	int lastSegmentCount=0;

	IEnumerable<string> OnGUISkillGroup(Character ac, IEnumerable<SkillDef> skills, IEnumerable<string> selectedGroup) {
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
				foreach(SkillDef s in group) {
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
				// Debug.Log("segs "+groupSegments.Length+" key segs "+groupKeySegments.Length+" scp1 "+(segmentCount+1));
				Array.Copy(groupSegments, groupKeySegments, segmentCount+1);
				string groupKey = string.Join("//", groupKeySegments);
				if(!usedEntities.Contains(groupKey)) {
					usedEntities.Add(groupKey);
				}
			}
			//get it all ordered and sorted and interleaved and displayed nicely
			sorted = usedEntities.OrderBy(delegate(object x) {
				if(x is SkillDef) {
					return (x as SkillDef).skillSorting;
				} else {
					string key = x as string;
					var deepGroupSkills = skills.Where(y => y.skillGroup != null && y.skillGroup.StartsWith(key));
					return (int)Mathf.Round((float)deepGroupSkills.Average(y => y.skillSorting));
				}
			}).ToArray().AsEnumerable();
		}
		foreach(object o in sorted) {
			if(o is SkillDef) {
				SkillDef skill = o as SkillDef;
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
	void SkillNeedsCharacterTargetingOption(SkillDef s) {
		Debug.Log("skill "+s.skillName+" wants delayed targeting");
		pendingTargetedSkill = s as ActionSkillDef;
	}
	string[] targetingChoices;
	void SkillIncrementalCancel() {
		pendingTargetedSkill = null;
	}
	public void OnGUI() {
		Scheduler s = GetComponent<Scheduler>();
		Arbiter a = GetComponent<Arbiter>();
		if(pendingTargetedSkill != null) {
			if(targetingChoices == null || targetingChoices.Length == 0) {
				targetingChoices = new string[]{
					"Tile",
					"Character",
					"Cancel"
				};
			}
			int choice = OnGUIChoices("Target tile or character?", targetingChoices);
			if(choice != -1) {
				if(choice == 0) {
					pendingTargetedSkill.ConfirmDelayedSkillTarget(TargetOption.Path);
				} else if(choice == 1) {
					pendingTargetedSkill.ConfirmDelayedSkillTarget(TargetOption.Character);
				} else if(choice == 2) {
					pendingTargetedSkill.IncrementalCancel();
				}
				pendingTargetedSkill = null;
			}
			return;
		}
		bool showAnySchedulerButtons = true;
		bool showCancelButton = true;
		Character ac = s.activeCharacter;
		var skills = ac == null ? new SkillDef[0] : ac.Skills;
		if(ac != null) {
			if(map == null) { map = transform.parent.GetComponent<Map>(); }
			MoveSkillDef ms = ac.moveSkill;
			if(ms != null && ms.isActive) {
				if(a.IsLocalTeam(ac.EffectiveTeamID)) {
					MoveExecutor me = ms.Executor;
					if(!me.IsMoving) {
						if(ms.RequireConfirmation &&
							 ms.AwaitingConfirmation) {
							bool yesButton=false, noButton=false;
							OnGUIConfirmation("Move here?", out yesButton, out noButton);
							if(yesButton) {
								ms.ApplyCurrentTarget();
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
				}
			}

			foreach(SkillDef skill in skills) {
				if(!skill.isPassive && skill.isActive && skill is ActionSkillDef && !(skill is MoveSkillDef || skill is WaitSkillDef)) {
					ActionSkillDef ask = skill as ActionSkillDef;
					if(a.IsLocalTeam(ac.EffectiveTeamID)) {
						if(ask.RequireConfirmation &&
							 ask.AwaitingConfirmation) {
							bool yesButton=false, noButton=false;
							OnGUIConfirmation("Confirm?", out yesButton, out noButton);
							if(yesButton) {
								ask.ApplyCurrentTarget();
							}
							if(noButton) {
					  		ask.AwaitingConfirmation = false;
							}
						}
						showAnySchedulerButtons = false;
					}
				}
			}
			WaitSkillDef ws = ac.waitSkill;
			if(ws != null && ws.isActive) {
				if(a.IsLocalTeam(ac.EffectiveTeamID)) {
					if(ws.RequireConfirmation &&
						 ws.AwaitingConfirmation) {
						bool yesButton=false, noButton=false;
						OnGUIConfirmation("Wait here?", out yesButton, out noButton);
						if(yesButton) {
							ws.ApplyCurrentTarget();
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
					128, 240
				));
				GUILayout.Label("Character:"+ac.gameObject.name);
				GUILayout.Label("HP: "+Mathf.Ceil(ac.GetStat("health", ac.GetStat("HP"))));
				GUILayout.Label("CT: "+Mathf.Floor(ac.GetStat((s as CTScheduler).ctStat)));

				//TODO:0: support skills
				//show list of skills
				SkillDef activeSkill = null;
				foreach(SkillDef sk in skills) {
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
				110, 240
			));
			GUILayout.Label("Current Team:"+tps.currentTeam);
			if(ac != null) {
				GUILayout.Label("Character:"+ac.gameObject.name);
				GUILayout.Label("Health: "+Mathf.Ceil(ac.GetStat("health")));
				//show list of skills
				SkillDef activeSkill = null;
				foreach(SkillDef sk in skills) {
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
					110, 240
				));
				GUILayout.Label("Current Team: "+tps.currentTeam);
				GUILayout.Label("Points Left: "+tps.pointsRemaining);
				if(ac != null) {
					GUILayout.Label("Character:"+ac.gameObject.name);
					GUILayout.Label("Health: "+Mathf.Ceil(ac.GetStat("health")));
					RoundPointsCharacter rpc = ac.GetComponent<RoundPointsCharacter>();
					GUILayout.Label("AP: "+Mathf.Floor(rpc.Limiter));

					//show list of skills
					SkillDef activeSkill = null;
					foreach(SkillDef sk in skills) {
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
							 activeSkill is MoveSkillDef &&
							 !(activeSkill as MoveSkillDef).Executor.IsMoving) {
							if(GUILayout.Button("End Move")) {
								//??
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
				110, 240
			));
			TeamRoundsInitiativeScheduler tis = s as TeamRoundsInitiativeScheduler;
			if(tis != null) {
				GUILayout.Label("Current Team:"+tis.currentTeam);
			}
			if(ac != null) {
				GUILayout.Label("Character:"+ac.gameObject.name);
				GUILayout.Label("Health: "+Mathf.Ceil(ac.GetStat("health")));
				//show list of skills
				SkillDef activeSkill = null;
				foreach(SkillDef sk in skills) {
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