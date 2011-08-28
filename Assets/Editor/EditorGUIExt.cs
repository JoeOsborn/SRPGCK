using UnityEngine;
using UnityEditor;
using System;

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
    public static string[] ArrayFoldout(string label, string[] array, ref bool foldout)
    {
        EditorGUILayout.BeginVertical();
        EditorGUIUtility.LookLikeInspector();
        foldout = EditorGUILayout.Foldout(foldout, label);
        string[] newArray = array;
        if (foldout)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();
            int arraySize = EditorGUILayout.IntField("Size", array.Length);
            if (arraySize != array.Length)
                newArray = new string[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                string entry = "";
                if (i < array.Length)
                    entry = array[i];
                newArray[i] = EditorGUILayout.TextField("Element " + i, entry);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
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

}
