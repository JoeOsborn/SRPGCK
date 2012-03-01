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
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();
			int arraySize = array.Length;
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Size", GUILayout.Height(18));
			GUILayout.FlexibleSpace();
			arraySize = EditorGUILayout.IntField(array.Length, EditorStyles.textField, GUILayout.Height(18));
			EditorGUILayout.EndHorizontal();
			if(arraySize != array.Length) {
				newArray = new string[arraySize];
			}
			for(int i = 0; i < arraySize; i++) {
				string entry = defaultString;
				if(i < array.Length) {
					entry = array[i];
				}
				if(showElementName) {
					newArray[i] = EditorGUILayout.TextField("Element " + i, entry);
				} else {
					newArray[i] = EditorGUILayout.TextField(entry);
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

	public static Formula FormulaField(string label, Formula f, string type, string[] formulaOptions, string lastFocusedControl=null, int i=0) {
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
			selection = EditorGUILayout.Popup(label, selection, formulaOptions);
		}
		if(selection == 0 || formulaOptions == null) {
			if(lastSelection != selection) {
				f = Formula.Constant(0);
			}
			string name = type+".formula."+i;
			GUI.SetNextControlName(name);
			bool priorWrap = EditorStyles.textField.wordWrap;
			EditorStyles.textField.wordWrap = true;
			f.text = EditorGUILayout.TextField(f.text).RemoveControlCharacters();
			EditorStyles.textField.wordWrap = priorWrap;
			if(GUI.GetNameOfFocusedControl() == name) {
				FormulaCompiler.CompileInPlace(f);
			}
			if(f.compilationError != null && f.compilationError != "") {
				EditorGUILayout.HelpBox(f.compilationError, MessageType.Error);
			}
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
		newP.Formula = EditorGUIExt.FormulaField("Formula:", p.Formula, type, formulaOptions, lastFocusedControl, i);
		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if(GUILayout.Button("Delete", GUILayout.Width(64))) {
			newP = null;
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
		return newP;
	}

	public static StatEffect StatEffectField(StatEffect fx, StatEffectContext ctx, string type, string[] formulaOptions, string lastFocusedControl=null, int i = 0) {
		StatEffect newFx = fx;
		GUILayout.BeginVertical();
		newFx.effectType = (StatEffectType)EditorGUILayout.EnumPopup("Effect Type:", fx.effectType);
		switch(newFx.effectType) {
			case StatEffectType.Augment:
			case StatEffectType.Multiply:
			case StatEffectType.Replace:
				newFx.statName = EditorGUILayout.TextField("Stat Name:", fx.statName == null ? "" : fx.statName).NormalizeName();
				newFx.value = EditorGUIExt.FormulaField("Formula:", fx.value, type, formulaOptions, lastFocusedControl, i);
			break;
			case StatEffectType.ChangeFacing:
				newFx.value = EditorGUIExt.FormulaField("Facing:", fx.value, type, formulaOptions, lastFocusedControl, i);
			break;
			case StatEffectType.EndTurn:
			break;
			case StatEffectType.Knockback:
				newFx.knockbackAngle = EditorGUIExt.FormulaField("Angle:", fx.knockbackAngle, type, formulaOptions, lastFocusedControl, i);
				EditorGUILayout.HelpBox("This angle formula can refer to c.facing, target.facing, or arg.angle.xy to find candidate directions!", MessageType.Info);
				newFx.value = EditorGUIExt.FormulaField("Distance:", fx.value, type, formulaOptions, lastFocusedControl, i);
			break;
		}
		if(ctx == StatEffectContext.Action) {
			newFx.target = (StatEffectTarget)EditorGUILayout.EnumPopup("Target:", fx.target);
			newFx.reactableTypes = ArrayFoldout("Reactable Types", fx.reactableTypes, ref newFx.editorShowsReactableTypes, false, Screen.width/2-32, "attack");
		}
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if(GUILayout.Button("Delete", GUILayout.Width(64))) {
			newFx = null;
		}
		GUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
		return newFx;
	}

	public static StatEffect[] StatEffectFoldout(string name, StatEffect[] effects, StatEffectContext ctx, string[] formulaOptions, string lastFocusedControl, ref bool foldout, bool pluralize=true) {
		EditorGUILayout.BeginVertical();
		if(effects == null) { effects = new StatEffect[0]; }
		StatEffect[] newEffects = effects;
		foldout = EditorGUILayout.Foldout(foldout, ""+effects.Length+" "+name+(!pluralize || effects.Length == 1 ? "" : "s"));
		if(foldout) {
			EditorGUILayout.BeginHorizontal();
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
		if(parameters == null) { parameters = new List<Parameter>(); }
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
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Size", GUILayout.Height(18));
			GUILayout.FlexibleSpace();
			int arraySize = EditorGUILayout.IntField(newArray.Length, EditorStyles.textField, GUILayout.Height(18));
			EditorGUILayout.EndHorizontal();
			if(arraySize != newArray.Length) {
				newArray = new StatChange[arraySize];
			}
			for(int i = 0; i < arraySize; i++)	{
				StatChange entry = new StatChange("health", StatChangeType.Decrease);
				if(i < changes.Length) {
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
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Size", GUILayout.Height(18));
			GUILayout.FlexibleSpace();
			if(newArray == null) { newArray = new T[0]; }
			int arraySize = EditorGUILayout.IntField(newArray.Length, EditorStyles.textField, GUILayout.Height(18));
			EditorGUILayout.EndHorizontal();
			if (arraySize != newArray.Length) {
				newArray = new T[arraySize];
			}
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

	static GUIContent[] regionTypes;
	static GUIContent[] spaceTypes;
	static GUIContent[] canCrossWallsFlags;
	static GUIContent[] canCrossEnemiesFlags;
	static GUIContent[] canHaltAtEnemiesFlags;
	static GUIContent[] useAbsoluteDZFlags;
	static GUIContent[] useArcRangeBonusFlags;
	static GUIStyle _imageButtonGridStyle;
	public static GUIStyle imageButtonGridStyle { get {
		if(_imageButtonGridStyle == null) {
			_imageButtonGridStyle = new GUIStyle(GUI.skin.GetStyle("Button"));
			_imageButtonGridStyle.imagePosition = ImagePosition.ImageAbove;
			_imageButtonGridStyle.alignment = TextAnchor.LowerCenter;
			_imageButtonGridStyle.fixedWidth = buttonDim;
			_imageButtonGridStyle.fixedHeight = buttonDim;
			_imageButtonGridStyle.stretchWidth = false;
			_imageButtonGridStyle.stretchHeight = false;
			_imageButtonGridStyle.margin = new RectOffset(2,2,2,2);
			_imageButtonGridStyle.padding = new RectOffset(0,0,0,6);
		}
		return _imageButtonGridStyle;
	} }
	public static float buttonDim = 88;

	public static Region RegionGUI(
		string label, string type,
		ref bool open,
		Region reg,
		string[] formulaOptions,
		float width,
		int subregionIndex=-1
	) {
		if(regionTypes == null) {
			regionTypes = new GUIContent[]{
				new GUIContent("Cylinder", EditorGUIUtility.LoadRequired("rgn-cylinder.png") as Texture),
				new GUIContent("Sphere", EditorGUIUtility.LoadRequired("rgn-sphere.png") as Texture),
				new GUIContent("Line", EditorGUIUtility.LoadRequired("rgn-line.png") as Texture),
				new GUIContent("Cone", EditorGUIUtility.LoadRequired("rgn-cone.png") as Texture),
				new GUIContent("Self", EditorGUIUtility.LoadRequired("rgn-self.png") as Texture),
				new GUIContent("Predicate", EditorGUIUtility.LoadRequired("rgn-predicate.png") as Texture),
				new GUIContent("Compound", EditorGUIUtility.LoadRequired("rgn-compound.png") as Texture)
			};
		}
		if(spaceTypes == null) {
			spaceTypes = new GUIContent[]{
				new GUIContent("Pick", EditorGUIUtility.LoadRequired("intv-pick.png") as Texture),
				new GUIContent("Path", EditorGUIUtility.LoadRequired("intv-path.png") as Texture),
				new GUIContent("Line", EditorGUIUtility.LoadRequired("intv-line.png") as Texture),
				new GUIContent("Arc", EditorGUIUtility.LoadRequired("intv-arc.png") as Texture),
			};
		}
		if(canCrossWallsFlags == null) {
			canCrossWallsFlags = new GUIContent[]{
				new GUIContent("Don't Cross", EditorGUIUtility.LoadRequired("cancross-walls-no.png") as Texture),
				new GUIContent("Cross", EditorGUIUtility.LoadRequired("cancross-walls.png") as Texture)
			};
		}
		if(canCrossEnemiesFlags == null) {
			canCrossEnemiesFlags = new GUIContent[]{
				new GUIContent("Don't Cross", EditorGUIUtility.LoadRequired("cancross-enemies-no.png") as Texture),
				new GUIContent("Cross", EditorGUIUtility.LoadRequired("cancross-enemies.png") as Texture)
			};
		}
		if(canHaltAtEnemiesFlags == null) {
			canHaltAtEnemiesFlags = new GUIContent[]{
				new GUIContent("Stop Before", EditorGUIUtility.LoadRequired("canstop-enemies-no.png") as Texture),
				new GUIContent("Stop At", EditorGUIUtility.LoadRequired("canstop-enemies.png") as Texture)
			};
		}
		if(useAbsoluteDZFlags == null) {
			useAbsoluteDZFlags = new GUIContent[]{
				new GUIContent("Relative", EditorGUIUtility.LoadRequired("dz-rel.png") as Texture),
				new GUIContent("Absolute", EditorGUIUtility.LoadRequired("dz-abs.png") as Texture)
			};
		}
		if(useArcRangeBonusFlags == null) {
			useArcRangeBonusFlags = new GUIContent[]{
				new GUIContent("No Bonus", EditorGUIUtility.LoadRequired("arc-bonus-off.png") as Texture),
				new GUIContent("Height Bonus", EditorGUIUtility.LoadRequired("arc-bonus-on.png") as Texture)
			};
		}
		Region newReg = reg;
		if(newReg == null) { newReg = new Region(); }
		if(subregionIndex == -1 &&
		   !(open = EditorGUILayout.Foldout(open, label))) {
			return reg;
		}
		int buttonsWide = (int)(width/buttonDim);
		GUILayout.Label("Region Type");
		newReg.type = (RegionType)GUILayout.SelectionGrid((int)newReg.type, regionTypes, buttonsWide, imageButtonGridStyle);
		//intervening space type (gfx)
		if(newReg.type == RegionType.Self) {
			newReg.interveningSpaceType = InterveningSpaceType.Pick;
			newReg.canCrossWalls = false;
			newReg.canCrossEnemies = false;
			newReg.canHaltAtEnemies = false;
		} else {
			if(subregionIndex == -1) {
				GUILayout.Label("Intervening Space Type");
				newReg.interveningSpaceType = (InterveningSpaceType)GUILayout.SelectionGrid((int)newReg.interveningSpaceType, spaceTypes, buttonsWide, imageButtonGridStyle);
				if(newReg.interveningSpaceType != InterveningSpaceType.Pick) {
					//can cross walls yes/no
					GUILayout.Label("Can cross walls?");
					newReg.canCrossWalls = GUILayout.SelectionGrid(newReg.canCrossWalls ? 1 : 0, canCrossWallsFlags, 2, imageButtonGridStyle) == 1 ? true : false;
					//can cross enemies yes/no
					GUILayout.Label("Can cross enemies?");
					newReg.canCrossEnemies = GUILayout.SelectionGrid(newReg.canCrossEnemies ? 1 : 0, canCrossEnemiesFlags, 2, imageButtonGridStyle) == 1 ? true : false;
				} else {
					newReg.canCrossWalls = true;
					newReg.canCrossEnemies = true;
				}
				//can halt on enemies yes/no
				GUILayout.Label("Can halt on enemies?");
				newReg.canHaltAtEnemies = GUILayout.SelectionGrid(newReg.canHaltAtEnemies ? 1 : 0, canHaltAtEnemiesFlags, 2, imageButtonGridStyle) == 1 ? true : false;
				//abs dz yes/no
				if(newReg.interveningSpaceType != InterveningSpaceType.Pick &&
				   newReg.type != RegionType.Line &&
				   newReg.type != RegionType.Cone) {
					GUILayout.Label("Use absolute DZ?");
					newReg.useAbsoluteDZ = GUILayout.SelectionGrid(newReg.useAbsoluteDZ ? 1 : 0, useAbsoluteDZFlags, 2, imageButtonGridStyle) == 1 ? true : false;
				} else {
					newReg.useAbsoluteDZ = true;
				}
				//arc bonus yes/no if intervening space is arc
				if(newReg.interveningSpaceType == InterveningSpaceType.Arc) {
					GUILayout.Label("Arc range bonus?");
					newReg.useArcRangeBonus = GUILayout.SelectionGrid(newReg.useArcRangeBonus ? 1 : 0, useArcRangeBonusFlags, 2, imageButtonGridStyle) == 1 ? true : false;
				} else {
					newReg.useArcRangeBonus = false;
				}
			}
			string prefix = type+".region."+label+".";
			if(newReg.type == RegionType.Cylinder ||
				 newReg.type == RegionType.Sphere   ||
				 newReg.type == RegionType.Cone     ||
				 newReg.type == RegionType.Line     ||
				 newReg.type == RegionType.Predicate) {
				//radius min, radius max
			 	newReg.radiusMinF = EditorGUIExt.FormulaField(
					"Radius Min",
					newReg.radiusMinF,
					prefix+"radiusMinF",
					formulaOptions
				);
			 	newReg.radiusMaxF = EditorGUIExt.FormulaField(
					"Radius Max",
					newReg.radiusMaxF,
					prefix+"radiusMaxF",
					formulaOptions
				);

			  if(newReg.type != RegionType.Cone) {
			  	//z up/down min/max
				 	newReg.zUpMinF = EditorGUIExt.FormulaField(
						"Z Up Min",
						newReg.zUpMinF,
						prefix+"zUpMinF",
						formulaOptions
					);
				 	newReg.zUpMaxF = EditorGUIExt.FormulaField(
						"Z Up Max",
						newReg.zUpMaxF,
						prefix+"zUpMax",
						formulaOptions
					);
				 	newReg.zDownMinF = EditorGUIExt.FormulaField(
						"Z Down Min",
						newReg.zDownMinF,
						prefix+"zDownMinF",
						formulaOptions
					);
				 	newReg.zDownMaxF = EditorGUIExt.FormulaField(
						"Z Down Max",
						newReg.zDownMaxF,
						prefix+"zDownMax",
						formulaOptions
					);
			  }
			}
			if(newReg.type == RegionType.Cone ||
				 newReg.type == RegionType.Line) {
				//xyDirection, zDirection
			 	newReg.xyDirectionF = EditorGUIExt.FormulaField(
					"XY Direction",
					newReg.xyDirectionF,
					prefix+"xyDirectionF",
					formulaOptions
				);
			 	newReg.zDirectionF = EditorGUIExt.FormulaField(
					"Z Direction",
					newReg.zDirectionF,
					prefix+"zDirectionF",
					formulaOptions
				);
			}
			if(newReg.type == RegionType.Cone) {
				//xyArcMin/Max
			 	newReg.xyArcMinF = EditorGUIExt.FormulaField(
					"XY Arc Min",
					newReg.xyArcMinF,
					prefix+"xyArcMinF",
					formulaOptions
				);
			 	newReg.xyArcMaxF = EditorGUIExt.FormulaField(
					"XY Arc Max",
					newReg.xyArcMaxF,
					prefix+"xyArcMax",
					formulaOptions
				);
				//zArcMin/Max
			 	newReg.zArcMinF = EditorGUIExt.FormulaField(
					"Z Arc Min",
					newReg.zArcMinF,
					prefix+"zArcMinF",
					formulaOptions
				);
			 	newReg.zArcMaxF = EditorGUIExt.FormulaField(
					"Z Arc Max",
					newReg.zArcMaxF,
					prefix+"zArcMaxF",
					formulaOptions
				);
				//rFwdClipMax
			 	newReg.rFwdClipMaxF = EditorGUIExt.FormulaField(
					"Forward Distance Max",
					newReg.rFwdClipMaxF,
					prefix+"rFwdClipMaxF",
					formulaOptions
				);
			}
			if(newReg.type == RegionType.Line) {
				//line width min/max
			 	newReg.lineWidthMinF = EditorGUIExt.FormulaField(
					"Line Width Min",
					newReg.lineWidthMinF,
					prefix+"lineWidthMinF",
					formulaOptions
				);
			 	newReg.lineWidthMaxF = EditorGUIExt.FormulaField(
					"Line Width Max",
					newReg.lineWidthMaxF,
					prefix+"lineWidthMaxF",
					formulaOptions
				);
			}
			if(newReg.type == RegionType.Predicate) {
				//predicateF
			 	newReg.predicateF = EditorGUIExt.FormulaField(
					"Predicate",
					newReg.predicateF,
					prefix+"predicateF",
					formulaOptions
				);
				//TODO: info box with available bindings?
			}
			if(newReg.type == RegionType.Compound) {
				//regions, but without UI for intervening space, cross/halt walls/enemies
				//size
				EditorGUILayout.Space();
				GUILayout.BeginHorizontal();
				if(newReg.regions == null) { newReg.regions = new Region[0]; }
				int regionCount = newReg.regions.Length;
				int newRegionCount = EditorGUILayout.IntField(regionCount, EditorStyles.textField, GUILayout.Height(18), GUILayout.Width(24));
				GUILayout.Label("Subregion"+(newRegionCount > 1 ? "s" : ""));
				GUILayout.EndHorizontal();
				if(newRegionCount < 1) {
					newRegionCount = 1;
				}
				if(newRegionCount != regionCount) {
					System.Array.Resize(ref newReg.regions, newRegionCount);
				}
				for(int i = 0; i < newRegionCount; i++) {
					EditorGUILayout.Space();
					EditorGUILayout.Space();
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.Space();
					EditorGUILayout.BeginVertical();
				  //0...i regiongui(...,i)
					bool ignoreFold = true;
					newReg.regions[i] = RegionGUI(label+" subregion "+i, prefix, ref ignoreFold, newReg.regions[i], formulaOptions, width-16, i);
					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.Space();
				}
			}
		}
		return newReg;
	}
}
