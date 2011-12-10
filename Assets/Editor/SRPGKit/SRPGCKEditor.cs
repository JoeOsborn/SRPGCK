using UnityEngine;
using UnityEditor;
using System.Linq;

public abstract class SRPGCKEditor : Editor {
	public bool listeningForGuiChanges;
	public bool guiChanged;

	public string[] formulaOptions;
	
	public string lastFocusedControl, newFocusedControl;
	
	void UpdateFormulae() {
		if(Formulae.GetInstance() == null) {
			Debug.Log("Need a formulae object somewhere to store formulae!");
		} else {
			formulaOptions = (new string[]{"Custom"}).Concat(Formulae.GetInstance().formulaNames).ToArray();
		}
	}
	
	void CheckFocus() {
	  newFocusedControl = GUI.GetNameOfFocusedControl();	
	}
	
	public virtual void OnEnable() {
		UpdateFormulae();
	}
	
	public abstract void OnSRPGCKInspectorGUI();
	
	public override void OnInspectorGUI() {
	  CheckUndo();
		CheckFocus();
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