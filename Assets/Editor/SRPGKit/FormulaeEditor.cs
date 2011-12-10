using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(Formulae))]

public class FormulaeEditor : SRPGCKEditor {		
	Formulae fdb;

	protected override void UpdateFormulae() {
		if(Formulae.GetInstance() == null) {
			Debug.LogError("Need a formulae object somewhere to store formulae!");
		} else {
			formulaOptions = Formulae.GetInstance().formulaNames.ToArray();
		}
	}	
	
	public override void OnEnable() {
		base.OnEnable();
		name = "Formulae";
		fdb = target as Formulae;
		selection = 0;
		newFormulaName = "";
	}
	
	int selection = 0;
	string newFormulaName;
	
	public override void OnSRPGCKInspectorGUI () {

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		newFormulaName = EditorGUILayout.TextField(newFormulaName);
		if(GUILayout.Button("Create New")) {
			fdb.AddFormula(Formula.Constant(0), newFormulaName);
			UpdateFormulae();
			selection = formulaOptions.Length-1;
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Space();

		EditorGUILayout.BeginHorizontal();
		GUI.enabled = formulaOptions.Length > 0;
		selection = EditorGUILayout.Popup("Formula:", selection, formulaOptions);
		if(GUILayout.Button("Delete", GUILayout.Width(64))) {
			fdb.RemoveFormula(formulaOptions[selection]);
			UpdateFormulae();
			while(selection >= formulaOptions.Length && formulaOptions.Length > 0) {
				selection--;
			}
		}
		GUI.enabled = true;
		EditorGUILayout.EndHorizontal();

		if(formulaOptions.Length > 0) {
			string name = "formulae.formula";
			GUI.SetNextControlName(name);
			bool priorWrap = EditorStyles.textField.wordWrap;
			EditorStyles.textField.wordWrap = true;
			Formula f = fdb.formulae[selection];
			f.text = EditorGUILayout.TextArea(f.text).RemoveControlCharacters();
			EditorStyles.textField.wordWrap = priorWrap;
			if(GUI.GetNameOfFocusedControl() == name) {
				FormulaCompiler.CompileInPlace(f);
			}
		}
	}
}