using UnityEngine;
using UnityEditor;
using System.Linq;

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
		if(!useFormulae) { return; }
		if(fdb == null) { fdb = Formulae.DefaultFormulae; }
		UpdateFormulae();
	}

	public abstract void OnSRPGCKInspectorGUI();

	public override void OnInspectorGUI() {
		CheckFocus();
	  CheckUndo();
		if(useFormulae) {
			GUILayout.BeginHorizontal();
			GUILayout.Label("Formulae", GUILayout.Width(60));
			GUI.enabled=false;
			EditorGUILayout.TextField(fdb == null ? "null" : AssetDatabase.GetAssetPath(fdb));
			GUI.enabled=true;
			GUILayout.EndHorizontal();
		}
		OnSRPGCKInspectorGUI();
		FinishOnGUI();
		if((guiChangedAtAll || guiChanged) &&
		   EditorApplication.isPlayingOrWillChangePlaymode) {
			// Debug.Log("save before playmode change");
			EditorUtility.SetDirty(target);
			guiChanged = false;
			guiChangedAtAll = false;
		}
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
			EditorUtility.SetDirty(target);
			guiChanged = false;
			guiChangedAtAll = false;
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

}