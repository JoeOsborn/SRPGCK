using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

public class EditorGUIExt
{
	public static void ArrayField (SerializedProperty property)
	{
		EditorGUIUtility.LookLikeInspector ();
		bool wasEnabled = GUI.enabled;
		int prevIdentLevel = EditorGUI.indentLevel;

		bool childrenAreExpanded = true;
		int propertyStartingDepth = property.depth;
		while(property.NextVisible(childrenAreExpanded) && propertyStartingDepth < property.depth)
		{
			childrenAreExpanded = EditorGUILayout.PropertyField(property);
		}

		EditorGUI.indentLevel = prevIdentLevel;
		GUI.enabled = wasEnabled;
   	EditorGUIUtility.LookLikeControls();
	}

	public static string FileLabel(string name, float labelWidth, string path, string extension)
	{
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label(name, GUILayout.MaxWidth(labelWidth));
		string filepath = EditorGUILayout.TextField(path);
		if(GUILayout.Button("Browse"))
		{
			filepath = EditorUtility.OpenFilePanel(name, path, extension);
		}
		EditorGUILayout.EndHorizontal();
		return filepath;
	}

	public static string FolderLabel(string name, float labelWidth, string path)
	{
		EditorGUILayout.BeginHorizontal();
		string filepath = EditorGUILayout.TextField(name, path);
		if(GUILayout.Button("Browse", GUILayout.MaxWidth(60)))
		{
			filepath = EditorUtility.SaveFolderPanel(name, path, "Folder");
		}
		EditorGUILayout.EndHorizontal();
		return filepath;
	}

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

	public static Enum EnumToolbar(Enum selected)
	{
		string[] toolbar = System.Enum.GetNames(selected.GetType());
		Array values = System.Enum.GetValues(selected.GetType());
		int selected_index = 0;
		while(selected_index < values.Length)
		{
			if(selected.ToString() == values.GetValue(selected_index).ToString())
			{
				break;
			}
			selected_index++;
		}
		selected_index = GUILayout.Toolbar(selected_index, toolbar);
		return (Enum) values.GetValue(selected_index);
	}

	public const float compileInterval = 1.0f;
	static float nextCompileTime = 0;

	public static Formula FormulaField(string label, Formula f, string type, string[] formulaOptions, string lastFocusedControl, int i=0) {
		int selection= 0;
		int lastSelection = 0;
		EditorGUILayout.BeginVertical();
		if(f == null || f.text == null || f.text == "") {
			f = Formula.Constant(0);
			f.text = "0";
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
			GUI.SetNextControlName("");
			EditorStyles.textField.wordWrap = priorWrap;
			if(GUI.GetNameOfFocusedControl() != name && lastFocusedControl == name) {
				FormulaCompiler.CompileInPlace(f);
			} else if(GUI.GetNameOfFocusedControl() == name) {
				float now = (float)EditorApplication.timeSinceStartup;
				if(now >= nextCompileTime || lastFocusedControl != name) {
					nextCompileTime = now + compileInterval;
					FormulaCompiler.CompileInPlace(f);
				}
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

	public static Parameter ParameterField(Parameter p, string type, string[] formulaOptions, string lastFocusedControl=null, int i = 0, string[] skipParams=null) {
		Parameter newP = p;
		EditorGUILayout.BeginVertical();
		string newName = EditorGUILayout.TextField(
			"Name:",
			p.Name == null ? "" : p.Name
		).NormalizeName();
		if(skipParams == null || !skipParams.Contains(newName)) {
			newP.Name = newName;
		}
		newP.Formula = FormulaField(
			"Formula:",
			p.Formula,
			type,
			formulaOptions,
			lastFocusedControl,
			i
		);
		if((newP.limitMinimum = EditorGUILayout.Toggle(
			"Limit Min",
			newP.limitMinimum
		))) {
			newP.minF = FormulaField(
				"Minimum:",
				p.minF,
				type+".limit.min",
				formulaOptions,
				lastFocusedControl,
				i
			);
		}
		if((newP.limitMaximum = EditorGUILayout.Toggle(
			"Limit Max",
			newP.limitMaximum
		))) {
			newP.maxF = FormulaField(
				"Maximum:",
				p.maxF,
				type+".limit.max",
				formulaOptions,
				lastFocusedControl,
				i
			);
		}

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if(GUILayout.Button("Delete", GUILayout.Width(64))) {
			newP = null;
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
		return newP;
	}

	protected static StatEffect StatEffectFieldCore(StatEffect fx, StatEffectContext ctx, string type, string[] formulaOptions, string lastFocusedControl, int i = -1) {
		StatEffect newFx = fx;
		GUILayout.BeginVertical();
		newFx.effectType = (StatEffectType)EditorGUILayout.EnumPopup("Effect Type:", fx.effectType);
		if(newFx.specialMoveGivenStartX == null || fx == null) {
			newFx.specialMoveGivenStartX = Formula.Lookup(
				"arg.x",
				LookupType.SkillParam
			);
		}
		if(newFx.specialMoveGivenStartY == null || fx == null) {
			newFx.specialMoveGivenStartY = Formula.Lookup(
				"arg.y",
				LookupType.SkillParam
			);
		}
		if(newFx.specialMoveGivenStartZ == null || fx == null) {
			newFx.specialMoveGivenStartZ = Formula.Lookup(
				"arg.z",
				LookupType.SkillParam
			);
		}
		switch(newFx.effectType) {
			case StatEffectType.Augment:
			case StatEffectType.Multiply:
			case StatEffectType.Replace:
				newFx.statName = EditorGUILayout.TextField(
					"Stat Name:",
					fx.statName == null ? "" : fx.statName
				).NormalizeName();
				newFx.value = EditorGUIExt.FormulaField(
					"Formula:",
					fx.value,
					type+".value",
					formulaOptions,
					lastFocusedControl,
					i
				);
				newFx.respectLimits = EditorGUILayout.Toggle(
					"Respect Limits:",
					newFx.respectLimits
				);
				newFx.constrainValueToLimits = EditorGUILayout.Toggle(
					"Constrain Value to Limits:",
					newFx.constrainValueToLimits
				);
				break;
			case StatEffectType.ChangeFacing:
				newFx.value = EditorGUIExt.FormulaField(
					"Facing:",
					fx.value,
					type+".value",
					formulaOptions,
					lastFocusedControl,
					i
				);
				break;
			case StatEffectType.EndTurn:
				break;
			case StatEffectType.SpecialMove:
				if(newFx.specialMoveLine == null ||
				   newFx.specialMoveLine.type != RegionType.LineMove) {
					newFx.specialMoveLine = new Region();
					newFx.specialMoveLine.type = RegionType.LineMove;
					newFx.specialMoveLine.interveningSpaceType = InterveningSpaceType.LineMove;
					newFx.specialMoveLine.radiusMinF = Formula.Constant(0);
					newFx.specialMoveLine.radiusMaxF = Formula.Constant(1);
					newFx.specialMoveLine.zUpMaxF = Formula.Constant(0);
					newFx.specialMoveLine.zDownMaxF = Formula.Constant(0);
					newFx.specialMoveLine.xyDirectionF = Formula.Constant(0);
					newFx.specialMoveLine.canCrossWalls = false;
					newFx.specialMoveLine.canCrossEnemies = false;
					newFx.specialMoveLine.canHaltAtEnemies = false;
					newFx.specialMoveLine.canGlide = false;
					newFx.specialMoveLine.facingLock = FacingLock.Cardinal;
				}
				newFx.specialMoveType = EditorGUILayout.TextField(
					"Move Type:",
					newFx.specialMoveType
				).NormalizeName();
				newFx.specialMoveLine = EditorGUIExt.SimpleRegionGUI(
					type+".specialMoveRegion",
					newFx.specialMoveLine,
					formulaOptions,
					lastFocusedControl,
					Screen.width,
					0
				);
				newFx.specialMoveGivenStartX = EditorGUIExt.FormulaField(
					"Start X:",
					newFx.specialMoveGivenStartX,
					type+".specialMoveStart.x",
					formulaOptions,
					lastFocusedControl,
					i
				);
				newFx.specialMoveGivenStartY = EditorGUIExt.FormulaField(
					"Start Y:",
					newFx.specialMoveGivenStartY,
					type+".specialMoveStart.y",
					formulaOptions,
					lastFocusedControl,
					i
				);
				newFx.specialMoveGivenStartZ = EditorGUIExt.FormulaField(
					"Start Z:",
					newFx.specialMoveGivenStartZ,
					type+".specialMoveStart.z",
					formulaOptions,
					lastFocusedControl,
					i
				);
				newFx.specialMoveAnimateToStart = EditorGUILayout.Toggle(
					"Animate to Start Position",
					newFx.specialMoveAnimateToStart
				);
				newFx.specialMoveSpeedXY = EditorGUILayout.FloatField(
					"Move Speed XY:",
					newFx.specialMoveSpeedXY
				);
				newFx.specialMoveSpeedZ = EditorGUILayout.FloatField(
					"Move Speed Z:",
					newFx.specialMoveSpeedZ
				);
				break;
		}
		if(ctx == StatEffectContext.Action || ctx == StatEffectContext.Any) {
			newFx.target = (StatEffectTarget)EditorGUILayout.EnumPopup("Target:", fx.target);
			newFx.reactableTypes = ArrayFoldout(
				"Reactable Types",
				fx.reactableTypes,
				ref newFx.editorShowsReactableTypes,
				false,
				Screen.width/2-32,
				"attack"
			);
		}
		return newFx;
	}

	public static StatEffect StatEffectField(StatEffect fx, StatEffectContext ctx, string type, string[] formulaOptions, string lastFocusedControl, int i = 0) {
		StatEffect newFx = fx ?? new StatEffect();
		if(i != -1) {
			if(!(newFx.editorShow = EditorGUILayout.Foldout(newFx.editorShow, "Stat Effect"))) {
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if(GUILayout.Button("Delete", GUILayout.Width(64))) {
					newFx = null;
				}
				GUILayout.EndHorizontal();
				return newFx;
			}
		}
		newFx = StatEffectFieldCore(newFx, ctx, type, formulaOptions, lastFocusedControl, i);
		if(i != -1) {
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Delete", GUILayout.Width(64))) {
				newFx = null;
			}
			GUILayout.EndHorizontal();
		}
		EditorGUILayout.EndVertical();
		return newFx;
	}

	public static StatEffect[] StatEffectFoldout(string name, StatEffect[] effects, StatEffectContext ctx, string id, string[] formulaOptions, string lastFocusedControl, ref bool foldout, bool pluralize=true) {
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
				StatEffect newFx = StatEffectField(fx, ctx, id+name+fxi, formulaOptions, lastFocusedControl, fxi);
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

	public static StatEffectGroup StatEffectGroupGUI(string label, StatEffectGroup g, StatEffectContext ctx, string id, string[] formulaOptions, string lastFocusedControl) {
		if(g == null) { g = new StatEffectGroup(); }
		g.effects = StatEffectFoldout(label, g.effects, ctx, id, formulaOptions, lastFocusedControl, ref g.editorDisplayEffects, false);
		return g;
	}

	public static StatEffectGroup[] StatEffectGroupsGUI(string name, StatEffectGroup[] fxgs, StatEffectContext ctx, string id, string[] formulaOptions, string lastFocusedControl) {
		if(fxgs == null) { fxgs = new StatEffectGroup[0]; }
		StatEffectGroup[] newFXGs = fxgs;
		EditorGUILayout.BeginVertical();
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.Space();
		int arraySize = EditorGUILayout.IntField(fxgs.Length, GUILayout.Width(32));
		GUILayout.Label(" "+name+(fxgs.Length == 1 ? "" : "s"));
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		if(arraySize != fxgs.Length) {
			newFXGs = new StatEffectGroup[arraySize];
		}
		EditorGUI.indentLevel++;
		EditorGUILayout.BeginVertical();
   	EditorGUIUtility.LookLikeControls();
		for(int i = 0; i < arraySize; i++)
		{
			StatEffectGroup entry = null;
			if(i < fxgs.Length) {
				entry = fxgs[i];
			} else {
				entry = new StatEffectGroup();
			}
			GUILayout.Label("Group "+i);
			entry.effects = StatEffectFoldout("in "+name+" "+i, entry.effects, ctx, id, formulaOptions, lastFocusedControl, ref entry.editorDisplayEffects, false);
			newFXGs[i] = entry;
		}
		EditorGUILayout.EndVertical();
		EditorGUI.indentLevel--;
		EditorGUILayout.EndVertical();
   	EditorGUIUtility.LookLikeControls();
		return newFXGs;
	}

	public static List<Parameter> ParameterFoldout(string name, List<Parameter> parameters, string id, string[] formulaOptions, string lastFocusedControl, ref bool foldout, string[] skipParams=null) {
		EditorGUILayout.BeginVertical();
		if(parameters == null) { parameters = new List<Parameter>(); }
		List<Parameter> newParams = parameters;
		int shownCount = parameters.Count-(skipParams != null ? skipParams.Length : 0);
		foldout = EditorGUILayout.Foldout(foldout, ""+shownCount+" "+name+(shownCount == 1 ? "" : "s"));
		if(foldout) {
			EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(Screen.width-16));
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();
			List<Parameter> toBeRemoved = null;
			for(int pi = 0; pi < parameters.Count; pi++) {
				Parameter p = parameters[pi];
				if(skipParams != null && skipParams.Contains(p.Name)) {
					continue;
				}
				Parameter newP = ParameterField(p, id+name+pi, formulaOptions, lastFocusedControl, pi, skipParams);
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
			if(arraySize != newArray.Length) {
				newArray = new T[arraySize];
			}
			for(int i = 0; i < arraySize; i++)
			{
				T entry = default(T);
				if(i < array.Length) {
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
	static GUIContent[] canGlideFlags;
	static GUIContent[] facingLockFlags;
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

	public static Region SimpleRegionGUI(
		string type,
		Region reg,
		string[] formulaOptions,
		string lastFocusedControl,
		float width,
		int subregionIndex=-1
	) {
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
		if(canGlideFlags == null) {
			canGlideFlags = new GUIContent[]{
				new GUIContent("Don't Glide", EditorGUIUtility.LoadRequired("canglide-no.png") as Texture),
				new GUIContent("Glide", EditorGUIUtility.LoadRequired("canglide.png") as Texture)
			};
		}
		if(facingLockFlags == null) {
			facingLockFlags = new GUIContent[]{
				new GUIContent("Free Angle", EditorGUIUtility.LoadRequired("lock-free.png") as Texture),
				new GUIContent("Cardinal", EditorGUIUtility.LoadRequired("lock-cardinal.png") as Texture),
				new GUIContent("Ordinal", EditorGUIUtility.LoadRequired("lock-ordinal.png") as Texture),
				new GUIContent("Eight Way", EditorGUIUtility.LoadRequired("lock-8way.png") as Texture)
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
		int buttonsWide = (int)(width/buttonDim);
		Region newReg = reg;
		if(newReg.type == RegionType.Self) {
			return newReg;
		}
		if(newReg.type == RegionType.LineMove) {
			newReg.interveningSpaceType = InterveningSpaceType.LineMove;
		}
		if(subregionIndex == -1 || newReg.type == RegionType.LineMove) {
			if(newReg.interveningSpaceType != InterveningSpaceType.Pick) {
				//can cross walls yes/no
				GUILayout.Label("Can cross walls?");
				newReg.canCrossWalls = GUILayout.SelectionGrid(newReg.canCrossWalls ? 1 : 0, canCrossWallsFlags, 2, imageButtonGridStyle) == 1 ? true : false;
				//can cross enemies yes/no
				if(newReg.interveningSpaceType != InterveningSpaceType.LineMove) {
					GUILayout.Label("Can cross enemies?");
				} else {
					GUILayout.Label("Can cross characters?");
				}
				newReg.canCrossEnemies = GUILayout.SelectionGrid(newReg.canCrossEnemies ? 1 : 0, canCrossEnemiesFlags, 2, imageButtonGridStyle) == 1 ? true : false;
			} else {
				newReg.canCrossWalls = true;
				newReg.canCrossEnemies = true;
			}

			if(newReg.interveningSpaceType == InterveningSpaceType.LineMove) {
				//can cross walls yes/no
				GUILayout.Label("Can glide?");
				newReg.canGlide = GUILayout.SelectionGrid(newReg.canGlide ? 1 : 0, canGlideFlags, 2, imageButtonGridStyle) == 1 ? true : false;
				newReg.preventStuckInAir = (StuckPrevention)EditorGUILayout.EnumPopup("Prevent Sticking in Air", newReg.preventStuckInAir);
				if(newReg.canCrossWalls) {
					newReg.preventStuckInWalls = (StuckPrevention)EditorGUILayout.EnumPopup("Prevent Sticking in Walls", newReg.preventStuckInWalls);
				}
				GUILayout.Label("Locking Mode");
				newReg.facingLock = (FacingLock)GUILayout.SelectionGrid((int)newReg.facingLock, facingLockFlags, buttonsWide, imageButtonGridStyle);
			}
			if(newReg.interveningSpaceType != InterveningSpaceType.LineMove) {
				//can halt on enemies yes/no
				GUILayout.Label("Can halt on enemies?");
				newReg.canHaltAtEnemies = GUILayout.SelectionGrid(newReg.canHaltAtEnemies ? 1 : 0, canHaltAtEnemiesFlags, 2, imageButtonGridStyle) == 1 ? true : false;
			} else {
				newReg.canHaltAtEnemies = true;
			}
			newReg.canMountEnemies = EditorGUILayout.Toggle("Can mount enemies?", newReg.canMountEnemies);
			newReg.canMountFriends = EditorGUILayout.Toggle("Can mount friends?", newReg.canMountFriends);
			//abs dz yes/no
			if(newReg.interveningSpaceType != InterveningSpaceType.Pick &&
				 newReg.type != RegionType.LineMove &&
			   newReg.type != RegionType.Line &&
			   newReg.type != RegionType.Cone) {
				GUILayout.Label("Use absolute DZ?");
				newReg.useAbsoluteDZ = GUILayout.SelectionGrid(newReg.useAbsoluteDZ ? 1 : 0, useAbsoluteDZFlags, 2, imageButtonGridStyle) == 1 ? true : false;
			} else if(newReg.interveningSpaceType != InterveningSpaceType.LineMove) {
				newReg.useAbsoluteDZ = false;
			} else {
				newReg.useAbsoluteDZ = true;
			}
		}
		//arc bonus yes/no if intervening space is arc
		if(newReg.interveningSpaceType == InterveningSpaceType.Arc) {
			GUILayout.Label("Arc range bonus?");
			newReg.useArcRangeBonus = GUILayout.SelectionGrid(newReg.useArcRangeBonus ? 1 : 0, useArcRangeBonusFlags, 2, imageButtonGridStyle) == 1 ? true : false;
		} else {
			newReg.useArcRangeBonus = false;
		}
		string prefix = type+".region.";
		//predicateF
		if(newReg.predicateF == null ||
			 (newReg.predicateF.formulaType == FormulaType.Constant &&
			  newReg.predicateF.constantValue == 0)) {
			newReg.predicateF = Formula.True();
		}
	 	newReg.predicateF = EditorGUIExt.FormulaField(
			"Predicate",
			newReg.predicateF,
			prefix+"predicateF",
			formulaOptions,
			lastFocusedControl
		);
		//TODO: info box with available bindings?
		if(newReg.type == RegionType.Cylinder ||
			 newReg.type == RegionType.Sphere   ||
			 newReg.type == RegionType.Cone     ||
			 newReg.type == RegionType.Line     ||
			 newReg.type == RegionType.LineMove) {
			//radius min, radius max
			if(newReg.type != RegionType.LineMove) {
			 	newReg.radiusMinF = EditorGUIExt.FormulaField(
					"Radius Min",
					newReg.radiusMinF,
					prefix+"radiusMinF",
					formulaOptions,
					lastFocusedControl
				);
			} else {
				newReg.radiusMinF = Formula.Constant(0);
			}
		 	newReg.radiusMaxF = EditorGUIExt.FormulaField(
				"Radius Max",
				newReg.radiusMaxF,
				prefix+"radiusMaxF",
				formulaOptions,
				lastFocusedControl
			);

		  if(newReg.type != RegionType.Cone) {
		  	//z up/down min/max
				if(newReg.type != RegionType.LineMove) {
				 	newReg.zUpMinF = EditorGUIExt.FormulaField(
						"Z Up Min",
						newReg.zUpMinF,
						prefix+"zUpMinF",
						formulaOptions,
						lastFocusedControl
					);
				} else {
					newReg.zUpMinF = Formula.Constant(0);
				}
			 	newReg.zUpMaxF = EditorGUIExt.FormulaField(
					"Z Up Max",
					newReg.zUpMaxF,
					prefix+"zUpMax",
					formulaOptions,
					lastFocusedControl
				);
				if(newReg.type != RegionType.LineMove) {
				 	newReg.zDownMinF = EditorGUIExt.FormulaField(
						"Z Down Min",
						newReg.zDownMinF,
						prefix+"zDownMinF",
						formulaOptions,
						lastFocusedControl
					);
				} else {
					newReg.zDownMinF = Formula.Constant(0);
				}
			 	newReg.zDownMaxF = EditorGUIExt.FormulaField(
					"Z Down Max",
					newReg.zDownMaxF,
					prefix+"zDownMax",
					formulaOptions,
					lastFocusedControl
				);
		  }
		}
		if(newReg.type == RegionType.Cone ||
			 newReg.type == RegionType.Line ||
	     newReg.type == RegionType.LineMove) {
			//xyDirection, zDirection
		 	newReg.xyDirectionF = EditorGUIExt.FormulaField(
				"XY Direction",
				newReg.xyDirectionF,
				prefix+"xyDirectionF",
				formulaOptions,
				lastFocusedControl
			);
			if(newReg.type != RegionType.LineMove) {
			 	newReg.zDirectionF = EditorGUIExt.FormulaField(
					"Z Direction",
					newReg.zDirectionF,
					prefix+"zDirectionF",
					formulaOptions,
					lastFocusedControl
				);
			}
			EditorGUILayout.HelpBox("These angle formulae can refer to c.facing, target.facing, or arg.angle.xy to find candidate directions!", MessageType.Info);
		}
		if(newReg.type == RegionType.Cone) {
			//xyArcMin/Max
		 	newReg.xyArcMinF = EditorGUIExt.FormulaField(
				"XY Arc Min",
				newReg.xyArcMinF,
				prefix+"xyArcMinF",
				formulaOptions,
				lastFocusedControl
			);
		 	newReg.xyArcMaxF = EditorGUIExt.FormulaField(
				"XY Arc Max",
				newReg.xyArcMaxF,
				prefix+"xyArcMax",
				formulaOptions,
				lastFocusedControl
			);
			//zArcMin/Max
		 	newReg.zArcMinF = EditorGUIExt.FormulaField(
				"Z Arc Min",
				newReg.zArcMinF,
				prefix+"zArcMinF",
				formulaOptions,
				lastFocusedControl
			);
		 	newReg.zArcMaxF = EditorGUIExt.FormulaField(
				"Z Arc Max",
				newReg.zArcMaxF,
				prefix+"zArcMaxF",
				formulaOptions,
				lastFocusedControl
			);
			//rFwdClipMax
		 	newReg.rFwdClipMaxF = EditorGUIExt.FormulaField(
				"Forward Distance Max",
				newReg.rFwdClipMaxF,
				prefix+"rFwdClipMaxF",
				formulaOptions,
				lastFocusedControl
			);
		}
		if(newReg.type == RegionType.Line) {
			//line width min/max
		 	newReg.lineWidthMinF = EditorGUIExt.FormulaField(
				"Line Width Min",
				newReg.lineWidthMinF,
				prefix+"lineWidthMinF",
				formulaOptions,
				lastFocusedControl
			);
		 	newReg.lineWidthMaxF = EditorGUIExt.FormulaField(
				"Line Width Max",
				newReg.lineWidthMaxF,
				prefix+"lineWidthMaxF",
				formulaOptions,
				lastFocusedControl
			);
		}
		if(newReg.type == RegionType.Compound || newReg.type == RegionType.NWay) {
			//regions, but without UI for intervening space, cross/halt walls/enemies
			//size
			if(newReg.type == RegionType.NWay) {
				if(newReg.nWaysF == null ||
					 (newReg.nWaysF.formulaType == FormulaType.Constant &&
					  newReg.nWaysF.constantValue == 0)) {
					newReg.nWaysF = Formula.Constant(1);
				}
			 	newReg.nWaysF = EditorGUIExt.FormulaField(
					"Number of Ways",
					newReg.nWaysF,
					prefix+"nWaysF",
					formulaOptions,
					lastFocusedControl
				);
			 	newReg.xyDirectionF = EditorGUIExt.FormulaField(
					"XY Direction Offset",
					newReg.xyDirectionF,
					prefix+"xyDirectionF",
					formulaOptions,
					lastFocusedControl
				);
			}
			EditorGUILayout.Space();
			GUILayout.BeginHorizontal();
			if(newReg.regions == null) { newReg.regions = new Region[]{new Region()}; }
			int regionCount = newReg.regions.Length;
			int newRegionCount = EditorGUILayout.IntField(
				regionCount,
				EditorStyles.textField,
				GUILayout.Height(18),
				GUILayout.Width(24)
			);
			GUILayout.Label("Subregion"+(newRegionCount != 1 ? "s" : ""));
			GUILayout.EndHorizontal();
			if(newRegionCount < 1) {
				newRegionCount = 1;
			}
			Array.Resize(ref newReg.regions, newRegionCount);
			for(int i = 0; i < newRegionCount; i++) {
				EditorGUILayout.Space();
				EditorGUILayout.Space();
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space();
				EditorGUILayout.BeginVertical();
			  //0...i regiongui(...,i)
				newReg.regions[i] = RegionGUI(
					null,
					prefix+"."+i+".",
					newReg.regions[i],
					formulaOptions,
					lastFocusedControl,
					width-16,
					i
				);
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();
			}
		}
		return newReg;
	}

	public static Region RegionGUI(
		string label, string type,
		Region reg,
		string[] formulaOptions,
		string lastFocusedControl,
		float width,
		int subregionIndex=-1
	) {
		if(reg == null) { reg = new Region(); }
		if(regionTypes == null) {
			regionTypes = new GUIContent[]{
				new GUIContent("Cylinder", EditorGUIUtility.LoadRequired("rgn-cylinder.png") as Texture),
				new GUIContent("Sphere", EditorGUIUtility.LoadRequired("rgn-sphere.png") as Texture),
				new GUIContent("Line", EditorGUIUtility.LoadRequired("rgn-line.png") as Texture),
				new GUIContent("Line Move", EditorGUIUtility.LoadRequired("rgn-linemove.png") as Texture),
				new GUIContent("Cone", EditorGUIUtility.LoadRequired("rgn-cone.png") as Texture),
				new GUIContent("Self", EditorGUIUtility.LoadRequired("rgn-self.png") as Texture),
				new GUIContent("N-Way", EditorGUIUtility.LoadRequired("rgn-nway.png") as Texture),
				new GUIContent("Compound", EditorGUIUtility.LoadRequired("rgn-compound.png") as Texture),
			};
		}
		if(spaceTypes == null) {
			spaceTypes = new GUIContent[]{
				new GUIContent("Pick", EditorGUIUtility.LoadRequired("intv-pick.png") as Texture),
				new GUIContent("Path", EditorGUIUtility.LoadRequired("intv-path.png") as Texture),
				new GUIContent("Line", EditorGUIUtility.LoadRequired("intv-line.png") as Texture),
				new GUIContent("Arc", EditorGUIUtility.LoadRequired("intv-arc.png") as Texture)
			};
		}
		Region newReg = reg ?? new Region();
		if(label != null &&
			 subregionIndex == -1 &&
		   !(newReg.editorShowContents = EditorGUILayout.Foldout(newReg.editorShowContents, label))) {
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
				if(newReg.type != RegionType.LineMove) {
					GUILayout.Label("Intervening Space Type");
					newReg.interveningSpaceType = (InterveningSpaceType)GUILayout.SelectionGrid((int)newReg.interveningSpaceType, spaceTypes, buttonsWide, imageButtonGridStyle);
				} else {
					newReg.interveningSpaceType = InterveningSpaceType.LineMove;
				}
				if(newReg.type != RegionType.LineMove) {
					newReg.canTargetSelf = EditorGUILayout.Toggle("Can Target Self", newReg.canTargetSelf);
				} else {
					newReg.canTargetSelf = true;
				}
				newReg.canTargetFriends = EditorGUILayout.Toggle("Can Target Friends", newReg.canTargetFriends);
				newReg.canTargetEnemies = EditorGUILayout.Toggle("Can Target Enemies", newReg.canTargetEnemies);
			}
		}
		return SimpleRegionGUI(type+"."+label, newReg, formulaOptions, lastFocusedControl, width, subregionIndex);
	}

	static GUIContent[] targetingModes;
	static GUIContent[] displayUnimpededTargetRegionFlags;

	public static TargetSettings TargetSettingsGUI(string label, TargetSettings oldTs, ActionSkillDef atk, string[] formulaOptions, string lastFocusedControl, int i=-1) {
		if(targetingModes == null || targetingModes.Length == 0) {
			targetingModes = new GUIContent[]{
				new GUIContent("Self", EditorGUIUtility.LoadRequired("skl-target-self.png") as Texture),
				new GUIContent("Pick", EditorGUIUtility.LoadRequired("skl-target-pick.png") as Texture),
				new GUIContent("Face", EditorGUIUtility.LoadRequired("skl-target-cardinal.png") as Texture),
				new GUIContent("Turn", EditorGUIUtility.LoadRequired("skl-target-radial.png") as Texture),
				new GUIContent("Select Region", EditorGUIUtility.LoadRequired("skl-target-selectregion.png") as Texture),
				new GUIContent("Draw Path", EditorGUIUtility.LoadRequired("skl-target-path.png") as Texture)
			};
		}
		if(displayUnimpededTargetRegionFlags == null || displayUnimpededTargetRegionFlags.Length == 0) {
			displayUnimpededTargetRegionFlags = new GUIContent[]{
				new GUIContent("Hide", EditorGUIUtility.LoadRequired("skl-impeded-off.png") as Texture),
				new GUIContent("Display", EditorGUIUtility.LoadRequired("skl-impeded-on.png") as Texture)
			};
		}
		int buttonsWide = (int)(Screen.width/buttonDim);
		TargetSettings ts = oldTs ?? new TargetSettings();
		if(label != null) {
			if(!(ts.showInEditor = EditorGUILayout.Foldout(ts.showInEditor, label))) {
				return ts;
			}
		}
		if(atk == null || atk.multiTargetMode == MultiTargetMode.Chain) {
			ts.doNotMoveChain = EditorGUILayout.Toggle("Chain: Leave Origin in Place", ts.doNotMoveChain);
		}
		if(ts.targetingMode == TargetingMode.Pick ||
		   ts.targetingMode == TargetingMode.Path) {
		  ts.allowsCharacterTargeting = EditorGUILayout.Toggle("Can Delay-Target Characters", ts.allowsCharacterTargeting);
		} else {
			ts.allowsCharacterTargeting = false;
		}
		EditorGUI.indentLevel++;
		ts.targetingMode = (TargetingMode)GUILayout.SelectionGrid((int)ts.targetingMode, targetingModes, buttonsWide, EditorGUIExt.imageButtonGridStyle);
		if(ts.targetingMode == TargetingMode.SelectRegion) {
			ts.targetRegion.type = RegionType.Compound;
		}
		if(ts.targetingMode == TargetingMode.Path) {
			ts.newNodeThreshold = EditorGUILayout.FloatField("Min Path Distance", ts.newNodeThreshold);
			ts.immediatelyExecuteDrawnPath = EditorGUILayout.Toggle("Instantly Apply Path", ts.immediatelyExecuteDrawnPath);
		}
		if(ts.targetingMode != TargetingMode.Self) {
			if(ts.targetRegion == null) {
				ts.targetRegion = new Region();
			}
			if(ts.targetRegion.interveningSpaceType != InterveningSpaceType.Pick) {
				GUILayout.Label("Show blocked tiles?");
				ts.displayUnimpededTargetRegion = GUILayout.SelectionGrid(ts.displayUnimpededTargetRegion ? 1 : 0, displayUnimpededTargetRegionFlags, 2, EditorGUIExt.imageButtonGridStyle) == 1 ? true : false;
			}
		}
		EditorGUI.indentLevel--;
		EditorGUILayout.Space();
		ts.targetRegion = EditorGUIExt.RegionGUI("Target Region", label+"."+i+".target", ts.targetRegion, formulaOptions, lastFocusedControl, Screen.width-16);
		EditorGUILayout.Space();
		if(atk == null || !(atk is MoveSkillDef)) {
			ts.effectRegion = EditorGUIExt.RegionGUI("Effect Region", label+"."+i+".effect", ts.effectRegion, formulaOptions, lastFocusedControl, Screen.width-16);
		}
		return ts;
	}
	public static SkillIO SkillIOGUI(string label, SkillIO io, ActionSkillDef s, string[] formulaOptions, string lastFocusedControl) {
		//FIXME: implicit assumption: lockToGrid=true;
		//so, no invertOverlay, overlayType, drawOverlayRim, drawOverlayVolume, rotationSpeedXYF
		//FIXME: not showing probe for now
		SkillIO newIO = io ?? new SkillIO();
		if(label != null) {
			if(!(newIO.editorShow = EditorGUILayout.Foldout(newIO.editorShow, label))) {
				return newIO;
			}
		}
		newIO.overlayColor = EditorGUILayout.ColorField("Target Overlay", newIO.overlayColor);
		newIO.highlightColor = EditorGUILayout.ColorField("Effect Highlight", newIO.highlightColor);
		newIO.supportKeyboard = EditorGUILayout.Toggle("Support Keyboard", newIO.supportKeyboard);
		if(newIO.supportKeyboard) {
			newIO.keyboardMoveSpeed = EditorGUILayout.FloatField("Keyboard Move Speed", newIO.keyboardMoveSpeed);
			newIO.indicatorCycleLength = EditorGUILayout.FloatField("Z Cycle Time", newIO.indicatorCycleLength);
		}
		newIO.supportMouse = EditorGUILayout.Toggle("Support Mouse", newIO.supportMouse);
		newIO.requireConfirmation = EditorGUILayout.Toggle("Require Confirmation", newIO.requireConfirmation);
		if(s == null ||
		   s.HasTargetingMode(TargetingMode.Path)) {
			newIO.pathMaterial = EditorGUILayout.ObjectField("Path Material", newIO.pathMaterial, typeof(Material), false) as Material;
		}
		if(s == null ||
			 (s.HasTargetingMode(TargetingMode.Pick) ||
		    s.HasTargetingMode(TargetingMode.Path))) {
			if(newIO.requireConfirmation) {
				newIO.performTemporaryStepsOnConfirmation = EditorGUILayout.Toggle("Preview Before Confirming", newIO.performTemporaryStepsOnConfirmation);
			}
			newIO.performTemporaryStepsImmediately = EditorGUILayout.Toggle("Preview Immediately", newIO.performTemporaryStepsImmediately);
		} else {
			newIO.performTemporaryStepsImmediately = false;
			newIO.performTemporaryStepsOnConfirmation = true;
		}
		return newIO;
	}

	public static T PickAssetGUI<T>(string label, T t) where T : ScriptableObject {
	  T newT = EditorGUILayout.ObjectField(
	    label,
	    t as UnityEngine.Object,
	    typeof(T),
	    false
	  ) as T;
	  return newT;
	}
}
