using UnityEngine;

[System.Serializable]
public class Parameter {
	public string name;
	public Formula formula;
	
	public string Name { get { return name; } set { name = value; } }
	public Formula Formula { get { return formula; } set { formula = value; } }
	public Parameter(string n, Formula f) {
		name = n;
		formula = f;
	}
}