using UnityEngine;

//wraps stat effects, has a duration in clock ticks, can supersede others
[AddComponentMenu("SRPGCK/Character/Status Effect")]
public class StatusEffect : MonoBehaviour {
	public StatEffect[] passiveEffects;

	public float ticksRemaining=0;
	public float tickDuration=0;
	public bool ticksInLocalTime=false;

	public string effectType;
	public int overridePriority=0, replacementPriority=0;
	public bool replaces=false;
	public bool overrides=false;
	public bool always=false;
	public bool usesDuration=false;

	public float tickEffectTicksRemaining=0;
	public float tickEffectInterval=1;
	public StatEffect[] tickIntervalEffects;

	public StatEffect[] characterActivatedEffects;
	public StatEffect[] characterDeactivatedEffects;
	public StatEffect[] statusEffectAppliedEffects;
	public StatEffect[] statusEffectRemovedEffects;

	public SkillDef applyingSkill;

	public Character _character;
	public Character character { get {
		if(_character == null && transform.parent != null) {
			Transform t = transform.parent;
			while(t != null && _character == null) {
				_character = t.GetComponent<Character>();
				if(_character == null) { t = t.parent; }
			}
			if(_character == null) { Debug.LogError("No attached character"); }
		}
		return _character;
	} }

	protected void ApplyActionEffects(StatEffect[] fx) {
		foreach(StatEffect se in fx) {
			se.Apply(
				applyingSkill,
				applyingSkill != null ? applyingSkill.character : null,
				character
			);
		}
	}

	public void Start() {
		ticksRemaining = tickDuration;
		if(character.ApplyStatusEffect(this)) {
			tickEffectTicksRemaining = tickEffectInterval;
			//apply on-apply stuff
			ApplyActionEffects(statusEffectAppliedEffects);
		}
	}

	public void RemovedFrom(Character c) {
		//apply on-unapply stuff
		ApplyActionEffects(statusEffectRemovedEffects);
	}

	// public void SkillApplied(Skill s) {
	// 	if(s.character == character && character.StatusEffectIsActive(this)) {
	// 		//apply on-skill-apply stuff?
	// 	}
	// }

	public void ActivatedCharacter(Character c) {
		if(c == character && character.HasStatusEffect(this)) {
			//apply on-activate stuff
			ApplyActionEffects(characterActivatedEffects);
		}
	}

	public void DeactivatedCharacter(Character c) {
		if(c == character && character.HasStatusEffect(this)) {
			//apply on-deactivate stuff
			ApplyActionEffects(characterDeactivatedEffects);
		}
	}

	public void Tick(float delta) {
		//apply on-tick-interval stuff
		if(tickIntervalEffects != null && tickIntervalEffects.Length > 0) {
			tickEffectTicksRemaining -= delta;
			while(tickEffectTicksRemaining < 0) {
				if(character.HasStatusEffect(this)) {
					ApplyActionEffects(tickIntervalEffects);
				}
				tickEffectTicksRemaining += tickEffectInterval;
			}
		}
		if(!usesDuration || always) { return; }
		ticksRemaining -= delta;
		if(ticksRemaining <= 0) {
			character.RemoveStatusEffect(this);
		}
	}
}