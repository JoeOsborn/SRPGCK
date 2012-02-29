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

	public override void OnEnable() {
		useFormulae = false;
		fdb = target as Formulae;
		base.OnEnable();
		name = "Formulae";
		newFormula = Formula.Constant(0);
		newFormula.name = "";
	}

	Formula newFormula;

	public void EditFormulaField(Formula f, int i) {
		string name = "formulae.formula."+i;
		bool priorWrap = EditorStyles.textField.wordWrap;
		EditorStyles.textField.wordWrap = true;
		GUI.SetNextControlName(name);
		f.text = EditorGUILayout.TextArea(f.text, GUILayout.Height(32)).RemoveControlCharacters();
		GUI.SetNextControlName("");
		EditorStyles.textField.wordWrap = priorWrap;
		if(GUI.GetNameOfFocusedControl() == name) {
			FormulaCompiler.CompileInPlace(f);
		}
		if(f.compilationError != null && f.compilationError.Length > 0) {
			EditorGUILayout.HelpBox(f.compilationError, MessageType.Error);
		}
	}

	public override void OnSRPGCKInspectorGUI () {
		for(int i = 0; i < fdb.formulae.Count; i++) {
			Formula f = fdb.formulae[i];
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(f.name);
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Delete")) {
				fdb.RemoveFormula(f.name);
			}
			EditorGUILayout.EndHorizontal();
			EditFormulaField(f, i);
		}
		EditorGUILayout.Space();
		EditorGUILayout.BeginHorizontal();
		if(newFormula.name == null) { newFormula.name = ""; }
		GUI.SetNextControlName("formulae.new.name");
		newFormula.name = EditorGUILayout.TextField(newFormula.name);
		GUI.SetNextControlName("");
		GUI.enabled = newFormula.name.Length > 0;
		if(GUILayout.Button("New Formula")) {
			fdb.AddFormula(newFormula, newFormula.name);
			newFormula = Formula.Constant(0);
			newFormula.name = "";
		}
		GUI.enabled = true;
		EditorGUILayout.EndHorizontal();
		EditFormulaField(newFormula, fdb.formulae.Count);
	}
}