//since we can't serialize 2D lists...
using System.Linq;

[System.Serializable]
public class StatEffectGroup {	
	public StatEffect[] effects;
	public int Length { get { return effects.Length; } }
	public StatEffectGroup() {
		effects = new StatEffect[0];
	}
	
	public StatEffectGroup Concat(StatEffectGroup other) {
		StatEffectGroup ret = new StatEffectGroup();
		ret.effects = this.effects.Concat(other.effects).ToArray();
		return ret;
	}
	
	//editor only
	public bool editorDisplayEffects=true;
}