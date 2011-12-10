using UnityEngine;

//wraps stat effects, has a duration in clock ticks, can supersede others
public class StatusEffect : MonoBehaviour {
	public StatEffect[] passiveEffects;
	public float ticksRemaining=0;
	public float tickDuration=0;
	public bool ticksInLocalTime=false;
	
	public string effectType;
	public int priority=0;
	public bool replaces=false;
	public bool always=false;
	public bool usesDuration=false;
	
	public void Start() {
		ticksRemaining = tickDuration;
		//todo: notify
	}
	
	public void Tick(float delta) {
		if(!usesDuration || always) { return; }
		ticksRemaining -= delta;
		if(ticksRemaining <= 0) {
			//todo: notify			
			Destroy(gameObject);
		}
	}
}