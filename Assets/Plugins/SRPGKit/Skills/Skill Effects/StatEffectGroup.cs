//since we can't serialize 2D lists...

[System.Serializable]
public class StatEffectGroup {	
	public StatEffect[] effects;
	public int Length { get { return effects.Length; } }
	public StatEffectGroup() {
		effects = new StatEffect[0];
	}
	
	//editor only
	public bool editorDisplayEffects=true;
}