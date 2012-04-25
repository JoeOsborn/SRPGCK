using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(Item))]
public class ItemEditor : SRPGCKEditor {
	[MenuItem("SRPGCK/Create item", false, 3)]
	public static Item CreateItem()
	{
		return ScriptableObjectUtility.CreateAsset<Item>(
			null,
			"Assets/SRPGCK Data/Items",
			true
		);
	}
	Item it;

	public override void OnEnable() {
		it = target as Item;
		base.OnEnable();
		it.fdb = fdb;
		name = "Item";
	}

	public override void OnSRPGCKInspectorGUI() {
		it.name = EditorGUILayout.TextField("Name", it.name);
		it.prefab = EditorGUILayout.ObjectField(
			"Prefab", 
			it.prefab as UnityEngine.Object, 
			typeof(Transform), 
			false
		) as Transform;
		it.weight = EditorGUILayout.FloatField("Weight", it.weight);
		it.stackSize = EditorGUILayout.IntField("Stack Size", it.stackSize);
		it.parameters = EditorGUIExt.ParameterFoldout(
			"Parameter",
			it.parameters,
			"item."+it.itemName+".params.",
			formulaOptions,
			lastFocusedControl,
			ref it.editorShowParameters
		);
	}
}
