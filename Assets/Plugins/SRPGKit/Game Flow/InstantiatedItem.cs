using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

public class InstantiatedItem : MonoBehaviour {
	public Item item;

	public bool HasParam(string pname) {
		return item.HasParam(pname);
	}

	public float GetParam(
		string pname, 
		SkillDef scontext=null, 
		float fallback=float.NaN
	) {
		return item.GetParam(pname, scontext, fallback);
	}
}
