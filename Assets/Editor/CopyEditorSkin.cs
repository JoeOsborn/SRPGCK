using UnityEngine;

using UnityEditor;

using System.Collections;



public class CopyEditorSkin : EditorWindow

{

    public GUISkin mySkin;

    public EditorSkin editorSkin = EditorSkin.Inspector;



    [MenuItem("Window/CopyEditorSkin")]

    public static void Init()

    {

        CopyEditorSkin window = (CopyEditorSkin)EditorWindow.GetWindow(typeof(CopyEditorSkin));

        window.Show();

    }



    public void OnGUI()

    {

        mySkin = EditorGUILayout.ObjectField("My Skin", mySkin, typeof(GUISkin), false) as GUISkin;

        editorSkin = (EditorSkin)EditorGUILayout.EnumPopup("Editor Skin", editorSkin);



        if (mySkin == null)

            GUI.enabled = false;



        if (GUILayout.Button("Copy Editor Skin"))

        {

            GUISkin builtinSkin = EditorGUIUtility.GetBuiltinSkin(editorSkin);

            EditorUtility.CopySerialized(builtinSkin, mySkin);

        }



        GUILayout.Label("NOTE: This will delete all Custom Styles!");

    }

}
