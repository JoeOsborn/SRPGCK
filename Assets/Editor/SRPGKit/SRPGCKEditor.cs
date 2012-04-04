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

	protected HOEditorUndoManager um;

	protected virtual void UpdateFormulae() {
		if(!useFormulae) { return; }
		if(fdb != null && fdb.formulae != null) {
			formulaOptions = (new string[]{"Custom"}).
				Concat(fdb.formulae.Select(f => f.name).
				OrderBy(n => n)).
				ToArray();
		} else {
			formulaOptions = new string[]{"Custom"};
		}
	}

	public virtual void OnEnable() {
		um = new HOEditorUndoManager(target, "Inspector");
		if(useFormulae) {
			if(fdb == null) { fdb = Formulae.DefaultFormulae; }
			UpdateFormulae();
		}
	}

	public abstract void OnSRPGCKInspectorGUI();

	public override void OnInspectorGUI() {
		newFocusedControl = GUI.GetNameOfFocusedControl();
	  um.CheckUndo();
		// if(useFormulae) {
		// 	GUILayout.BeginHorizontal();
		// 	GUILayout.Label("Formulae", GUILayout.Width(60));
		// 	GUI.enabled=false;
		// 	EditorGUILayout.TextField(fdb == null ? "null" : AssetDatabase.GetAssetPath(fdb));
		// 	GUI.enabled=true;
		// 	GUILayout.EndHorizontal();
		// }
		OnSRPGCKInspectorGUI();
		guiChangedAtAll = guiChangedAtAll || GUI.changed;
		um.CheckDirty();
		lastFocusedControl = newFocusedControl;
		if(guiChangedAtAll &&
		   EditorApplication.isPlayingOrWillChangePlaymode) {
			// Debug.Log("save before playmode change");
			SaveAsset();
		}
	}

	protected virtual void SaveAsset() {
		um.ForceDirty();
		guiChanged = false;
		guiChangedAtAll = false;
	}

	public virtual bool FocusMovedFrom(string name) {
		return lastFocusedControl == name && newFocusedControl != name;
	}

	protected virtual void OnDisable() {
		if(guiChangedAtAll) {
			SaveAsset();
		}
		um = null;
	}

	public static void EnsurePath(string p) {
		if(!Directory.Exists(p)) {
			Directory.CreateDirectory(p);
			AssetDatabase.Refresh();
		}
	}
}