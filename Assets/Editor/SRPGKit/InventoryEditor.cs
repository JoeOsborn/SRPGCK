using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(Inventory))]
public class InventoryEditor : SRPGCKEditor {
	Inventory inv;

	public override void OnEnable() {
		inv = target as Inventory;
		base.OnEnable();
		name = "Inventory";
	}

	public override void OnSRPGCKInspectorGUI () {
		if(inv.GetComponent<Character>() != null) {
			EditorGUILayout.HelpBox("Inventory is used for unequipped items such as commodities and consumables. Inventory items do not provide passive effects, skills, or other adjustments until they are instantiated. Formulae are in character-stat scope.", MessageType.Info);
		}
		EditorGUIUtility.LookLikeInspector();
		inv.limitedStacks = EditorGUILayout.Toggle(
			"Limited Stacks",
			inv.limitedStacks
		);
		if(inv.limitedStacks) {
			inv.stackLimitF = EditorGUIExt.FormulaField(
				"Stack Limit:",
				inv.stackLimitF ??
					Formula.Constant(20),
				inv.name+".inventory.stackLimit",
				formulaOptions,
				lastFocusedControl
			);
		}
		
		inv.limitedStacks = EditorGUILayout.Toggle(
			"Override Item Stack Size",
			inv.limitedStackSize
		);
		if(inv.limitedStackSize) {
			inv.stackSizeF = EditorGUIExt.FormulaField(
				"Stack Size:",
				inv.stackSizeF ??
					Formula.Constant(1),
				inv.name+".inventory.stackSize",
				formulaOptions,
				lastFocusedControl
			);
		}

		inv.limitedWeight = EditorGUILayout.Toggle(
			"Limited Weight",
			inv.limitedWeight
		);
		if(inv.limitedWeight) {
			inv.weightLimitF = EditorGUIExt.FormulaField(
				"Weight Limit:",
				inv.weightLimitF ??
					Formula.Constant(20),
				inv.name+".inventory.weightLimit",
				formulaOptions,
				lastFocusedControl
			);
		}

		inv.stacksMustBeUnique = EditorGUILayout.Toggle(
			"Stacks Must Be Unique",
			inv.stacksMustBeUnique
		);

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.BeginVertical();
		if(inv.items == null) { inv.items = new List<Item>(); }
		if(inv.counts == null) { inv.counts = new List<int>(); }
		int arraySize = inv.items.Count;
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Size", GUILayout.Height(18));
		GUILayout.FlexibleSpace();
		arraySize = EditorGUILayout.IntField(
			arraySize,
			EditorStyles.textField,
			GUILayout.Height(18)
		);
		EditorGUILayout.EndHorizontal();
		while(arraySize > inv.items.Count) {
			Item def = null;
			int ct = 1;
			if(inv.items.Count > 0) {
				def = inv.items[inv.items.Count-1];
				ct = inv.counts[inv.counts.Count-1];
			}
			inv.items.Add(def);
			inv.counts.Add(ct);
		}
		while(arraySize < inv.items.Count) {
			inv.items.RemoveAt(inv.items.Count-1);
			inv.counts.RemoveAt(inv.counts.Count-1);
		}
		for(int i = 0; i < inv.items.Count; i++) {
			EditorGUILayout.BeginHorizontal();
			inv.items[i] = EditorGUILayout.ObjectField(
				inv.items[i] as UnityEngine.Object,
				typeof(Item),
				false
			) as Item;
			GUILayout.FlexibleSpace();
			inv.counts[i] = EditorGUILayout.IntField(inv.counts[i]);
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.EndVertical();
		EditorGUILayout.EndHorizontal();
		EditorGUIUtility.LookLikeControls();
	}
}
