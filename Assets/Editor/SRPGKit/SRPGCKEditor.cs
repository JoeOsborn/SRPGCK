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
	
	protected bool supportsUndo=true;

	protected virtual void UpdateFormulae() {
		if(!useFormulae) { return; }
		if(fdb != null && fdb.formulae != null) {
			formulaOptions = (new string[]{"Custom"}).
				Concat(fdb.formulae.Select(f => f.name).OrderBy(n => n)).
				ToArray();
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
		StoreUndo();
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

	protected virtual UnityEngine.Object[] UndoTargets { get {
		return targets;
	} }

  private void CheckUndo()
  {
		if(!supportsUndo) { guiChanged = false; return; }
    Event e = Event.current;

    if((e.type == EventType.MouseDown) ||
		   (e.type == EventType.KeyUp && e.keyCode == KeyCode.Tab)) {
		  listeningForGuiChanges = true;
      Undo.SetSnapshotTarget(UndoTargets, "Modify "+name);
      Undo.CreateSnapshot();
      Undo.ClearSnapshotTarget();
			guiChanged = false;
    }

		StoreUndo();
  }
	protected virtual void StoreUndo() {
    if(supportsUndo && listeningForGuiChanges && guiChanged) {
			// Debug.Log("store undo");
	    // Some GUI value changed after pressing the mouse.
	    // Register the previous snapshot as a valid undo.
	    Undo.SetSnapshotTarget(UndoTargets, "Modify "+name);
	    Undo.RegisterSnapshot();
	    Undo.ClearSnapshotTarget();
	    listeningForGuiChanges = false;
	    guiChanged = false;
		}
	}
	public static void EnsurePath(string p) {
		if(!Directory.Exists(p)) {
			Directory.CreateDirectory(p);
			AssetDatabase.Refresh();
		}
	}
}