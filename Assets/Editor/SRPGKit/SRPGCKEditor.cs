using UnityEngine;
using UnityEditor;
using System.Linq;

public abstract class SRPGCKEditor : Editor {
	public Formulae fdb;

	public bool listeningForGuiChanges;
	public bool guiChanged;

	public string[] formulaOptions;

	public string lastFocusedControl, newFocusedControl;

	protected virtual void UpdateFormulae() {
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
		if(fdb == null) { fdb = Formulae.DefaultFormulae; }
		UpdateFormulae();
	}

	public abstract void OnSRPGCKInspectorGUI();

	public override void OnInspectorGUI() {
	  CheckUndo();
		CheckFocus();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Formulae", GUILayout.Width(60));
		GUI.enabled=false;
		EditorGUILayout.TextField(fdb == null ? "null" : AssetDatabase.GetAssetPath(fdb));
		GUI.enabled=true;
		GUILayout.EndHorizontal();
		OnSRPGCKInspectorGUI();
		FinishOnGUI();
	}

	public virtual bool FocusMovedFrom(string name) {
		return lastFocusedControl == name && newFocusedControl != name;
	}

	protected virtual void FinishOnGUI() {
		if(GUI.changed) {
			EditorUtility.SetDirty(target);
		}
		lastFocusedControl = newFocusedControl;
	}

  private void CheckUndo()
  {
    Event e = Event.current;

    if ( e.type == EventType.MouseDown && e.button == 0 || e.type == EventType.KeyUp && ( e.keyCode == KeyCode.Tab ) ) {
      // When the LMB is pressed or the TAB key is released,
      // store a snapshot, but don't register it as an undo
      // ( so that if nothing changes we avoid storing a useless undo)
      Undo.SetSnapshotTarget( target, name+"InspectorUndo" );
      Undo.CreateSnapshot();
      Undo.ClearSnapshotTarget();
      listeningForGuiChanges = true;
      guiChanged = false;
    }

    if ( listeningForGuiChanges && guiChanged ) {
      // Some GUI value changed after pressing the mouse.
      // Register the previous snapshot as a valid undo.
      Undo.SetSnapshotTarget( target, name+"InspectorUndo" );
      Undo.RegisterSnapshot();
      Undo.ClearSnapshotTarget();
      listeningForGuiChanges = false;
    }
  }

}