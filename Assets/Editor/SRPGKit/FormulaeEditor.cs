using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(Formulae))]

public class FormulaeEditor : SRPGCKEditor {
  [MenuItem("SRPGCK/Create formula database")]
  public static Formulae CreateFormulae()
  {
    Formulae asset = ScriptableObject.CreateInstance<Formulae>();
		asset.formulaNames = new List<string>();
		asset.formulae = new List<Formula>();
		AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath("Assets/Resources/Formulae.asset"));
    AssetDatabase.SaveAssets();
    EditorUtility.FocusProjectWindow();
    Selection.activeObject = asset;
		return asset;
  }

	protected override void UpdateFormulae() {
		formulaOptions = fdb.formulaNames.ToArray();
	}

	public override void OnEnable() {
		fdb = target as Formulae;
		base.OnEnable();
		name = "Formulae";
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