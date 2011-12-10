using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

public class EditorGUIExt
{
		/// <summary>
		/// Creates an array foldout like in inspectors for SerializedProperty of array type.
	/// Counterpart for standard EditorGUILayout.PropertyField which doesn't support SerializedProperty of array type.
	/// </summary>
	public static void ArrayField (SerializedProperty property)
	{
		EditorGUIUtility.LookLikeInspector ();
		bool wasEnabled = GUI.enabled;
		int prevIdentLevel = EditorGUI.indentLevel;

		// Iterate over all child properties of array
		bool childrenAreExpanded = true;
		int propertyStartingDepth = property.depth;
		while (property.NextVisible(childrenAreExpanded) && propertyStartingDepth < property.depth)
		{
			childrenAreExpanded = EditorGUILayout.PropertyField(property);
		}

		EditorGUI.indentLevel = prevIdentLevel;
		GUI.enabled = wasEnabled;
   	EditorGUIUtility.LookLikeControls();
	}
 
	/// <summary>
	/// Creates a filepath textfield with a browse button. Opens the open file panel.
	/// </summary>
	public static string FileLabel(string name, float labelWidth, string path, string extension)
	{
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label(name, GUILayout.MaxWidth(labelWidth));
		string filepath = EditorGUILayout.TextField(path);
		if (GUILayout.Button("Browse"))
		{
			filepath = EditorUtility.OpenFilePanel(name, path, extension);
		}
		EditorGUILayout.EndHorizontal();
		return filepath;
	}

	/// <summary>
	/// Creates a folder path textfield with a browse button. Opens the save folder panel.
	/// </summary>
	public static string FolderLabel(string name, float labelWidth, string path)
	{
		EditorGUILayout.BeginHorizontal();
		string filepath = EditorGUILayout.TextField(name, path);
		if (GUILayout.Button("Browse", GUILayout.MaxWidth(60)))
		{
			filepath = EditorUtility.SaveFolderPanel(name, path, "Folder");
		}
		EditorGUILayout.EndHorizontal();
		return filepath;
	}

	/// <summary>
	/// Creates an array foldout like in inspectors. Hand editable ftw!
	/// </summary>
	public static string[] ArrayFoldout(string label, string[] array, ref bool foldout, bool showElementName=true, float width=-1, string defaultString="")
	{
		if(width == -1) {
			EditorGUILayout.BeginVertical();
		} else {
			EditorGUILayout.BeginVertical(GUILayout.Width(width));
		}
		if(array == null) { array = new string[0]; }
		EditorGUIUtility.LookLikeInspector();
		foldout = EditorGUILayout.Foldout(foldout, label);
		string[] newArray = array;
		if(foldout) {
			if(width == -1) {
				EditorGUILayout.BeginHorizontal();
			} else {
				EditorGUILayout.BeginHorizontal(GUILayout.Width(width));
			}
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();
			int arraySize = EditorGUILayout.IntField("Size", array.Length);
			if (arraySize != array.Length)
				newArray = new string[arraySize];
			for (int i = 0; i < arraySize; i++)
			{
				string entry = defaultString;
				if (i < array.Length)
					entry = array[i];
				if(showElementName) {
					newArray[i] = EditorGUILayout.TextField("Element " + i, entry);
				} else {
					EditorGUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					newArray[i] = EditorGUILayout.TextField(entry);
					EditorGUILayout.EndHorizontal();
				}
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
		}
   	EditorGUIUtility.LookLikeControls();
		EditorGUILayout.EndVertical();
		return newArray;
	}

	/// <summary>
	/// Creates a toolbar that is filled in from an Enum. Useful for setting tool modes.
	/// </summary>
	public static Enum EnumToolbar(Enum selected)
	{
		string[] toolbar = System.Enum.GetNames(selected.GetType());
		Array values = System.Enum.GetValues(selected.GetType());
		int selected_index = 0;
		while (selected_index < values.Length)
		{
			if (selected.ToString() == values.GetValue(selected_index).ToString())
			{
				break;
			}
			selected_index++;
		}
		selected_index = GUILayout.Toolbar(selected_index, toolbar);
		return (Enum) values.GetValue(selected_index);
	}
	
	public static Formula FormulaField(Formula f, string type, string[] formulaOptions, string lastFocusedControl=null, int i=0) {
		int selection= 0;
		int lastSelection = 0;
		EditorGUILayout.BeginVertical();
		if(f == null) {
			f = Formula.Constant(0);
		}
		if(formulaOptions != null) {
			if(f.formulaType == FormulaType.Lookup && 
				 f.lookupType == LookupType.NamedFormula) {
				selection = System.Array.IndexOf(formulaOptions, f.lookupReference);
				lastSelection = selection;
			}
			selection = EditorGUILayout.Popup("Formula:", selection, formulaOptions);
		}
		if(selection == 0 || formulaOptions == null) {
			if(lastSelection != selection) {
				f = Formula.Constant(0);
			}
			EditorGUILayout.BeginHorizontal();
			string name = type+".formula."+i;
			GUI.SetNextControlName(name);
			bool priorWrap = EditorStyles.textField.wordWrap;
			EditorStyles.textField.wordWrap = true;
			f.text = EditorGUILayout.TextArea(f.text).RemoveControlCharacters();
			EditorStyles.textField.wordWrap = priorWrap;
			if(GUI.GetNameOfFocusedControl() == name) {
				FormulaCompiler.CompileInPlace(f);
			}
			EditorGUILayout.EndHorizontal();
		} else if(lastSelection != selection && formulaOptions != null) {
			f = Formula.Lookup(formulaOptions[selection], LookupType.NamedFormula);
		}
		EditorGUILayout.EndVertical();
		return f;
	}
	
	public static Parameter ParameterField(Parameter p, string type, string[] formulaOptions, string lastFocusedControl=null, int i = 0) {
		Parameter newP = p;
		EditorGUILayout.BeginVertical();
		newP.Name = EditorGUILayout.TextField("Name:", p.Name == null ? "" : p.Name).NormalizeName();
		newP.Formula = EditorGUIExt.FormulaField(p.Formula, type, formulaOptions, lastFocusedControl, i);
		EditorGUILayout.BeginHorizontal();
		if(newP.Formula.compilationError != null && newP.Formula.compilationError != "") {
			GUILayout.Label(newP.Formula.compilationError);
		} else {
			GUILayout.Label("");
		}
		GUILayout.FlexibleSpace();
		if(GUILayout.Button("Delete")) {
			newP = null;
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
		return newP;	
	}

	public static StatEffect StatEffectField(StatEffect fx, StatEffectContext ctx, string type, string[] formulaOptions, string lastFocusedControl=null, int i = 0) {
		StatEffect newFx = fx;
		GUILayout.BeginVertical();
		newFx.statName = EditorGUILayout.TextField("Stat Name:", fx.statName == null ? "" : fx.statName).NormalizeName();
		if(ctx == StatEffectContext.Action) {
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical(GUILayout.Width(Screen.width/2-32));
			newFx.effectType = (StatEffectType)EditorGUILayout.EnumPopup("Effect Type:", fx.effectType);
			newFx.target = (StatEffectTarget)EditorGUILayout.EnumPopup("Target:", fx.target);
			GUILayout.EndVertical();
			GUILayout.BeginVertical(GUILayout.Width(Screen.width/2-32));
			newFx.reactableTypes = ArrayFoldout("Reactable Types", fx.reactableTypes, ref newFx.editorShowsReactableTypes, false, Screen.width/2-32, "attack");
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		} else {
			newFx.effectType = (StatEffectType)EditorGUILayout.EnumPopup("Effect Type:", fx.effectType);	
		}
		newFx.value = EditorGUIExt.FormulaField(fx.value, type, formulaOptions, lastFocusedControl, i);
		GUILayout.BeginHorizontal();
		if(fx.value.compilationError != null && fx.value.compilationError != "") {
			GUILayout.Label(fx.value.compilationError);
		} else {
			GUILayout.Label("");
		}
		GUILayout.FlexibleSpace();
		if(GUILayout.Button("Delete")) {
			newFx = null;
		}
		GUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
		return newFx;
	}

	public static StatEffect[] StatEffectFoldout(string name, StatEffect[] effects, StatEffectContext ctx, string[] formulaOptions, string lastFocusedControl, ref bool foldout, bool pluralize=true) {
		EditorGUILayout.BeginVertical();
		StatEffect[] newEffects = effects;
		foldout = EditorGUILayout.Foldout(foldout, ""+effects.Length+" "+name+(!pluralize || effects.Length == 1 ? "" : "s"));
		if(foldout) {
			EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(Screen.width-16));
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();
			List<StatEffect> toBeRemoved = null;
			for(int fxi = 0; fxi < effects.Length; fxi++) {
				StatEffect fx = effects[fxi];
				StatEffect newFx = StatEffectField(fx, ctx, name+fxi, formulaOptions, lastFocusedControl, fxi);
				if(newFx == null) {
					if(toBeRemoved == null) { toBeRemoved = new List<StatEffect>(); }
					toBeRemoved.Add(fx);
				} else {
					effects[fxi] = newFx;
				}
				EditorGUILayout.Space();
			}
			if(toBeRemoved != null) {
				newEffects = effects.Except(toBeRemoved).ToArray();
			}
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Add Effect")) {
				newEffects = newEffects.Concat(new StatEffect[]{new StatEffect()}).ToArray();
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
		} 
		EditorGUILayout.EndVertical();
		return newEffects;
	}

	public static StatEffectGroup[] StatEffectGroupsGUI(string name, StatEffectGroup[] fxgs, StatEffectContext ctx, string[] formulaOptions, string lastFocusedControl) {
		if(fxgs == null) { fxgs = new StatEffectGroup[0]; }
		StatEffectGroup[] newFXGs = fxgs;
		EditorGUILayout.BeginVertical();
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.Space();
		int arraySize = EditorGUILayout.IntField(fxgs.Length, GUILayout.Width(32));
		GUILayout.Label(" "+name+(fxgs.Length == 1 ? "" : "s"));
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		if (arraySize != fxgs.Length)
			newFXGs = new StatEffectGroup[arraySize];
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.Space();
		EditorGUILayout.BeginVertical();
   	EditorGUIUtility.LookLikeControls();
		for (int i = 0; i < arraySize; i++)
		{
	   	EditorGUIUtility.LookLikeControls();
			StatEffectGroup entry = null;
			if (i < fxgs.Length) {
				entry = fxgs[i];
			} else {
				entry = new StatEffectGroup();
			}
			GUILayout.Label("Group "+i);
			entry.effects = StatEffectFoldout("in "+name+" "+i, entry.effects, ctx, formulaOptions, lastFocusedControl, ref entry.editorDisplayEffects, false);
			newFXGs[i] = entry;
		}
		EditorGUILayout.EndVertical();
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
   	EditorGUIUtility.LookLikeControls();
		return newFXGs;
	}

	public static List<Parameter> ParameterFoldout(string name, List<Parameter> parameters, string[] formulaOptions, string lastFocusedControl, ref bool foldout) {
		EditorGUILayout.BeginVertical();
		List<Parameter> newParams = parameters;
		foldout = EditorGUILayout.Foldout(foldout, ""+parameters.Count+" "+name+(parameters.Count == 1 ? "" : "s"));
		if(foldout) {
			EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(Screen.width-16));
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();
			List<Parameter> toBeRemoved = null;
			for(int pi = 0; pi < parameters.Count; pi++) {
				Parameter p = parameters[pi];
				Parameter newP = ParameterField(p, name+pi, formulaOptions, lastFocusedControl, pi);
				if(newP == null) {
					if(toBeRemoved == null) { toBeRemoved = new List<Parameter>(); }
					toBeRemoved.Add(p);
				} else {
					parameters[pi] = newP;
				}
				EditorGUILayout.Space();
			}
			if(toBeRemoved != null) {
				newParams = parameters.Except(toBeRemoved).ToList();
			}
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Add Parameter")) {
				newParams = newParams.Concat(new List<Parameter>{new Parameter("parameter", Formula.Constant(0))}).ToList();
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
		} 
		EditorGUILayout.EndVertical();
		return newParams;
	}
	public static StatChange[] StatChangeFoldout(string label, StatChange[] changes, ref bool foldout, float width=-1) {
		if(width == -1) {
			EditorGUILayout.BeginVertical();
		} else {
			EditorGUILayout.BeginVertical(GUILayout.Width(width));
		}
		EditorGUIUtility.LookLikeInspector();
		foldout = EditorGUILayout.Foldout(foldout, label);
		if(changes == null) {
			changes = new StatChange[0];
		}
		StatChange[] newArray = changes;
		if(foldout) {
			if(width == -1) {
				EditorGUILayout.BeginHorizontal();
			} else {
				EditorGUILayout.BeginHorizontal(GUILayout.Width(width));
			}
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();
			int arraySize = EditorGUILayout.IntField("Size", newArray.Length);
			if (arraySize != newArray.Length)
				newArray = new StatChange[arraySize];
			for (int i = 0; i < arraySize; i++)
			{
				StatChange entry = new StatChange("health", StatChangeType.Decrease);
				if (i < changes.Length) {
					entry = changes[i];
				}
				EditorGUILayout.BeginHorizontal();
				entry.statName = EditorGUILayout.TextField(entry.statName);
				entry.changeType = (StatChangeType)EditorGUILayout.EnumPopup(entry.changeType);
				newArray[i] = entry;
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
		}
   	EditorGUIUtility.LookLikeControls();
		EditorGUILayout.EndVertical();
		return newArray;
	}
	public static T[] ObjectArrayFoldout<T>(string label, T[] array, ref bool foldout, float width=-1) where T:UnityEngine.Object
	{
		if(width == -1) {
			EditorGUILayout.BeginVertical(GUILayout.MaxWidth(Screen.width-24));
		} else {
			EditorGUILayout.BeginVertical(GUILayout.Width(width));
		}
		EditorGUIUtility.LookLikeInspector();
		foldout = EditorGUILayout.Foldout(foldout, label);
		T[] newArray = array;
		if(foldout) {
			if(width == -1) {
				EditorGUILayout.BeginHorizontal();
			} else {
				EditorGUILayout.BeginHorizontal(GUILayout.Width(width));
			}
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();
			int arraySize = EditorGUILayout.IntField("Size", array.Length);
			if (arraySize != array.Length)
				newArray = new T[arraySize];
			for (int i = 0; i < arraySize; i++)
			{
				T entry = default(T);
				if (i < array.Length) {
					entry = array[i];
				}
				newArray[i] = EditorGUILayout.ObjectField(entry as UnityEngine.Object, typeof(T), false) as T;
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
		}
   	EditorGUIUtility.LookLikeControls();
		EditorGUILayout.EndVertical();
		return newArray;
	}
	
	public static ActionStrategy StrategyGUI(ActionStrategy strat, bool showEffect=true) {
		ActionStrategy newStrat = strat;
		GUILayout.BeginHorizontal();
		newStrat.canCrossWalls = !EditorGUILayout.Toggle("Walls Block Targeting", !strat.canCrossWalls);
		if(showEffect) {
			newStrat.canEffectCrossWalls = !EditorGUILayout.Toggle("Walls Block Effect", !strat.canEffectCrossWalls);
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		newStrat.canCrossEnemies = !EditorGUILayout.Toggle("Enemies Block Targeting", !strat.canCrossEnemies);
		if(showEffect) {
			newStrat.canEffectCrossEnemies = !EditorGUILayout.Toggle("Enemies Block Effect", !strat.canEffectCrossEnemies);
		}
		GUILayout.EndHorizontal();
		return newStrat;
	}
}
