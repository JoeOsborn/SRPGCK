using UnityEngine;
using System.Linq;
using System.Collections.Generic;

using UnityEditor;

public class SRPGCKSettings : ScriptableObject {
	static SRPGCKSettings settings;
	public static SRPGCKSettings Settings { get {
		if(settings == null) {
			settings = Resources.Load("SRPGCKSettings", typeof(SRPGCKSettings)) as SRPGCKSettings;
			if(settings == null) {
				Debug.LogError("Please create a global SRPGCK Settings asset");
				return null;
			}
		}
		return settings;
	} }

	public Formulae defaultFormulae;
	public SkillIO defaultActionIO, defaultMoveIO;
}
