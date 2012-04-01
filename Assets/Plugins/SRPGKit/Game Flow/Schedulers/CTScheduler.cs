using UnityEngine;
using System.Collections;

[AddComponentMenu("SRPGCK/Arbiter/Scheduler/CT")]
public class CTScheduler : Scheduler {
	public string speedStat = "speed";
	public string ctStat = "ct";
	public string maxCTStat = "maxCT";
	public string skillSpeedStat = "speed";
	public string perActivationCTCostStat = "activationCT";
	public string perMoveCTCostStat = "moveCT";
	public string perTileCTCostStat = "perTileCT";
	public string perActionCTCostStat = "actCT";

	public float defaultSpeed = 1;
	public float defaultSkillSpeed = 1;
	public float defaultMaxCT = 100;
	public float defaultPerActivationCTCost = 30;
	public float defaultPerMoveCTCost = 30;
	public float defaultPerTileCTCost = 0;
	public float defaultPerActionCTCost = 40;

	public bool coalesceCTDecrements = false;
	[HideInInspector]
	[SerializeField]
	protected float pendingCTDecrement = 0;

	[HideInInspector]
	public bool activeCharacterHasMoved=false;
	[HideInInspector]
	public bool activeCharacterHasActed=false;

	override public void Start () {
		base.Start();
	}

	override public void AddCharacter(Character c) {
		if(!c.HasStat(ctStat)) {
			Debug.LogError("CT-scheduled character "+c+" must have ct stat.");
			return;
		}
		if(!c.HasStat(speedStat)) {
			Debug.LogError("CT-scheduled character "+c+" must have speed stat.");
			return;
		}
		base.AddCharacter(c);
	}

	override public void Activate(Character c, object ctx=null) {
		base.Activate(c, ctx);
		pendingCTDecrement = 0;
		activeCharacterHasMoved = false;
		activeCharacterHasActed = false;
		//(for now): ON `activate`, MOVE
//		Debug.Log("activate");
	}

	override public void Deactivate(Character c, object ctx=null) {
		base.Deactivate(c, ctx);
		//reduce c's CT by base turn cost (30)
		float cost = perActivationCTCostStat != null ?
			c.GetStat(perActivationCTCostStat, defaultPerActivationCTCost) :
			defaultPerActivationCTCost;
		if(coalesceCTDecrements) {
			pendingCTDecrement += cost;
			c.AdjustBaseStat(ctStat, -pendingCTDecrement);
			pendingCTDecrement = 0;
		} else {
			c.AdjustBaseStat(ctStat, -cost);
		}
		activeCharacterHasMoved = false;
		activeCharacterHasActed = false;
	}

	override public void SkillApplied(SkillDef s) {
		base.SkillApplied(s);
		if(s.character != null && s.character == activeCharacter) {
			if(!(s is MoveSkillDef)) {
				activeCharacterHasActed = true;
				float cost = perActionCTCostStat != null ?
					s.character.GetStat(perActionCTCostStat, defaultPerActionCTCost) :
					defaultPerActionCTCost;
				if(coalesceCTDecrements) {
					pendingCTDecrement += cost;
				} else {
					s.character.AdjustBaseStat(ctStat, -cost);
				}
			}
		}
	}

	public void WillSpecialMoveCharacter(CharacterSpecialMoveReport csmr) {
		Pause();
	}
	public void DidSpecialMoveCharacter(CharacterSpecialMoveReport csmr) {
		Resume();
	}

	override public void CharacterMoved(
		Character c,
		Vector3 src,
		Vector3 dest,
		PathNode endOfPath
	) {
		base.CharacterMoved(c, src, dest, endOfPath);
		if(c != activeCharacter) { return; }
		//reduce c's CT by per-tile movement cost (0)
		activeCharacterHasMoved = true;
		float cost =
			((perTileCTCostStat != null ?
				c.GetStat(perTileCTCostStat, defaultPerTileCTCost) :
				defaultPerTileCTCost)*endOfPath.xyDistanceFromStart) +
			(perMoveCTCostStat != null ?
				c.GetStat(perMoveCTCostStat, defaultPerMoveCTCost) :
				defaultPerMoveCTCost);
		if(coalesceCTDecrements) {
			pendingCTDecrement += cost;
		} else {
			c.AdjustBaseStat(ctStat, -cost);
		}
	}

	public override void FixedUpdate() {
		base.FixedUpdate();
		if(paused) { return; }
		//if there is no active unit
		if(activeCharacter == null) {
			//else, take the first unit or action with CT > 100, if any, and activate it
			for(int i = 0; i < pendingSkillActivations.Count; i++) {
				SkillActivation sa = pendingSkillActivations[i];
				if(sa.delayRemaining <= 0) {
					sa.Apply();
					pendingSkillActivations.RemoveAt(i);
					//FIXME: need to prevent scheduler from
					//fixedupdate-ing while skills are animating
					return;
				}
			}
			foreach(Character c in characters) {
				float ct = c.GetStat(ctStat);
				float maxCT = maxCTStat != null ?
					c.GetStat(maxCTStat, defaultMaxCT) :
					defaultMaxCT;
				if(ct >= maxCT) {
					c.SetBaseStat(ctStat, maxCT);
					Activate(c);
					return;
				}
			}
			//and tick up CT on everything/body by their effective speed
			foreach(SkillActivation sa in pendingSkillActivations) {
				sa.delayRemaining =
					sa.delayRemaining - sa.skill.GetParam(skillSpeedStat, defaultSkillSpeed);
			}
			foreach(Character c in characters) {
				//don't mess with jumpers' speed because speed is used in jump timing
				if(c.HasStatusEffect("jumping")) { continue; }
				float speed = speedStat != null ?
					c.GetStat(speedStat, defaultSpeed) :
					defaultSpeed;
				c.AdjustBaseStat(ctStat, speed);
				foreach(StatusEffect se in c.StatusEffects) {
					se.Tick(se.ticksInLocalTime ? speed : 1);
				}
			}
		}
		//otherwise, do nothing
	}
}
