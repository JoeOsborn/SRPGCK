using UnityEngine;

//wraps stat effects, has a duration in clock ticks, can supersede others
public class StatusEffect : MonoBehaviour {
	public StatEffect[] passiveEffects;
	public float ticksRemaining=0;
	public float tickDuration=0;
	
	public string effectType;
	public int priority=0;
	public bool replaces=false;
	public bool always=false;
	
	public void Start() {
		//todo: notify
	}
	
	public void Tick(float delta) {
		if(tickDuration == 0 || always) { return; }
		ticksRemaining -= delta;
		if(ticksRemaining <= 0) {
			//todo: notify			
			Destroy(gameObject);
		}
	}
}