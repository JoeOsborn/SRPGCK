using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

public abstract class SRPGCKEditor : Editor {
	public Formulae fdb;

	public bool listeningForGuiChanges;
	public bool guiChanged, guiChangedAtAll;

	public string[] formulaOptions;

	public string lastFocusedControl, newFocusedControl;

	protected bool useFormulae=true;

	protected virtual void UpdateFormulae() {
		if(!useFormulae) { return; }
		if(fdb != null && fdb.formulaNames != null) {
			formulaOptions = (new string[]{"Custom"}).Concat(fdb.formulaNames).ToArray();
		} else {
			formulaOptions = new string[]{"Custom"};
		}
	}

	void CheckFocus() {
	  newFocusedControl = GUI.GetNameOfFocusedControl();
	}

	public virtual void OnEnable() {
		guiChangedAtAll = false;
		guiChanged = false;
		if(useFormulae) {
			if(fdb == null) { fdb = Formulae.DefaultFormulae; }
			UpdateFormulae();
		}
	}

	public abstract void OnSRPGCKInspectorGUI();

	public override void OnInspectorGUI() {
		CheckFocus();
	  CheckUndo();
		// if(useFormulae) {
		// 	GUILayout.BeginHorizontal();
		// 	GUILayout.Label("Formulae", GUILayout.Width(60));
		// 	GUI.enabled=false;
		// 	EditorGUILayout.TextField(fdb == null ? "null" : AssetDatabase.GetAssetPath(fdb));
		// 	GUI.enabled=true;
		// 	GUILayout.EndHorizontal();
		// }
		OnSRPGCKInspectorGUI();
		FinishOnGUI();
		if((guiChangedAtAll || guiChanged) &&
		   EditorApplication.isPlayingOrWillChangePlaymode) {
			// Debug.Log("save before playmode change");
			SaveAsset();
		}
	}

	protected virtual void SaveAsset() {
		EditorUtility.SetDirty(target);
		guiChanged = false;
		guiChangedAtAll = false;
	}

	public virtual bool FocusMovedFrom(string name) {
		return lastFocusedControl == name && newFocusedControl != name;
	}

	protected virtual void FinishOnGUI() {
		if(GUI.changed) {
			guiChanged = true;
			guiChangedAtAll = true;
		}
		lastFocusedControl = newFocusedControl;
	}

	protected virtual void OnDisable() {
		if(guiChanged || guiChangedAtAll) {
			// Debug.Log("disable "+target.name);
			SaveAsset();
		}
	}

  private void CheckUndo()
  {
    Event e = Event.current;

    if((e.type == EventType.MouseDown) ||
		   (e.type == EventType.KeyUp && e.keyCode == KeyCode.Tab) ||
		   (newFocusedControl != lastFocusedControl)) {
		 listeningForGuiChanges = true;
 			// Debug.Log("ready to store undo, changed="+guiChanged);
    }

    if(listeningForGuiChanges && guiChanged) {
			// Debug.Log("store undo");
      // Some GUI value changed after pressing the mouse.
      // Register the previous snapshot as a valid undo.
      // Undo.SetSnapshotTarget(target, name+"InspectorUndo");
      // Undo.CreateSnapshot();
      // Undo.RegisterSnapshot();
      // Undo.ClearSnapshotTarget();
      listeningForGuiChanges = false;
      guiChanged = false;
    }
  }
	protected static void EnsurePath(string p) {
		if(!Directory.Exists(p)) {
			Directory.CreateDirectory(p);
			AssetDatabase.Refresh();
		}
	}
	protected static void CopyFieldsTo<TA, TB>(TA s, TB def) {
		FieldInfo[] fields = typeof(TA).
			GetFields(BindingFlags.Public |
								BindingFlags.Instance |
                BindingFlags.NonPublic).
			Where(info =>
			  (info.IsPublic && !info.IsNotSerialized) ||
			  info.GetCustomAttributes(typeof(SerializeField),true).Length != 0
			).ToArray();
		FieldInfo[] defFields = typeof(TB).
			GetFields(BindingFlags.Public |
								BindingFlags.Instance |
                BindingFlags.NonPublic).
			Where(info =>
			  (info.IsPublic && !info.IsNotSerialized) ||
			  info.GetCustomAttributes(typeof(SerializeField),true).Length != 0
			).ToArray();
		for(int i = 0; i < defFields.Length; i++) {
			FieldInfo sdf = defFields[i];
			string sdfn = sdf.Name;
			Debug.Log("copy "+sdfn+"?");
			FieldInfo sf = fields.FirstOrDefault(fi => fi.Name == sdfn);
			if(sf == null) { Debug.Log("no field!"); continue; }
			Debug.Log("src "+(sf.GetValue(s) != null ? sf.GetValue(s).ToString() : "(null)"));
			if(sf.FieldType.IsValueType) {
				sdf.SetValue(def, sf.GetValue(s));
				Debug.Log("byval");
			} else if(sf.FieldType.IsSubclassOf(typeof(ScriptableObject))) {
				ScriptableObject oldO = sf.GetValue(s) as ScriptableObject;
				if(oldO == null) { Debug.Log("null SO"); continue; }
				ScriptableObject newO = Instantiate(oldO) as ScriptableObject;
				Debug.Log("byscopy");
				sdf.SetValue(def, newO);
			} else {
				object oldO = sf.GetValue(s);
				if(oldO == null) { Debug.Log("null O"); continue; }
				object newO = oldO.Copy();
				sdf.SetValue(def, newO);
				Debug.Log("bycopy");
			}
		}
	}
}